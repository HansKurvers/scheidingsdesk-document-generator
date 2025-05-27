using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics;
using System.Net;
using System.Linq;

namespace Scheidingsdesk
{
    public class ProcessDocumentFunction
    {
        private readonly ILogger<ProcessDocumentFunction> _logger;
        private readonly DocumentProcessor _documentProcessor;

        public ProcessDocumentFunction(ILogger<ProcessDocumentFunction> logger)
        {
            _logger = logger;
            _documentProcessor = new DocumentProcessor(logger);
        }

        [Function("ProcessDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "process")] HttpRequestData req)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString();
            
            _logger.LogInformation($"[{correlationId}] Processing document request started");

            try
            {
                // Validate content type
                var contentType = req.Headers.GetValues("Content-Type")?.FirstOrDefault() ?? string.Empty;
                if (!contentType.Contains("multipart/form-data") && 
                    !contentType.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document"))
                {
                    _logger.LogWarning($"[{correlationId}] Invalid content type: {contentType}");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, new
                    {
                        error = "Invalid content type. Expected Word document.",
                        correlationId = correlationId
                    });
                }

                // Get the uploaded file
                byte[] fileContent = null;
                string fileName = "document.docx";
                
                if (contentType.Contains("multipart/form-data"))
                {
                    // Parse multipart form data
                    var formData = await req.ParseMultipartAsync();
                    var file = formData.Files.FirstOrDefault(f => f.Name == "document" || f.Name == "file");
                    
                    if (file != null)
                    {
                        fileContent = file.Data;
                        fileName = file.FileName ?? fileName;
                    }
                }
                else
                {
                    // Read directly from body
                    using var memoryStream = new MemoryStream();
                    await req.Body.CopyToAsync(memoryStream);
                    fileContent = memoryStream.ToArray();
                }
                
                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning($"[{correlationId}] No file uploaded");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, new
                    {
                        error = "Please upload a Word document.",
                        correlationId = correlationId
                    });
                }

                // Check file size (50MB limit)
                const long maxFileSize = 50 * 1024 * 1024; // 50MB
                if (fileContent.Length > maxFileSize)
                {
                    _logger.LogWarning($"[{correlationId}] File too large: {fileContent.Length} bytes");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, new
                    {
                        error = $"File size exceeds 50MB limit. File size: {fileContent.Length / (1024 * 1024)}MB",
                        correlationId = correlationId
                    });
                }

                _logger.LogInformation($"[{correlationId}] Processing file: {fileName}, Size: {fileContent.Length} bytes");

                // Process the document
                using var inputStream = new MemoryStream(fileContent);
                var outputStream = await _documentProcessor.ProcessDocumentAsync(inputStream, correlationId);
                outputStream.Position = 0;

                stopwatch.Stop();
                _logger.LogInformation($"[{correlationId}] Document processed successfully in {stopwatch.ElapsedMilliseconds}ms");

                // Create success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"Processed_{Path.GetFileNameWithoutExtension(fileName)}.docx\"");
                response.Headers.Add("X-Correlation-Id", correlationId);
                response.Headers.Add("X-Processing-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());
                response.Headers.Add("X-Document-Size", outputStream.Length.ToString());
                
                await response.Body.WriteAsync(outputStream.ToArray());
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"[{correlationId}] Document processing error: {ex.Message}");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, new
                {
                    error = ex.Message,
                    correlationId = correlationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Unexpected error processing document");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, new
                {
                    error = "An unexpected error occurred while processing the document.",
                    details = ex.Message,
                    correlationId = correlationId
                });
            }
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, object errorContent)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(errorContent);
            return response;
        }
    }
}