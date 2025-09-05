using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using System;
using System.Collections.Generic;
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
        /// <param name="dossierId">The ID of the dossier to retrieve</param>
        /// <returns>Complete dossier data or null if not found</returns>
        public async Task<DossierData?> GetDossierDataAsync(int dossierId)
        {
            try
            {
                _logger.LogInformation("Retrieving dossier data for dossier ID: {DossierId}", dossierId);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

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
                               a.bijdrage_template, bt.omschrijving AS bijdrage_template_omschrijving
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
                        SELECT opi.id, opi.dossier_id, opi.partij_1_persoon_id, opi.partij_2_persoon_id,
                               opi.soort_relatie, opi.soort_relatie_verbreking, opi.betrokkenheid_kind, opi.kiesplan,
                               opi.gezag_partij, opi.wa_op_naam_van_partij, opi.keuze_devices, 
                               opi.zorgverzekering_op_naam_van_partij, opi.kinderbijslag_partij,
                               opi.brp_partij_1, opi.brp_partij_2, opi.kgb_partij_1, opi.kgb_partij_2,
                               opi.hoofdverblijf, opi.zorgverdeling, opi.opvang_kinderen,
                               opi.bankrekeningnummers_op_naam_van_kind, opi.parenting_coordinator,
                               opi.created_at, opi.updated_at
                        FROM dbo.ouderschapsplan_info opi
                        WHERE opi.dossier_id = @DossierId;
                    END
                    ELSE
                    BEGIN
                        SELECT NULL WHERE 1=0; -- Empty result set
                    END";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@DossierId", dossierId);

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
                    if (reader.FieldCount > 0 && await reader.ReadAsync())
                    {
                        alimentatie = new AlimentatieData
                        {
                            Id = (int)reader["id"],
                            DossierId = (int)reader["dossier_id"],
                            NettoBesteedbaarGezinsinkomen = reader["netto_besteedbaar_gezinsinkomen"] == DBNull.Value ? null : (decimal?)reader["netto_besteedbaar_gezinsinkomen"],
                            KostenKinderen = reader["kosten_kinderen"] == DBNull.Value ? null : (decimal?)reader["kosten_kinderen"],
                            BijdrageKostenKinderen = reader["bijdrage_kosten_kinderen"] == DBNull.Value ? null : (decimal?)reader["bijdrage_kosten_kinderen"],
                            BijdrageTemplate = reader["bijdrage_template"] == DBNull.Value ? null : (int?)reader["bijdrage_template"],
                            BijdrageTemplateOmschrijving = reader["bijdrage_template_omschrijving"] == DBNull.Value ? null : ConvertToString(reader["bijdrage_template_omschrijving"])
                        };
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
                    if (reader.FieldCount > 0)
                    {
                        while (await reader.ReadAsync())
                        {
                            bijdragenKostenKinderen.Add(new BijdrageKostenKinderenData
                            {
                                Id = (int)reader["id"],
                                AlimentatieId = (int)reader["alimentatie_id"],
                                PersonenId = (int)reader["personen_id"],
                                PersoonNaam = ConvertToString(reader["persoon_naam"]),
                                EigenAandeel = reader["eigen_aandeel"] == DBNull.Value ? null : (decimal?)reader["eigen_aandeel"]
                            });
                        }
                    }
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
                    if (reader.FieldCount > 0)
                    {
                        while (await reader.ReadAsync())
                        {
                            financieleAfsprakenKinderen.Add(new FinancieleAfsprakenKinderenData
                            {
                                Id = (int)reader["id"],
                                AlimentatieId = (int)reader["alimentatie_id"],
                                KindId = (int)reader["kind_id"],
                                KindNaam = ConvertToString(reader["kind_naam"]),
                                AlimentatieBedrag = reader["alimentatie_bedrag"] == DBNull.Value ? null : (decimal?)reader["alimentatie_bedrag"],
                                Hoofdverblijf = reader["hoofdverblijf"] == DBNull.Value ? null : (int?)reader["hoofdverblijf"],
                                KinderbijslagOntvanger = reader["kinderbijslag_ontvanger"] == DBNull.Value ? null : (int?)reader["kinderbijslag_ontvanger"],
                                ZorgkortingPercentage = reader["zorgkorting_percentage"] == DBNull.Value ? null : (decimal?)reader["zorgkorting_percentage"],
                                Inschrijving = reader["inschrijving"] == DBNull.Value ? null : (int?)reader["inschrijving"],
                                KindgebondenBudget = reader["kindgebonden_budget"] == DBNull.Value ? null : (int?)reader["kindgebonden_budget"]
                            });
                        }
                    }
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
                            Id = (int)reader["id"],
                            DossierId = (int)reader["dossier_id"],
                            Partij1PersoonId = (int)reader["partij_1_persoon_id"],
                            Partij2PersoonId = (int)reader["partij_2_persoon_id"],
                            SoortRelatie = reader["soort_relatie"] == DBNull.Value ? null : ConvertToString(reader["soort_relatie"]),
                            SoortRelatieVerbreking = reader["soort_relatie_verbreking"] == DBNull.Value ? null : ConvertToString(reader["soort_relatie_verbreking"]),
                            BetrokkenheidKind = reader["betrokkenheid_kind"] == DBNull.Value ? null : ConvertToString(reader["betrokkenheid_kind"]),
                            Kiesplan = reader["kiesplan"] == DBNull.Value ? null : ConvertToString(reader["kiesplan"]),
                            GezagPartij = reader["gezag_partij"] == DBNull.Value ? null : (int?)reader["gezag_partij"],
                            WaOpNaamVanPartij = reader["wa_op_naam_van_partij"] == DBNull.Value ? null : (int?)reader["wa_op_naam_van_partij"],
                            KeuzeDevices = reader["keuze_devices"] == DBNull.Value ? null : ConvertToString(reader["keuze_devices"]),
                            ZorgverzekeringOpNaamVanPartij = reader["zorgverzekering_op_naam_van_partij"] == DBNull.Value ? null : (int?)reader["zorgverzekering_op_naam_van_partij"],
                            KinderbijslagPartij = reader["kinderbijslag_partij"] == DBNull.Value ? null : (int?)reader["kinderbijslag_partij"],
                            BrpPartij1 = reader["brp_partij_1"] == DBNull.Value ? null : ConvertToString(reader["brp_partij_1"]),
                            BrpPartij2 = reader["brp_partij_2"] == DBNull.Value ? null : ConvertToString(reader["brp_partij_2"]),
                            KgbPartij1 = reader["kgb_partij_1"] == DBNull.Value ? null : ConvertToString(reader["kgb_partij_1"]),
                            KgbPartij2 = reader["kgb_partij_2"] == DBNull.Value ? null : ConvertToString(reader["kgb_partij_2"]),
                            Hoofdverblijf = reader["hoofdverblijf"] == DBNull.Value ? null : ConvertToString(reader["hoofdverblijf"]),
                            Zorgverdeling = reader["zorgverdeling"] == DBNull.Value ? null : ConvertToString(reader["zorgverdeling"]),
                            OpvangKinderen = reader["opvang_kinderen"] == DBNull.Value ? null : ConvertToString(reader["opvang_kinderen"]),
                            BankrekeningnummersOpNaamVanKind = reader["bankrekeningnummers_op_naam_van_kind"] == DBNull.Value ? null : ConvertToString(reader["bankrekeningnummers_op_naam_van_kind"]),
                            ParentingCoordinator = reader["parenting_coordinator"] == DBNull.Value ? null : ConvertToString(reader["parenting_coordinator"]),
                            CreatedAt = (DateTime)reader["created_at"],
                            UpdatedAt = (DateTime)reader["updated_at"]
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ouderschapsplan info table may not exist yet, skipping");
                }
                dossier.OuderschapsplanInfo = ouderschapsplanInfo;

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
    }
}