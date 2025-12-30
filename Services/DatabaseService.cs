using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Constants;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace scheidingsdesk_document_generator.Services
{
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _connectionString = _configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' not found in configuration");
        }

        /// <summary>
        /// Retrieves complete dossier data including parties, children, visitation and care arrangements
        /// </summary>
        /// <param name="dossierId">The ID or dossier nummer to retrieve</param>
        /// <returns>Complete dossier data or null if not found</returns>
        public async Task<DossierData?> GetDossierDataAsync(int dossierId)
        {
            try
            {
                _logger.LogInformation("Retrieving dossier data for dossier ID/Nummer: {DossierId}", dossierId);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // First, resolve the actual dossier ID (in case a dossier nummer was provided)
                int actualDossierId = await ResolveDossierIdAsync(connection, dossierId);

                // Create a single command with multiple result sets
                const string query = @"
                    -- Result set 1: Dossier information
                    SELECT id, dossier_nummer, aangemaakt_op, gewijzigd_op, status, gebruiker_id, is_anoniem
                    FROM dbo.dossiers 
                    WHERE id = @DossierId;

                    -- Result set 2: Parties (rol_id 1 and 2)
                    SELECT p.id, p.voorletters, p.voornamen, p.roepnaam, p.geslacht, 
                           p.tussenvoegsel, p.achternaam, p.adres, p.postcode, p.plaats,
                           p.geboorteplaats, p.geboorte_datum, p.nationaliteit_1, p.nationaliteit_2,
                           p.telefoon, p.email, p.beroep, dp.rol_id, r.naam as rol_naam
                    FROM dbo.personen p
                    INNER JOIN dbo.dossiers_partijen dp ON p.id = dp.persoon_id
                    INNER JOIN dbo.rollen r ON dp.rol_id = r.id
                    WHERE dp.dossier_id = @DossierId AND dp.rol_id IN (1, 2)
                    ORDER BY dp.rol_id;

                    -- Result set 3: Children
                    SELECT p.id, p.voorletters, p.voornamen, p.roepnaam, p.geslacht, 
                           p.tussenvoegsel, p.achternaam, p.adres, p.postcode, p.plaats,
                           p.geboorteplaats, p.geboorte_datum, p.nationaliteit_1, p.nationaliteit_2,
                           p.telefoon, p.email, p.beroep
                    FROM dbo.personen p
                    INNER JOIN dbo.dossiers_kinderen dk ON p.id = dk.kind_id
                    WHERE dk.dossier_id = @DossierId
                    ORDER BY p.geboorte_datum DESC;

                    -- Result set 4: Parent-child relations
                    SELECT 
                        ok.ouder_id, 
                        p.voornamen + ISNULL(' ' + p.tussenvoegsel, '') + ' ' + p.achternaam AS ouder_naam,
                        ok.relatie_type_id,
                        rt.naam AS relatie_type,
                        ok.kind_id
                    FROM dbo.kinderen_ouders ok
                    INNER JOIN dbo.personen p ON ok.ouder_id = p.id
                    INNER JOIN dbo.relatie_types rt ON ok.relatie_type_id = rt.id
                    WHERE ok.kind_id IN (
                        SELECT kind_id FROM dbo.dossiers_kinderen WHERE dossier_id = @DossierId
                    );

                    -- Result set 5: Visitation arrangements
                    SELECT o.id, o.dag_id, d.naam AS dag_naam, o.dagdeel_id, dd.naam AS dagdeel_naam,
                           o.verzorger_id, p.voornamen + ISNULL(' ' + p.tussenvoegsel, '') + ' ' + p.achternaam AS verzorger_naam,
                           o.wissel_tijd, o.week_regeling_id, wr.omschrijving AS week_regeling_omschrijving,
                           o.week_regeling_anders, o.dossier_id, o.aangemaakt_op, o.gewijzigd_op
                    FROM dbo.omgang o
                    INNER JOIN dbo.dagen d ON o.dag_id = d.id
                    INNER JOIN dbo.dagdelen dd ON o.dagdeel_id = dd.id
                    INNER JOIN dbo.personen p ON o.verzorger_id = p.id
                    INNER JOIN dbo.week_regelingen wr ON o.week_regeling_id = wr.id
                    WHERE o.dossier_id = @DossierId
                    ORDER BY d.id, dd.id;

                    -- Result set 6: Care arrangements
                    SELECT z.id, z.zorg_categorie_id, zc.naam AS zorg_categorie_naam,
                           z.zorg_situatie_id, zs.naam AS zorg_situatie_naam,
                           z.overeenkomst, z.situatie_anders, z.dossier_id,
                           z.aangemaakt_op, z.aangemaakt_door, z.gewijzigd_op, z.gewijzigd_door
                    FROM dbo.zorg z
                    INNER JOIN dbo.zorg_categorieen zc ON z.zorg_categorie_id = zc.id
                    INNER JOIN dbo.zorg_situaties zs ON z.zorg_situatie_id = zs.id
                    WHERE z.dossier_id = @DossierId
                    ORDER BY zc.naam, zs.naam;

                    -- Result set 7: Alimentatie (Alimony) - Optional, may not exist
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'alimentaties' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        SELECT a.id, a.dossier_id, a.netto_besteedbaar_gezinsinkomen,
                               a.kosten_kinderen, a.bijdrage_kosten_kinderen,
                               a.bijdrage_template, bt.omschrijving AS bijdrage_template_omschrijving,
                               a.storting_ouder1_kinderrekening, a.storting_ouder2_kinderrekening,
                               a.kinderrekening_kostensoorten, a.kinderrekening_maximum_opname,
                               a.kinderrekening_maximum_opname_bedrag, a.kinderbijslag_storten_op_kinderrekening,
                               a.kindgebonden_budget_storten_op_kinderrekening, a.bedragen_alle_kinderen_gelijk,
                               a.alimentatiebedrag_per_kind, a.alimentatiegerechtigde, a.zorgkorting_percentage_alle_kinderen,
                               a.afspraken_alle_kinderen_gelijk, a.hoofdverblijf_alle_kinderen,
                               a.inschrijving_alle_kinderen, a.kinderbijslag_ontvanger_alle_kinderen,
                               a.kindgebonden_budget_alle_kinderen
                        FROM dbo.alimentaties a
                        LEFT JOIN dbo.bijdrage_templates bt ON a.bijdrage_template = bt.id
                        WHERE a.dossier_id = @DossierId;
                    END
                    ELSE
                    BEGIN
                        SELECT NULL WHERE 1=0; -- Empty result set
                    END

                    -- Result set 8: Bijdragen kosten kinderen (Child cost contributions) - Optional
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'bijdragen_kosten_kinderen' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        SELECT bkk.id, bkk.alimentatie_id, bkk.personen_id, bkk.eigen_aandeel,
                               p.voornamen + ISNULL(' ' + p.tussenvoegsel, '') + ' ' + p.achternaam AS persoon_naam
                        FROM dbo.bijdragen_kosten_kinderen bkk
                        INNER JOIN dbo.personen p ON bkk.personen_id = p.id
                        WHERE bkk.alimentatie_id IN (
                            SELECT id FROM dbo.alimentaties WHERE dossier_id = @DossierId
                        );
                    END
                    ELSE
                    BEGIN
                        SELECT NULL WHERE 1=0; -- Empty result set
                    END

                    -- Result set 9: Financiele afspraken kinderen (Financial agreements for children) - Optional
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'financiele_afspraken_kinderen' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        SELECT fak.id, fak.alimentatie_id, fak.kind_id, fak.alimentatie_bedrag,
                               fak.hoofdverblijf, fak.kinderbijslag_ontvanger, fak.zorgkorting_percentage,
                               fak.inschrijving, fak.kindgebonden_budget,
                               p.voornamen + ISNULL(' ' + p.tussenvoegsel, '') + ' ' + p.achternaam AS kind_naam
                        FROM dbo.financiele_afspraken_kinderen fak
                        INNER JOIN dbo.personen p ON fak.kind_id = p.id
                        WHERE fak.alimentatie_id IN (
                            SELECT id FROM dbo.alimentaties WHERE dossier_id = @DossierId
                        );
                    END
                    ELSE
                    BEGIN
                        SELECT NULL WHERE 1=0; -- Empty result set
                    END

                    -- Result set 10: Ouderschapsplan info - Optional
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ouderschapsplan_info' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        -- Select all columns dynamically to support both old and new schemas
                        SELECT opi.*
                        FROM dbo.ouderschapsplan_info opi
                        WHERE opi.dossier_id = @DossierId;
                    END
                    ELSE
                    BEGIN
                        SELECT NULL WHERE 1=0; -- Empty result set
                    END

                    -- Result set 11: Communicatie Afspraken - Optional
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'communicatie_afspraken' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        SELECT ca.id, ca.dossier_id, ca.villa_pinedo_kinderen, ca.kinderen_betrokkenheid,
                               ca.kies_methode, ca.opvang, ca.informatie_uitwisseling,
                               ca.bijlage_beslissingen, ca.social_media, ca.mobiel_tablet, ca.toezicht_apps,
                               ca.locatie_delen, ca.id_bewijzen, ca.aansprakelijkheidsverzekering, ca.ziektekostenverzekering,
                               ca.toestemming_reizen, ca.jongmeerderjarige, ca.studiekosten,
                               ca.bankrekening_kinderen, ca.evaluatie, ca.parenting_coordinator, ca.mediation_clausule
                        FROM dbo.communicatie_afspraken ca
                        WHERE ca.dossier_id = @DossierId;
                    END
                    ELSE
                    BEGIN
                        SELECT NULL WHERE 1=0; -- Empty result set
                    END

                    -- Result set 12: Omgangsregeling - Optional (moved from communicatie_afspraken)
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'omgangsregeling' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        SELECT omg.id, omg.dossier_id, omg.omgang_tekst_of_schema, omg.omgang_beschrijving
                        FROM dbo.omgangsregeling omg
                        WHERE omg.dossier_id = @DossierId;
                    END
                    ELSE
                    BEGIN
                        SELECT NULL WHERE 1=0; -- Empty result set
                    END";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@DossierId", actualDossierId);

                using var reader = await command.ExecuteReaderAsync();
                
                // Result set 1: Dossier
                if (!await reader.ReadAsync())
                {
                    _logger.LogWarning("Dossier with ID {DossierId} not found", dossierId);
                    return null;
                }

                var dossier = new DossierData
                {
                    Id = (int)reader["id"],
                    DossierNummer = ConvertToString(reader["dossier_nummer"]),
                    AangemaaktOp = (DateTime)reader["aangemaakt_op"],
                    GewijzigdOp = (DateTime)reader["gewijzigd_op"],
                    Status = ConvertToString(reader["status"]),
                    GebruikerId = (int)reader["gebruiker_id"],
                    IsAnoniem = reader["is_anoniem"] == DBNull.Value ? null : (bool?)reader["is_anoniem"]
                };

                // Result set 2: Parties
                await reader.NextResultAsync();
                var parties = new List<PersonData>();
                while (await reader.ReadAsync())
                {
                    parties.Add(MapPersonData(reader));
                }
                dossier.Partijen = parties;

                // Result set 3: Children
                await reader.NextResultAsync();
                var children = new List<ChildData>();
                while (await reader.ReadAsync())
                {
                    children.Add(MapChildData(reader));
                }
                dossier.Kinderen = children;

                // Result set 4: Parent-child relations
                await reader.NextResultAsync();
                var childRelations = new Dictionary<int, List<ParentChildRelation>>();
                while (await reader.ReadAsync())
                {
                    var kindId = (int)reader["kind_id"];
                    if (!childRelations.ContainsKey(kindId))
                        childRelations[kindId] = new List<ParentChildRelation>();
                    
                    childRelations[kindId].Add(new ParentChildRelation
                    {
                        OuderId = (int)reader["ouder_id"],
                        OuderNaam = ConvertToString(reader["ouder_naam"]),
                        RelatieTypeId = (int)reader["relatie_type_id"],
                        RelatieType = reader["relatie_type"] == DBNull.Value ? null : ConvertToString(reader["relatie_type"])
                    });
                }

                // Assign relations to children
                foreach (var child in children)
                {
                    if (childRelations.ContainsKey(child.Id))
                        child.ParentRelations = childRelations[child.Id];
                }

                // Result set 5: Visitation arrangements
                await reader.NextResultAsync();
                var visitationArrangements = new List<OmgangData>();
                while (await reader.ReadAsync())
                {
                    visitationArrangements.Add(new OmgangData
                    {
                        Id = (int)reader["id"],
                        DagId = (int)reader["dag_id"],
                        DagNaam = ConvertToString(reader["dag_naam"]),
                        DagdeelId = (int)reader["dagdeel_id"],
                        DagdeelNaam = ConvertToString(reader["dagdeel_naam"]),
                        VerzorgerId = (int)reader["verzorger_id"],
                        VerzorgerNaam = ConvertToString(reader["verzorger_naam"]),
                        WisselTijd = reader["wissel_tijd"] == DBNull.Value ? null : ConvertToString(reader["wissel_tijd"]),
                        WeekRegelingId = (int)reader["week_regeling_id"],
                        WeekRegelingOmschrijving = ConvertToString(reader["week_regeling_omschrijving"]),
                        WeekRegelingAnders = reader["week_regeling_anders"] == DBNull.Value ? null : ConvertToString(reader["week_regeling_anders"]),
                        DossierId = (int)reader["dossier_id"],
                        AangemaaktOp = (DateTime)reader["aangemaakt_op"],
                        GewijzigdOp = (DateTime)reader["gewijzigd_op"]
                    });
                }
                dossier.Omgang = visitationArrangements;

                // Result set 6: Care arrangements
                await reader.NextResultAsync();
                var careArrangements = new List<ZorgData>();
                while (await reader.ReadAsync())
                {
                    careArrangements.Add(new ZorgData
                    {
                        Id = (int)reader["id"],
                        ZorgCategorieId = (int)reader["zorg_categorie_id"],
                        ZorgCategorieNaam = ConvertToString(reader["zorg_categorie_naam"]),
                        ZorgSituatieId = (int)reader["zorg_situatie_id"],
                        ZorgSituatieNaam = ConvertToString(reader["zorg_situatie_naam"]),
                        Overeenkomst = ConvertToString(reader["overeenkomst"]),
                        SituatieAnders = reader["situatie_anders"] == DBNull.Value ? null : ConvertToString(reader["situatie_anders"]),
                        DossierId = (int)reader["dossier_id"],
                        AangemaaktOp = (DateTime)reader["aangemaakt_op"],
                        AangemaaktDoor = (int)reader["aangemaakt_door"],
                        GewijzigdOp = (DateTime)reader["gewijzigd_op"],
                        GewijzigdDoor = reader["gewijzigd_door"] == DBNull.Value ? null : (int?)reader["gewijzigd_door"]
                    });
                }
                dossier.Zorg = careArrangements;

                // Result set 7: Alimentatie (Optional - may not exist if tables are not created yet)
                await reader.NextResultAsync();
                AlimentatieData? alimentatie = null;
                try
                {
                    _logger.LogInformation("Reading alimentatie data for dossier {DossierId}, FieldCount: {FieldCount}", dossierId, reader.FieldCount);

                    if (reader.FieldCount > 0 && await reader.ReadAsync())
                    {
                        alimentatie = new AlimentatieData
                        {
                            Id = (int)reader["id"],
                            DossierId = (int)reader["dossier_id"],
                            NettoBesteedbaarGezinsinkomen = reader["netto_besteedbaar_gezinsinkomen"] == DBNull.Value ? null : Convert.ToDecimal(reader["netto_besteedbaar_gezinsinkomen"]),
                            KostenKinderen = reader["kosten_kinderen"] == DBNull.Value ? null : Convert.ToDecimal(reader["kosten_kinderen"]),
                            BijdrageKostenKinderen = reader["bijdrage_kosten_kinderen"] == DBNull.Value ? null : Convert.ToDecimal(reader["bijdrage_kosten_kinderen"]),
                            BijdrageTemplate = reader["bijdrage_template"] == DBNull.Value ? null : (int?)reader["bijdrage_template"],
                            BijdrageTemplateOmschrijving = reader["bijdrage_template_omschrijving"] == DBNull.Value ? null : ConvertToString(reader["bijdrage_template_omschrijving"]),

                            // Kinderrekening velden - safely read (backwards compatible if columns don't exist yet)
                            StortingOuder1Kinderrekening = SafeReadDecimal(reader, "storting_ouder1_kinderrekening"),
                            StortingOuder2Kinderrekening = SafeReadDecimal(reader, "storting_ouder2_kinderrekening"),
                            KinderrekeningKostensoorten = SafeReadJsonArray(reader, "kinderrekening_kostensoorten"),
                            KinderrekeningMaximumOpname = SafeReadBoolean(reader, "kinderrekening_maximum_opname"),
                            KinderrekeningMaximumOpnameBedrag = SafeReadDecimal(reader, "kinderrekening_maximum_opname_bedrag"),
                            KinderbijslagStortenOpKinderrekening = SafeReadBoolean(reader, "kinderbijslag_storten_op_kinderrekening"),
                            KindgebondenBudgetStortenOpKinderrekening = SafeReadBoolean(reader, "kindgebonden_budget_storten_op_kinderrekening"),

                            // Alimentatie settings - safely read (backwards compatible if columns don't exist yet)
                            BedragenAlleKinderenGelijk = SafeReadBoolean(reader, "bedragen_alle_kinderen_gelijk"),
                            AlimentatiebedragPerKind = SafeReadDecimal(reader, "alimentatiebedrag_per_kind"),
                            Alimentatiegerechtigde = SafeReadString(reader, "alimentatiegerechtigde"),
                            ZorgkortingPercentageAlleKinderen = SafeReadDecimal(reader, "zorgkorting_percentage_alle_kinderen"),

                            // Sync settings for all children - safely read (backwards compatible if columns don't exist yet)
                            AfsprakenAlleKinderenGelijk = SafeReadBoolean(reader, "afspraken_alle_kinderen_gelijk"),
                            HoofdverblijfAlleKinderen = SafeReadString(reader, "hoofdverblijf_alle_kinderen"),
                            InschrijvingAlleKinderen = SafeReadString(reader, "inschrijving_alle_kinderen"),
                            KinderbijslagOntvangerAlleKinderen = SafeReadString(reader, "kinderbijslag_ontvanger_alle_kinderen"),
                            KindgebondenBudgetAlleKinderen = SafeReadString(reader, "kindgebonden_budget_alle_kinderen"),

                            // Kinderrekening opheffing en alimentatie ingangsdatum velden
                            KinderrekeningOpheffen = SafeReadString(reader, "kinderrekening_opheffen"),
                            IngangsdatumOptie = SafeReadString(reader, "ingangsdatum_optie"),
                            Ingangsdatum = SafeReadDateTime(reader, "ingangsdatum"),
                            IngangsdatumAnders = SafeReadString(reader, "ingangsdatum_anders"),
                            EersteIndexeringJaar = SafeReadInt(reader, "eerste_indexering_jaar")
                        };

                        var hasNewFields = ColumnExists(reader, "storting_ouder1_kinderrekening");
                        _logger.LogInformation("Loaded alimentatie: Gezinsinkomen={Gezinsinkomen}, KostenKinderen={Kosten}, HasNewKinderrekeningFields={HasNewFields}",
                            alimentatie.NettoBesteedbaarGezinsinkomen, alimentatie.KostenKinderen, hasNewFields);
                    }
                    else
                    {
                        _logger.LogInformation("No alimentatie data found for dossier {DossierId}", dossierId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Alimentatie tables may not exist yet, skipping alimentatie data");
                }
                dossier.Alimentatie = alimentatie;

                // Result set 8: Bijdragen kosten kinderen (Optional)
                await reader.NextResultAsync();
                var bijdragenKostenKinderen = new List<BijdrageKostenKinderenData>();
                try
                {
                    _logger.LogInformation("Reading bijdragen kosten kinderen for dossier {DossierId}, FieldCount: {FieldCount}", dossierId, reader.FieldCount);

                    if (reader.FieldCount > 0)
                    {
                        while (await reader.ReadAsync())
                        {
                            var bijdrage = new BijdrageKostenKinderenData
                            {
                                Id = (int)reader["id"],
                                AlimentatieId = (int)reader["alimentatie_id"],
                                PersonenId = (int)reader["personen_id"],
                                PersoonNaam = ConvertToString(reader["persoon_naam"]),
                                EigenAandeel = reader["eigen_aandeel"] == DBNull.Value ? null : (decimal?)reader["eigen_aandeel"]
                            };
                            bijdragenKostenKinderen.Add(bijdrage);
                            _logger.LogInformation("Loaded bijdrage: PersonId={PersonId}, Naam={Naam}, Aandeel={Aandeel}",
                                bijdrage.PersonenId, bijdrage.PersoonNaam, bijdrage.EigenAandeel);
                        }
                    }
                    _logger.LogInformation("Total bijdragen loaded: {Count}", bijdragenKostenKinderen.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bijdragen kosten kinderen table may not exist yet, skipping");
                }
                if (alimentatie != null)
                    alimentatie.BijdragenKostenKinderen = bijdragenKostenKinderen;

                // Result set 9: Financiele afspraken kinderen (Optional)
                await reader.NextResultAsync();
                var financieleAfsprakenKinderen = new List<FinancieleAfsprakenKinderenData>();
                try
                {
                    _logger.LogInformation("Reading financiele afspraken kinderen for dossier {DossierId}, FieldCount: {FieldCount}", dossierId, reader.FieldCount);

                    if (reader.FieldCount > 0)
                    {
                        while (await reader.ReadAsync())
                        {
                            var afspraak = new FinancieleAfsprakenKinderenData
                            {
                                Id = (int)reader["id"],
                                AlimentatieId = (int)reader["alimentatie_id"],
                                KindId = (int)reader["kind_id"],
                                KindNaam = ConvertToString(reader["kind_naam"]),
                                AlimentatieBedrag = reader["alimentatie_bedrag"] == DBNull.Value ? null : Convert.ToDecimal(reader["alimentatie_bedrag"]),
                                Hoofdverblijf = reader["hoofdverblijf"] == DBNull.Value ? null : ConvertToString(reader["hoofdverblijf"]),
                                KinderbijslagOntvanger = reader["kinderbijslag_ontvanger"] == DBNull.Value ? null : ConvertToString(reader["kinderbijslag_ontvanger"]),
                                ZorgkortingPercentage = reader["zorgkorting_percentage"] == DBNull.Value ? null : Convert.ToDecimal(reader["zorgkorting_percentage"]),
                                Inschrijving = reader["inschrijving"] == DBNull.Value ? null : ConvertToString(reader["inschrijving"]),
                                KindgebondenBudget = reader["kindgebonden_budget"] == DBNull.Value ? null : ConvertToString(reader["kindgebonden_budget"])
                            };
                            financieleAfsprakenKinderen.Add(afspraak);
                            _logger.LogInformation("Loaded financiele afspraak: KindId={KindId}, Naam={Naam}, Bedrag={Bedrag}, Hoofdverblijf={Hoofdverblijf}",
                                afspraak.KindId, afspraak.KindNaam, afspraak.AlimentatieBedrag, afspraak.Hoofdverblijf);
                        }
                    }
                    _logger.LogInformation("Total financiele afspraken loaded: {Count}", financieleAfsprakenKinderen.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Financiele afspraken kinderen table may not exist yet, skipping");
                }
                if (alimentatie != null)
                    alimentatie.FinancieleAfsprakenKinderen = financieleAfsprakenKinderen;

                // Result set 10: Ouderschapsplan info (Optional)
                await reader.NextResultAsync();
                OuderschapsplanInfoData? ouderschapsplanInfo = null;
                try
                {
                    if (reader.FieldCount > 0 && await reader.ReadAsync())
                    {
                        ouderschapsplanInfo = new OuderschapsplanInfoData
                        {
                            Id = SafeReadInt(reader, "id") ?? 0,
                            DossierId = SafeReadInt(reader, "dossier_id") ?? 0,
                            Partij1PersoonId = SafeReadInt(reader, "partij_1_persoon_id") ?? 0,
                            Partij2PersoonId = SafeReadInt(reader, "partij_2_persoon_id") ?? 0,
                            SoortRelatie = SafeReadString(reader, "soort_relatie"),
                            DatumAanvangRelatie = SafeReadDateTime(reader, "datum_aanvang_relatie"),
                            PlaatsRelatie = SafeReadString(reader, "plaats_relatie"),
                            SoortRelatieVerbreking = SafeReadString(reader, "soort_relatie_verbreking"),
                            BetrokkenheidKind = SafeReadString(reader, "betrokkenheid_kind"),
                            Kiesplan = SafeReadString(reader, "kiesplan"),
                            GezagPartij = SafeReadInt(reader, "gezag_partij"),
                            GezagTermijnWeken = SafeReadInt(reader, "gezag_termijn_weken"),
                            WaOpNaamVanPartij = SafeReadInt(reader, "wa_op_naam_van_partij"),
                            KeuzeDevices = SafeReadString(reader, "keuze_devices"),
                            ZorgverzekeringOpNaamVanPartij = SafeReadInt(reader, "zorgverzekering_op_naam_van_partij"),
                            KinderbijslagPartij = SafeReadInt(reader, "kinderbijslag_partij"),
                            WoonplaatsOptie = SafeReadInt(reader, "woonplaats_optie"),
                            WoonplaatsPartij1 = SafeReadString(reader, "woonplaats_partij1"),
                            WoonplaatsPartij2 = SafeReadString(reader, "woonplaats_partij2"),
                            BrpPartij1 = SafeReadString(reader, "brp_partij_1"),
                            BrpPartij2 = SafeReadString(reader, "brp_partij_2"),
                            KgbPartij1 = SafeReadString(reader, "kgb_partij_1"),
                            KgbPartij2 = SafeReadString(reader, "kgb_partij_2"),
                            Hoofdverblijf = SafeReadString(reader, "hoofdverblijf"),
                            Zorgverdeling = SafeReadString(reader, "zorgverdeling"),
                            OpvangKinderen = SafeReadString(reader, "opvang_kinderen"),
                            BankrekeningnummersOpNaamVanKind = SafeReadString(reader, "bankrekeningnummers_op_naam_van_kind"),
                            ParentingCoordinator = SafeReadString(reader, "parenting_coordinator"),
                            
                            // Note: GezagZin, RelatieAanvangZin and OuderschapsplanDoelZin 
                            // are generated dynamically and not stored in database
                            
                            CreatedAt = SafeReadDateTime(reader, "created_at") ?? DateTime.Now,
                            UpdatedAt = SafeReadDateTime(reader, "updated_at") ?? DateTime.Now
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ouderschapsplan info table may not exist yet, skipping");
                }
                dossier.OuderschapsplanInfo = ouderschapsplanInfo;

                // Result set 11: Communicatie Afspraken (Optional)
                await reader.NextResultAsync();
                CommunicatieAfsprakenData? communicatieAfspraken = null;
                try
                {
                    if (reader.FieldCount > 0 && await reader.ReadAsync())
                    {
                        communicatieAfspraken = new CommunicatieAfsprakenData
                        {
                            Id = SafeReadInt(reader, "id") ?? 0,
                            DossierId = SafeReadInt(reader, "dossier_id") ?? 0,
                            VillaPinedoKinderen = SafeReadString(reader, "villa_pinedo_kinderen"),
                            KinderenBetrokkenheid = SafeReadString(reader, "kinderen_betrokkenheid"),
                            KiesMethode = SafeReadString(reader, "kies_methode"),
                            Opvang = SafeReadString(reader, "opvang"),
                            InformatieUitwisseling = SafeReadString(reader, "informatie_uitwisseling"),
                            BijlageBeslissingen = SafeReadString(reader, "bijlage_beslissingen"),
                            SocialMedia = SafeReadString(reader, "social_media"),
                            MobielTablet = SafeReadString(reader, "mobiel_tablet"),
                            ToezichtApps = SafeReadString(reader, "toezicht_apps"),
                            LocatieDelen = SafeReadString(reader, "locatie_delen"),
                            IdBewijzen = SafeReadString(reader, "id_bewijzen"),
                            Aansprakelijkheidsverzekering = SafeReadString(reader, "aansprakelijkheidsverzekering"),
                            Ziektekostenverzekering = SafeReadString(reader, "ziektekostenverzekering"),
                            ToestemmingReizen = SafeReadString(reader, "toestemming_reizen"),
                            Jongmeerderjarige = SafeReadString(reader, "jongmeerderjarige"),
                            Studiekosten = SafeReadString(reader, "studiekosten"),
                            BankrekeningKinderen = SafeReadString(reader, "bankrekening_kinderen"),
                            Evaluatie = SafeReadString(reader, "evaluatie"),
                            ParentingCoordinator = SafeReadString(reader, "parenting_coordinator"),
                            MediationClausule = SafeReadString(reader, "mediation_clausule")
                        };
                        _logger.LogInformation("Loaded CommunicatieAfspraken for dossier {DossierId}", dossierId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Communicatie afspraken table may not exist yet, skipping");
                }
                dossier.CommunicatieAfspraken = communicatieAfspraken;

                // Result set 12: Omgangsregeling - Read omgang_tekst_of_schema from separate table
                await reader.NextResultAsync();
                try
                {
                    if (reader.FieldCount > 0 && await reader.ReadAsync())
                    {
                        // Create communicatieAfspraken if it doesn't exist yet
                        if (communicatieAfspraken == null)
                        {
                            communicatieAfspraken = new CommunicatieAfsprakenData
                            {
                                DossierId = actualDossierId
                            };
                        }

                        // Populate omgang fields from omgangsregeling table
                        communicatieAfspraken.OmgangTekstOfSchema = SafeReadString(reader, "omgang_tekst_of_schema");
                        communicatieAfspraken.OmgangBeschrijving = SafeReadString(reader, "omgang_beschrijving");

                        // Update dossier reference
                        dossier.CommunicatieAfspraken = communicatieAfspraken;

                        _logger.LogInformation("Loaded Omgangsregeling data for dossier {DossierId}", dossierId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Omgangsregeling table may not exist yet, skipping");
                }

                _logger.LogInformation("Successfully retrieved dossier data for dossier ID: {DossierId}", dossierId);
                return dossier;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error while retrieving dossier data for dossier ID: {DossierId}", dossierId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving dossier data for dossier ID: {DossierId}", dossierId);
                throw;
            }
        }


        private static PersonData MapPersonData(SqlDataReader reader)
        {
            return new PersonData
            {
                Id = (int)reader["id"],
                Voorletters = reader["voorletters"] == DBNull.Value ? null : ConvertToString(reader["voorletters"]),
                Voornamen = reader["voornamen"] == DBNull.Value ? null : ConvertToString(reader["voornamen"]),
                Roepnaam = reader["roepnaam"] == DBNull.Value ? null : ConvertToString(reader["roepnaam"]),
                Geslacht = reader["geslacht"] == DBNull.Value ? null : ConvertToString(reader["geslacht"]),
                Tussenvoegsel = reader["tussenvoegsel"] == DBNull.Value ? null : ConvertToString(reader["tussenvoegsel"]),
                Achternaam = ConvertToString(reader["achternaam"]),
                Adres = reader["adres"] == DBNull.Value ? null : ConvertToString(reader["adres"]),
                Postcode = reader["postcode"] == DBNull.Value ? null : ConvertToString(reader["postcode"]),
                Plaats = reader["plaats"] == DBNull.Value ? null : ConvertToString(reader["plaats"]),
                GeboortePlaats = reader["geboorteplaats"] == DBNull.Value ? null : ConvertToString(reader["geboorteplaats"]),
                GeboorteDatum = reader["geboorte_datum"] == DBNull.Value ? null : (DateTime?)reader["geboorte_datum"],
                Nationaliteit1 = reader["nationaliteit_1"] == DBNull.Value ? null : ConvertToString(reader["nationaliteit_1"]),
                Nationaliteit2 = reader["nationaliteit_2"] == DBNull.Value ? null : ConvertToString(reader["nationaliteit_2"]),
                Telefoon = reader["telefoon"] == DBNull.Value ? null : ConvertToString(reader["telefoon"]),
                Email = reader["email"] == DBNull.Value ? null : ConvertToString(reader["email"]),
                Beroep = reader["beroep"] == DBNull.Value ? null : ConvertToString(reader["beroep"]),
                RolId = reader["rol_id"] == DBNull.Value ? null : (int?)reader["rol_id"],
                RolNaam = reader["rol_naam"] == DBNull.Value ? null : ConvertToString(reader["rol_naam"])
            };
        }

        private static ChildData MapChildData(SqlDataReader reader)
        {
            return new ChildData
            {
                Id = (int)reader["id"],
                Voorletters = reader["voorletters"] == DBNull.Value ? null : ConvertToString(reader["voorletters"]),
                Voornamen = reader["voornamen"] == DBNull.Value ? null : ConvertToString(reader["voornamen"]),
                Roepnaam = reader["roepnaam"] == DBNull.Value ? null : ConvertToString(reader["roepnaam"]),
                Geslacht = reader["geslacht"] == DBNull.Value ? null : ConvertToString(reader["geslacht"]),
                Tussenvoegsel = reader["tussenvoegsel"] == DBNull.Value ? null : ConvertToString(reader["tussenvoegsel"]),
                Achternaam = ConvertToString(reader["achternaam"]),
                Adres = reader["adres"] == DBNull.Value ? null : ConvertToString(reader["adres"]),
                Postcode = reader["postcode"] == DBNull.Value ? null : ConvertToString(reader["postcode"]),
                Plaats = reader["plaats"] == DBNull.Value ? null : ConvertToString(reader["plaats"]),
                GeboortePlaats = reader["geboorteplaats"] == DBNull.Value ? null : ConvertToString(reader["geboorteplaats"]),
                GeboorteDatum = reader["geboorte_datum"] == DBNull.Value ? null : (DateTime?)reader["geboorte_datum"],
                Nationaliteit1 = reader["nationaliteit_1"] == DBNull.Value ? null : ConvertToString(reader["nationaliteit_1"]),
                Nationaliteit2 = reader["nationaliteit_2"] == DBNull.Value ? null : ConvertToString(reader["nationaliteit_2"]),
                Telefoon = reader["telefoon"] == DBNull.Value ? null : ConvertToString(reader["telefoon"]),
                Email = reader["email"] == DBNull.Value ? null : ConvertToString(reader["email"]),
                Beroep = reader["beroep"] == DBNull.Value ? null : ConvertToString(reader["beroep"])
            };
        }

        /// <summary>
        /// Safely converts database values to string, handling booleans and other types
        /// </summary>
        private static string ConvertToString(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            // Handle boolean values
            if (value is bool boolValue)
                return boolValue ? "Ja" : "Nee";

            // Default to string conversion
            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Checks if a column exists in the data reader
        /// </summary>
        private static bool ColumnExists(SqlDataReader reader, string columnName)
        {
            try
            {
                return reader.GetOrdinal(columnName) >= 0;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Safely reads a nullable decimal value from the reader, returns null if column doesn't exist
        /// </summary>
        private static decimal? SafeReadDecimal(SqlDataReader reader, string columnName)
        {
            if (!ColumnExists(reader, columnName))
                return null;

            var value = reader[columnName];
            return value == DBNull.Value ? null : Convert.ToDecimal(value);
        }

        /// <summary>
        /// Safely reads a nullable boolean value from the reader, returns null if column doesn't exist
        /// </summary>
        private static bool? SafeReadBoolean(SqlDataReader reader, string columnName)
        {
            if (!ColumnExists(reader, columnName))
                return null;

            var value = reader[columnName];
            return value == DBNull.Value ? null : (bool?)value;
        }

        /// <summary>
        /// Resolves a dossier ID or nummer to the actual dossier ID
        /// If the input is already a valid dossier ID, returns it
        /// If the input is a dossier nummer (string numeric), looks up the actual ID
        /// </summary>
        private async Task<int> ResolveDossierIdAsync(SqlConnection connection, int inputId)
        {
            // First, try to find a dossier with this ID
            const string checkIdQuery = "SELECT id FROM dbo.dossiers WHERE id = @InputId";
            using var checkIdCommand = new SqlCommand(checkIdQuery, connection);
            checkIdCommand.Parameters.AddWithValue("@InputId", inputId);

            var resultById = await checkIdCommand.ExecuteScalarAsync();
            if (resultById != null)
            {
                // Found by ID - this is the normal case, no need to log
                return inputId;
            }

            // If not found by ID, try to find by dossier_nummer
            const string checkNummerQuery = "SELECT id FROM dbo.dossiers WHERE dossier_nummer = @DossierNummer";
            using var checkNummerCommand = new SqlCommand(checkNummerQuery, connection);
            checkNummerCommand.Parameters.AddWithValue("@DossierNummer", inputId.ToString());

            var resultByNummer = await checkNummerCommand.ExecuteScalarAsync();
            if (resultByNummer != null)
            {
                int actualId = Convert.ToInt32(resultByNummer);
                _logger.LogInformation("Resolved dossier nummer {DossierNummer} to ID {ActualId}", inputId, actualId);
                return actualId;
            }

            // If still not found, return the original input (will fail later with proper error)
            _logger.LogWarning("Dossier not found by ID or nummer: {InputId}", inputId);
            return inputId;
        }

        /// <summary>
        /// Safely reads a nullable string value from the reader, returns null if column doesn't exist
        /// </summary>
        private static string? SafeReadString(SqlDataReader reader, string columnName)
        {
            if (!ColumnExists(reader, columnName))
                return null;

            var value = reader[columnName];
            return value == DBNull.Value ? null : ConvertToString(value);
        }

        /// <summary>
        /// Safely reads a nullable integer value from the reader, returns null if column doesn't exist
        /// Handles both INT and TINYINT (byte) database types
        /// </summary>
        private static int? SafeReadInt(SqlDataReader reader, string columnName)
        {
            if (!ColumnExists(reader, columnName))
                return null;

            var value = reader[columnName];
            if (value == DBNull.Value)
                return null;

            // Handle both INT (int) and TINYINT (byte) types
            return value switch
            {
                int intValue => intValue,
                byte byteValue => (int)byteValue,
                short shortValue => (int)shortValue,
                long longValue => (int)longValue,
                _ => Convert.ToInt32(value)
            };
        }

        /// <summary>
        /// Safely reads a nullable DateTime value from the reader, returns null if column doesn't exist
        /// </summary>
        private static DateTime? SafeReadDateTime(SqlDataReader reader, string columnName)
        {
            if (!ColumnExists(reader, columnName))
                return null;

            var value = reader[columnName];
            return value == DBNull.Value ? null : (DateTime?)value;
        }

        /// <summary>
        /// Parses a JSON array of strings from database value
        /// </summary>
        private static List<string> ParseJsonStringArray(object value)
        {
            if (value == null || value == DBNull.Value)
                return new List<string>();

            var jsonString = value.ToString();
            if (string.IsNullOrWhiteSpace(jsonString))
                return new List<string>();

            try
            {
                var result = JsonSerializer.Deserialize<List<string>>(jsonString);
                return result ?? new List<string>();
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return empty list
                return new List<string>();
            }
        }

        /// <summary>
        /// Safely reads a JSON array from the reader, returns empty list if column doesn't exist
        /// </summary>
        private static List<string> SafeReadJsonArray(SqlDataReader reader, string columnName)
        {
            if (!ColumnExists(reader, columnName))
                return new List<string>();

            return ParseJsonStringArray(reader[columnName]);
        }

        /// <summary>
        /// Gets available template types from the database
        /// </summary>
        /// <returns>List of available template types</returns>
        public async Task<List<string>> GetAvailableTemplateTypesAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving available template types");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Query distinct types from regelingen_templates table
                const string query = @"
                    SELECT DISTINCT type 
                    FROM dbo.regelingen_templates 
                    WHERE type IS NOT NULL 
                    ORDER BY type";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var templateTypes = new List<string>();
                while (await reader.ReadAsync())
                {
                    var type = ConvertToString(reader["type"]);
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        templateTypes.Add(type);
                    }
                }

                // If no types found in database, return default types
                if (templateTypes.Count == 0)
                {
                    templateTypes.AddRange(TemplateTypes.DefaultTypes);
                    _logger.LogWarning("No template types found in database, using default types");
                }

                _logger.LogInformation($"Retrieved {templateTypes.Count} template types: {string.Join(", ", templateTypes)}");
                return templateTypes;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error while retrieving template types");
                // Return default types on error
                return new List<string>(TemplateTypes.DefaultTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving template types");
                // Return default types on error
                return new List<string>(TemplateTypes.DefaultTypes);
            }
        }

        /// <summary>
        /// Gets templates by type from the database
        /// </summary>
        /// <param name="templateType">The type of templates to retrieve (e.g., "Feestdag", "Vakantie", "Algemeen", "Bijzondere dag")</param>
        /// <returns>List of templates for the specified type</returns>
        public async Task<List<RegelingTemplate>> GetTemplatesByTypeAsync(string templateType)
        {
            try
            {
                _logger.LogInformation($"Retrieving templates for type: {templateType}");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Query templates by type
                const string query = @"
                    SELECT id, template_naam, template_tekst, meervoud_kinderen, type
                    FROM dbo.regelingen_templates 
                    WHERE type = @TemplateType
                    ORDER BY template_naam";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@TemplateType", templateType);
                
                using var reader = await command.ExecuteReaderAsync();
                
                var templates = new List<RegelingTemplate>();
                while (await reader.ReadAsync())
                {
                    templates.Add(new RegelingTemplate
                    {
                        Id = (int)reader["id"],
                        TemplateNaam = ConvertToString(reader["template_naam"]),
                        TemplateTekst = ConvertToString(reader["template_tekst"]),
                        MeervoudKinderen = (bool)reader["meervoud_kinderen"],
                        Type = ConvertToString(reader["type"])
                    });
                }

                _logger.LogInformation($"Retrieved {templates.Count} templates for type: {templateType}");
                return templates;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, $"Database error while retrieving templates for type: {templateType}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while retrieving templates for type: {templateType}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves artikelen (articles/clauses) for a dossier with 3-layer priority:
        /// Dossier override > Gebruiker aanpassing > Systeem template
        /// </summary>
        /// <param name="dossierId">The dossier ID</param>
        /// <param name="gebruikerId">The user ID (owner of the dossier)</param>
        /// <param name="documentType">The document type (e.g., "ouderschapsplan", "convenant")</param>
        /// <returns>List of articles with effective text based on priority</returns>
        public async Task<List<ArtikelData>> GetArtikelenVoorDossierAsync(int dossierId, int gebruikerId, string documentType = "ouderschapsplan")
        {
            try
            {
                _logger.LogInformation("Retrieving artikelen for dossier {DossierId}, user {GebruikerId}, type {DocumentType}",
                    dossierId, gebruikerId, documentType);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Query that joins artikel_templates with gebruiker_artikelen and dossier_artikelen
                // to get the effective text based on the 3-layer priority system
                const string query = @"
                    SELECT
                        t.id,
                        t.document_type,
                        t.artikel_code,
                        t.artikel_titel,
                        t.artikel_tekst,
                        t.volgorde,
                        t.is_verplicht,
                        t.is_conditioneel,
                        t.conditie_veld,
                        t.categorie,
                        t.help_tekst,
                        t.versie,
                        t.is_actief,
                        -- Gebruiker aanpassingen
                        ga.aangepaste_titel AS gebruiker_titel,
                        ga.aangepaste_tekst AS gebruiker_tekst,
                        ga.is_actief AS gebruiker_actief,
                        -- Dossier overrides
                        da.aangepaste_tekst AS dossier_tekst,
                        ISNULL(da.is_uitgesloten, 0) AS is_uitgesloten
                    FROM dbo.artikel_templates t
                    LEFT JOIN dbo.gebruiker_artikelen ga
                        ON t.id = ga.artikel_template_id
                        AND ga.gebruiker_id = @GebruikerId
                    LEFT JOIN dbo.dossier_artikelen da
                        ON t.id = da.artikel_template_id
                        AND da.dossier_id = @DossierId
                    WHERE t.document_type = @DocumentType
                        AND t.is_actief = 1
                        AND ISNULL(da.is_uitgesloten, 0) = 0
                    ORDER BY t.volgorde ASC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@DossierId", dossierId);
                command.Parameters.AddWithValue("@GebruikerId", gebruikerId);
                command.Parameters.AddWithValue("@DocumentType", documentType);

                using var reader = await command.ExecuteReaderAsync();

                var artikelen = new List<ArtikelData>();
                while (await reader.ReadAsync())
                {
                    artikelen.Add(new ArtikelData
                    {
                        Id = (int)reader["id"],
                        DocumentType = ConvertToString(reader["document_type"]),
                        ArtikelCode = ConvertToString(reader["artikel_code"]),
                        ArtikelTitel = ConvertToString(reader["artikel_titel"]),
                        ArtikelTekst = ConvertToString(reader["artikel_tekst"]),
                        Volgorde = (int)reader["volgorde"],
                        IsVerplicht = (bool)reader["is_verplicht"],
                        IsConditioneel = (bool)reader["is_conditioneel"],
                        ConditieVeld = reader["conditie_veld"] == DBNull.Value ? null : ConvertToString(reader["conditie_veld"]),
                        Categorie = reader["categorie"] == DBNull.Value ? null : ConvertToString(reader["categorie"]),
                        HelpTekst = reader["help_tekst"] == DBNull.Value ? null : ConvertToString(reader["help_tekst"]),
                        Versie = (int)reader["versie"],
                        IsActief = (bool)reader["is_actief"],
                        // Gebruiker aanpassingen
                        GebruikerTitel = reader["gebruiker_titel"] == DBNull.Value ? null : ConvertToString(reader["gebruiker_titel"]),
                        GebruikerTekst = reader["gebruiker_tekst"] == DBNull.Value ? null : ConvertToString(reader["gebruiker_tekst"]),
                        GebruikerActief = reader["gebruiker_actief"] == DBNull.Value ? null : (bool?)reader["gebruiker_actief"],
                        // Dossier overrides
                        DossierTekst = reader["dossier_tekst"] == DBNull.Value ? null : ConvertToString(reader["dossier_tekst"]),
                        IsUitgesloten = (bool)reader["is_uitgesloten"]
                    });
                }

                _logger.LogInformation("Retrieved {Count} artikelen for dossier {DossierId}", artikelen.Count, dossierId);
                return artikelen;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error while retrieving artikelen for dossier {DossierId}", dossierId);
                // Return empty list if table doesn't exist (backwards compatibility)
                if (ex.Message.Contains("Invalid object name") && ex.Message.Contains("artikel"))
                {
                    _logger.LogWarning("Artikel tables do not exist yet, returning empty list");
                    return new List<ArtikelData>();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving artikelen for dossier {DossierId}", dossierId);
                throw;
            }
        }
    }
}