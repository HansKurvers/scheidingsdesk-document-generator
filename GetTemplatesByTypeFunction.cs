using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using scheidingsdesk_document_generator.Services;
using scheidingsdesk_document_generator.Constants;
using System;
using System.Threading.Tasks;

namespace Scheidingsdesk
{
    /// <summary>
    /// Function to retrieve templates by type
    /// </summary>
    public class GetTemplatesByTypeFunction
    {
        private readonly ILogger<GetTemplatesByTypeFunction> _logger;
        private readonly DatabaseService _databaseService;

        public GetTemplatesByTypeFunction(
            ILogger<GetTemplatesByTypeFunction> logger,
            DatabaseService databaseService)
        {
            _logger = logger;
            _databaseService = databaseService;
        }

        [Function("GetTemplatesByType")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "templates/{templateType}")] HttpRequest req,
            string templateType)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation($"[{correlationId}] Get templates by type request started for type: {templateType}");

            try
            {
                // Validate template type
                if (string.IsNullOrWhiteSpace(templateType))
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "Template type is required",
                        correlationId = correlationId
                    });
                }

                // Decode URL-encoded template type (e.g., "Bijzondere%20dag" -> "Bijzondere dag")
                templateType = Uri.UnescapeDataString(templateType);

                // Validate against known types (optional - you can remove this if you want to allow any type)
                if (!TemplateTypes.IsValidType(templateType))
                {
                    _logger.LogWarning($"[{correlationId}] Invalid template type requested: {templateType}");
                    // Still proceed with the query - let the database determine if it exists
                }

                var templates = await _databaseService.GetTemplatesByTypeAsync(templateType);

                _logger.LogInformation($"[{correlationId}] Successfully retrieved {templates.Count} templates for type: {templateType}");

                return new OkObjectResult(new
                {
                    type = templateType,
                    templates = templates,
                    count = templates.Count,
                    correlationId = correlationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Error retrieving templates for type: {templateType}");
                return new ObjectResult(new
                {
                    error = "An error occurred while retrieving templates",
                    correlationId = correlationId
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}