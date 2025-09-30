using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using scheidingsdesk_document_generator.Services.DocumentGeneration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Scheidingsdesk
{
    /// <summary>
    /// Refactored Ouderschapsplan Function - Simple endpoint that delegates to services
    /// Reduced from 1670 lines to ~100 lines by using modular architecture
    /// </summary>
    public class OuderschapsplanFunctionRefactored
    {
        private readonly ILogger<OuderschapsplanFunctionRefactored> _logger;
        private readonly IDocumentGenerationService _documentGenerationService;

        public OuderschapsplanFunctionRefactored(
            ILogger<OuderschapsplanFunctionRefactored> logger,
            IDocumentGenerationService documentGenerationService)
        {
            _logger = logger;
            _documentGenerationService = documentGenerationService;
        }

        [Function("OuderschapsplanRefactored")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ouderschapsplan-refactored")] HttpRequest req)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation($"[{correlationId}] Ouderschapsplan generation request started");

            try
            {
                // Parse and validate request
                var request = await ParseRequestAsync(req, correlationId);
                if (request == null)
                {
                    return CreateBadRequest("Invalid request body. Please provide a JSON object with DossierId.", correlationId);
                }

                // Get template URL
                string templateUrl = Environment.GetEnvironmentVariable("TemplateStorageUrl")
                    ?? throw new InvalidOperationException("TemplateStorageUrl environment variable is not set.");

                _logger.LogInformation($"[{correlationId}] Generating document for DossierId: {request.DossierId}");

                // Generate document (ALL logic delegated to service)
                var documentStream = await _documentGenerationService.GenerateDocumentAsync(
                    request.DossierId,
                    templateUrl,
                    correlationId
                );

                stopwatch.Stop();
                _logger.LogInformation($"[{correlationId}] Document generated successfully in {stopwatch.ElapsedMilliseconds}ms");

                // Return file result
                var fileName = $"Ouderschapsplan_Dossier_{request.DossierId}_{DateTime.Now:yyyyMMdd}.docx";
                return new FileStreamResult(documentStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    FileDownloadName = fileName
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Invalid operation: {ex.Message}");
                return CreateErrorResponse(ex.Message, correlationId, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Unexpected error: {ex.Message}");
                return CreateErrorResponse("An unexpected error occurred during document generation.", correlationId, 500);
            }
        }

        private async Task<OuderschapsplanRequest?> ParseRequestAsync(HttpRequest req, string correlationId)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning($"[{correlationId}] Empty request body");
                    return null;
                }

                var request = JsonConvert.DeserializeObject<OuderschapsplanRequest>(requestBody);

                if (request?.DossierId == null || request.DossierId <= 0)
                {
                    _logger.LogWarning($"[{correlationId}] Invalid DossierId: {request?.DossierId}");
                    return null;
                }

                return request;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"[{correlationId}] JSON parsing error");
                return null;
            }
        }

        private IActionResult CreateBadRequest(string message, string correlationId)
        {
            return new BadRequestObjectResult(new
            {
                error = message,
                correlationId = correlationId
            });
        }

        private IActionResult CreateErrorResponse(string message, string correlationId, int statusCode)
        {
            return new ObjectResult(new
            {
                error = message,
                correlationId = correlationId
            })
            {
                StatusCode = statusCode
            };
        }
    }
}