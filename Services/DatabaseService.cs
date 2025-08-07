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

                // Get basic dossier information
                var dossier = await GetDossierAsync(connection, dossierId);
                if (dossier == null)
                {
                    _logger.LogWarning("Dossier with ID {DossierId} not found", dossierId);
                    return null;
                }

                // Get all related data in parallel
                var partiesTask = GetPartiesAsync(connection, dossierId);
                var childrenTask = GetChildrenAsync(connection, dossierId);
                var visitationTask = GetVisitationArrangementsAsync(connection, dossierId);
                var careTask = GetCareArrangementsAsync(connection, dossierId);

                await Task.WhenAll(partiesTask, childrenTask, visitationTask, careTask);

                dossier.Partijen = partiesTask.Result;
                dossier.Kinderen = childrenTask.Result;
                dossier.Omgang = visitationTask.Result;
                dossier.Zorg = careTask.Result;

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

        private async Task<DossierData?> GetDossierAsync(SqlConnection connection, int dossierId)
        {
            const string query = @"
                SELECT id, dossier_nummer, aangemaakt_op, gewijzigd_op, status, gebruiker_id
                FROM dbo.dossiers 
                WHERE id = @DossierId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DossierId", dossierId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new DossierData
            {
                Id = (int)reader["id"],
                DossierNummer = (string)reader["dossier_nummer"],
                AangemaaktOp = (DateTime)reader["aangemaakt_op"],
                GewijzigdOp = (DateTime)reader["gewijzigd_op"],
                Status = (string)reader["status"],
                GebruikerId = (int)reader["gebruiker_id"]
            };
        }

        private async Task<List<PersonData>> GetPartiesAsync(SqlConnection connection, int dossierId)
        {
            const string query = @"
                SELECT p.id, p.voorletters, p.voornamen, p.roepnaam, p.geslacht, 
                       p.tussenvoegsel, p.achternaam, p.adres, p.postcode, p.plaats,
                       p.geboorte_plaats, p.geboorte_datum, p.nationaliteit_1, p.nationaliteit_2,
                       p.telefoon, p.email, p.beroep, dp.rol_id, r.naam as rol_naam
                FROM dbo.personen p
                INNER JOIN dbo.dossiers_partijen dp ON p.id = dp.persoon_id
                INNER JOIN dbo.rollen r ON dp.rol_id = r.id
                WHERE dp.dossier_id = @DossierId AND dp.rol_id IN (1, 2)
                ORDER BY dp.rol_id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DossierId", dossierId);

            var parties = new List<PersonData>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                parties.Add(MapPersonData(reader));
            }

            return parties;
        }

        private async Task<List<ChildData>> GetChildrenAsync(SqlConnection connection, int dossierId)
        {
            const string query = @"
                SELECT p.id, p.voorletters, p.voornamen, p.roepnaam, p.geslacht, 
                       p.tussenvoegsel, p.achternaam, p.adres, p.postcode, p.plaats,
                       p.geboorte_plaats, p.geboorte_datum, p.nationaliteit_1, p.nationaliteit_2,
                       p.telefoon, p.email, p.beroep
                FROM dbo.personen p
                INNER JOIN dbo.dossiers_kinderen dk ON p.id = dk.kind_id
                WHERE dk.dossier_id = @DossierId
                ORDER BY p.geboorte_datum DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DossierId", dossierId);

            var children = new List<ChildData>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var child = MapChildData(reader);
                children.Add(child);
            }

            // Get parent relationships for each child
            foreach (var child in children)
            {
                child.ParentRelations = await GetParentChildRelationsAsync(connection, child.Id);
            }

            return children;
        }

        private async Task<List<ParentChildRelation>> GetParentChildRelationsAsync(SqlConnection connection, int childId)
        {
            const string query = @"
                SELECT ko.ouder_id, p.voornamen + ' ' + ISNULL(p.tussenvoegsel + ' ', '') + p.achternaam as ouder_naam,
                       ko.relatie_type_id, rt.naam as relatie_type
                FROM dbo.kinderen_ouders ko
                INNER JOIN dbo.personen p ON ko.ouder_id = p.id
                INNER JOIN dbo.relatie_types rt ON ko.relatie_type_id = rt.id
                WHERE ko.kind_id = @ChildId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ChildId", childId);

            var relations = new List<ParentChildRelation>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                relations.Add(new ParentChildRelation
                {
                    OuderId = (int)reader["ouder_id"],
                    OuderNaam = (string)reader["ouder_naam"],
                    RelatieTypeId = (int)reader["relatie_type_id"],
                    RelatieType = reader["relatie_type"] == DBNull.Value ? null : (string)reader["relatie_type"]
                });
            }

            return relations;
        }

        private async Task<List<OmgangData>> GetVisitationArrangementsAsync(SqlConnection connection, int dossierId)
        {
            const string query = @"
                SELECT o.id, o.dag_id, d.naam AS dag_naam, o.dagdeel_id, dd.naam AS dagdeel_naam,
                       o.verzorger_id, p.voornamen + ' ' + ISNULL(p.tussenvoegsel + ' ', '') + p.achternaam AS verzorger_naam,
                       o.wissel_tijd, o.week_regeling_id, wr.omschrijving AS week_regeling_omschrijving,
                       o.week_regeling_anders, o.dossier_id, o.aangemaakt_op, o.gewijzigd_op
                FROM dbo.omgang o
                INNER JOIN dbo.dagen d ON o.dag_id = d.id
                INNER JOIN dbo.dagdelen dd ON o.dagdeel_id = dd.id
                INNER JOIN dbo.personen p ON o.verzorger_id = p.id
                INNER JOIN dbo.week_regelingen wr ON o.week_regeling_id = wr.id
                WHERE o.dossier_id = @DossierId
                ORDER BY d.id, dd.id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DossierId", dossierId);

            var arrangements = new List<OmgangData>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                arrangements.Add(new OmgangData
                {
                    Id = (int)reader["id"],
                    DagId = (int)reader["dag_id"],
                    DagNaam = (string)reader["dag_naam"],
                    DagdeelId = (int)reader["dagdeel_id"],
                    DagdeelNaam = (string)reader["dagdeel_naam"],
                    VerzorgerId = (int)reader["verzorger_id"],
                    VerzorgerNaam = (string)reader["verzorger_naam"],
                    WisselTijd = reader["wissel_tijd"] == DBNull.Value ? null : (string)reader["wissel_tijd"],
                    WeekRegelingId = (int)reader["week_regeling_id"],
                    WeekRegelingOmschrijving = (string)reader["week_regeling_omschrijving"],
                    WeekRegelingAnders = reader["week_regeling_anders"] == DBNull.Value ? null : (string)reader["week_regeling_anders"],
                    DossierId = (int)reader["dossier_id"],
                    AangemaaktOp = (DateTime)reader["aangemaakt_op"],
                    GewijzigdOp = (DateTime)reader["gewijzigd_op"]
                });
            }

            return arrangements;
        }

        private async Task<List<ZorgData>> GetCareArrangementsAsync(SqlConnection connection, int dossierId)
        {
            const string query = @"
                SELECT z.id, z.zorg_categorie_id, zc.naam AS zorg_categorie_naam,
                       z.zorg_situatie_id, zs.naam AS zorg_situatie_naam,
                       z.overeenkomst, z.situatie_anders, z.dossier_id,
                       z.aangemaakt_op, z.aangemaakt_door, z.gewijzigd_op, z.gewijzigd_door
                FROM dbo.zorg z
                INNER JOIN dbo.zorg_categorieen zc ON z.zorg_categorie_id = zc.id
                INNER JOIN dbo.zorg_situaties zs ON z.zorg_situatie_id = zs.id
                WHERE z.dossier_id = @DossierId
                ORDER BY zc.naam, zs.naam";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DossierId", dossierId);

            var arrangements = new List<ZorgData>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                arrangements.Add(new ZorgData
                {
                    Id = (int)reader["id"],
                    ZorgCategorieId = (int)reader["zorg_categorie_id"],
                    ZorgCategorieNaam = (string)reader["zorg_categorie_naam"],
                    ZorgSituatieId = (int)reader["zorg_situatie_id"],
                    ZorgSituatieNaam = (string)reader["zorg_situatie_naam"],
                    Overeenkomst = (string)reader["overeenkomst"],
                    SituatieAnders = reader["situatie_anders"] == DBNull.Value ? null : (string)reader["situatie_anders"],
                    DossierId = (int)reader["dossier_id"],
                    AangemaaktOp = (DateTime)reader["aangemaakt_op"],
                    AangemaaktDoor = (int)reader["aangemaakt_door"],
                    GewijzigdOp = (DateTime)reader["gewijzigd_op"],
                    GewijzigdDoor = reader["gewijzigd_door"] == DBNull.Value ? null : (int?)reader["gewijzigd_door"]
                });
            }

            return arrangements;
        }

        private static PersonData MapPersonData(SqlDataReader reader)
        {
            return new PersonData
            {
                Id = (int)reader["id"],
                Voorletters = reader["voorletters"] == DBNull.Value ? null : (string)reader["voorletters"],
                Voornamen = reader["voornamen"] == DBNull.Value ? null : (string)reader["voornamen"],
                Roepnaam = reader["roepnaam"] == DBNull.Value ? null : (string)reader["roepnaam"],
                Geslacht = reader["geslacht"] == DBNull.Value ? null : (string)reader["geslacht"],
                Tussenvoegsel = reader["tussenvoegsel"] == DBNull.Value ? null : (string)reader["tussenvoegsel"],
                Achternaam = (string)reader["achternaam"],
                Adres = reader["adres"] == DBNull.Value ? null : (string)reader["adres"],
                Postcode = reader["postcode"] == DBNull.Value ? null : (string)reader["postcode"],
                Plaats = reader["plaats"] == DBNull.Value ? null : (string)reader["plaats"],
                GeboortePlaats = reader["geboorte_plaats"] == DBNull.Value ? null : (string)reader["geboorte_plaats"],
                GeboorteDatum = reader["geboorte_datum"] == DBNull.Value ? null : (DateTime?)reader["geboorte_datum"],
                Nationaliteit1 = reader["nationaliteit_1"] == DBNull.Value ? null : (string)reader["nationaliteit_1"],
                Nationaliteit2 = reader["nationaliteit_2"] == DBNull.Value ? null : (string)reader["nationaliteit_2"],
                Telefoon = reader["telefoon"] == DBNull.Value ? null : (string)reader["telefoon"],
                Email = reader["email"] == DBNull.Value ? null : (string)reader["email"],
                Beroep = reader["beroep"] == DBNull.Value ? null : (string)reader["beroep"],
                RolId = reader["rol_id"] == DBNull.Value ? null : (int?)reader["rol_id"],
                RolNaam = reader["rol_naam"] == DBNull.Value ? null : (string)reader["rol_naam"]
            };
        }

        private static ChildData MapChildData(SqlDataReader reader)
        {
            return new ChildData
            {
                Id = (int)reader["id"],
                Voorletters = reader["voorletters"] == DBNull.Value ? null : (string)reader["voorletters"],
                Voornamen = reader["voornamen"] == DBNull.Value ? null : (string)reader["voornamen"],
                Roepnaam = reader["roepnaam"] == DBNull.Value ? null : (string)reader["roepnaam"],
                Geslacht = reader["geslacht"] == DBNull.Value ? null : (string)reader["geslacht"],
                Tussenvoegsel = reader["tussenvoegsel"] == DBNull.Value ? null : (string)reader["tussenvoegsel"],
                Achternaam = (string)reader["achternaam"],
                Adres = reader["adres"] == DBNull.Value ? null : (string)reader["adres"],
                Postcode = reader["postcode"] == DBNull.Value ? null : (string)reader["postcode"],
                Plaats = reader["plaats"] == DBNull.Value ? null : (string)reader["plaats"],
                GeboortePlaats = reader["geboorte_plaats"] == DBNull.Value ? null : (string)reader["geboorte_plaats"],
                GeboorteDatum = reader["geboorte_datum"] == DBNull.Value ? null : (DateTime?)reader["geboorte_datum"],
                Nationaliteit1 = reader["nationaliteit_1"] == DBNull.Value ? null : (string)reader["nationaliteit_1"],
                Nationaliteit2 = reader["nationaliteit_2"] == DBNull.Value ? null : (string)reader["nationaliteit_2"],
                Telefoon = reader["telefoon"] == DBNull.Value ? null : (string)reader["telefoon"],
                Email = reader["email"] == DBNull.Value ? null : (string)reader["email"],
                Beroep = reader["beroep"] == DBNull.Value ? null : (string)reader["beroep"]
            };
        }
    }
}