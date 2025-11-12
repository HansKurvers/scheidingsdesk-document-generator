using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using scheidingsdesk_document_generator.Services;
using System;
using System.Threading.Tasks;

namespace Scheidingsdesk
{
    /// <summary>
    /// Function to retrieve available template types
    /// </summary>
    public class GetTemplateTypesFunction
    {
        private readonly ILogger<GetTemplateTypesFunction> _logger;
        private readonly DatabaseService _databaseService;

        public GetTemplateTypesFunction(
            ILogger<GetTemplateTypesFunction> logger,
            DatabaseService databaseService)
        {
            _logger = logger;
            _databaseService = databaseService;
        }

        [Function("GetTemplateTypes")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "template-types")] HttpRequest req)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation($"[{correlationId}] Get template types request started");

            try
            {
                var templateTypes = await _databaseService.GetAvailableTemplateTypesAsync();

                _logger.LogInformation($"[{correlationId}] Successfully retrieved {templateTypes.Count} template types");

                return new OkObjectResult(new
                {
                    templateTypes = templateTypes,
                    count = templateTypes.Count,
                    correlationId = correlationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Error retrieving template types");
                return new ObjectResult(new
                {
                    error = "An error occurred while retrieving template types",
                    correlationId = correlationId
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}