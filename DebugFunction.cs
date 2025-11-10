using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace scheidingsdesk_document_generator
{
    public class DebugFunction
    {
        private readonly ILogger<DebugFunction> _logger;
        private readonly DatabaseService _databaseService;

        public DebugFunction(ILogger<DebugFunction> logger, DatabaseService databaseService)
        {
            _logger = logger;
            _databaseService = databaseService;
        }

        [Function("debug-placeholders")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("Debug endpoint called");

            // Get dossier ID from query string
            string? dossierIdStr = req.Query["dossierId"].FirstOrDefault();

            if (!int.TryParse(dossierIdStr, out int dossierId))
            {
                return new BadRequestObjectResult(new { error = "Please provide a valid dossierId" });
            }

            try
            {
                // Get dossier data
                var dossierData = await _databaseService.GetDossierDataAsync(dossierId);
                
                if (dossierData == null)
                {
                    return new NotFoundObjectResult(new { error = $"Dossier {dossierId} not found" });
                }

                var debugInfo = new
                {
                    dossierId = dossierId,
                    hasOuderschapsplanInfo = dossierData.OuderschapsplanInfo != null,
                    ouderschapsplanInfo = dossierData.OuderschapsplanInfo != null ? new
                    {
                        soortRelatie = dossierData.OuderschapsplanInfo.SoortRelatie,
                        datumAanvangRelatie = dossierData.OuderschapsplanInfo.DatumAanvangRelatie,
                        plaatsRelatie = dossierData.OuderschapsplanInfo.PlaatsRelatie,
                        // API generated fields
                        gezagZin = dossierData.OuderschapsplanInfo.GezagZin,
                        relatieAanvangZin = dossierData.OuderschapsplanInfo.RelatieAanvangZin,
                        ouderschapsplanDoelZin = dossierData.OuderschapsplanInfo.OuderschapsplanDoelZin,
                        // Check if they're empty
                        hasGezagZin = !string.IsNullOrEmpty(dossierData.OuderschapsplanInfo.GezagZin),
                        hasRelatieAanvangZin = !string.IsNullOrEmpty(dossierData.OuderschapsplanInfo.RelatieAanvangZin),
                        hasOuderschapsplanDoelZin = !string.IsNullOrEmpty(dossierData.OuderschapsplanInfo.OuderschapsplanDoelZin)
                    } : null,
                    kinderenCount = dossierData.Kinderen?.Count ?? 0,
                    partijenCount = dossierData.Partijen?.Count ?? 0
                };

                return new OkObjectResult(debugInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug endpoint");
                return new ObjectResult(new { error = ex.Message, stackTrace = ex.StackTrace })
                {
                    StatusCode = 500
                };
            }
        }
    }
}