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
            // Check if request is null
            if (req == null)
            {
                _logger.LogError("Request is null");
                throw new ArgumentNullException(nameof(req));
            }

            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString();
            
            _logger.LogInformation($"[{correlationId}] Processing document request started");

            try
            {
                // Log request details safely
                try
                {
                    _logger.LogInformation($"[{correlationId}] Request URL: {req.Url}");
                    _logger.LogInformation($"[{correlationId}] Request Method: {req.Method}");
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning($"[{correlationId}] Error logging request details: {logEx.Message}");
                }
                
                // Validate content type
                string contentType = string.Empty;
                try
                {
                    if (req.Headers != null && req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                    {
                        contentType = contentTypeValues.FirstOrDefault() ?? string.Empty;
                    }
                }
                catch (Exception headerEx)
                {
                    _logger.LogWarning($"[{correlationId}] Error reading Content-Type header: {headerEx.Message}");
                }
                _logger.LogInformation($"[{correlationId}] Content-Type: {contentType}");
                
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
                    _logger.LogInformation($"[{correlationId}] Parsing multipart form data");
                    
                    try
                    {
                        // Parse multipart form data
                        var formData = await req.ParseMultipartAsync();
                        _logger.LogInformation($"[{correlationId}] Found {formData.Files.Count} files in form data");
                        
                        var file = formData.Files.FirstOrDefault(f => f.Name == "document" || f.Name == "file");
                        
                        if (file != null)
                        {
                            fileContent = file.Data;
                            fileName = file.FileName ?? fileName;
                            _logger.LogInformation($"[{correlationId}] Found file: {fileName}, Size: {file.Data?.Length ?? 0} bytes");
                        }
                        else
                        {
                            _logger.LogWarning($"[{correlationId}] No file with name 'document' or 'file' found in form data");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError($"[{correlationId}] Error parsing multipart form data: {parseEx.Message}");
                        return await CreateErrorResponse(req, HttpStatusCode.BadRequest, new
                        {
                            error = "Failed to parse multipart form data. Please ensure the file is uploaded correctly.",
                            details = parseEx.Message,
                            correlationId = correlationId
                        });
                    }
                }
                else
                {
                    // Read directly from body
                    try
                    {
                        if (req.Body != null)
                        {
                            using var memoryStream = new MemoryStream();
                            await req.Body.CopyToAsync(memoryStream);
                            fileContent = memoryStream.ToArray();
                        }
                    }
                    catch (Exception bodyEx)
                    {
                        _logger.LogWarning($"[{correlationId}] Error reading request body: {bodyEx.Message}");
                    }
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
                _logger.LogInformation($"[{correlationId}] Created input stream with {inputStream.Length} bytes");
                
                var outputStream = await _documentProcessor.ProcessDocumentAsync(inputStream, correlationId);
                outputStream.Position = 0;
                
                _logger.LogInformation($"[{correlationId}] Received output stream with {outputStream.Length} bytes");

                stopwatch.Stop();
                _logger.LogInformation($"[{correlationId}] Document processed successfully in {stopwatch.ElapsedMilliseconds}ms");

                // Create success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                
                // Set headers using TryAdd to avoid conflicts
                response.Headers.TryAddWithoutValidation("Content-Type", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                response.Headers.TryAddWithoutValidation("Content-Disposition", $"attachment; filename=\"Processed_{Path.GetFileNameWithoutExtension(fileName)}.docx\"");
                response.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
                response.Headers.TryAddWithoutValidation("X-Processing-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());
                response.Headers.TryAddWithoutValidation("X-Document-Size", outputStream.Length.ToString());
                
                var outputBytes = outputStream.ToArray();
                _logger.LogInformation($"[{correlationId}] Writing {outputBytes.Length} bytes to response");
                await response.Body.WriteAsync(outputBytes);
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
                _logger.LogError(ex, $"[{correlationId}] Unexpected error processing document: {ex.Message}");
                _logger.LogError($"[{correlationId}] Stack trace: {ex.StackTrace}");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, new
                {
                    error = "An unexpected error occurred while processing the document.",
                    details = ex.Message,
                    correlationId = correlationId
                });
            }
        }

        private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, object errorContent)
        {
            var response = req.CreateResponse(statusCode);
            // Don't set Content-Type manually - WriteAsJsonAsync sets it automatically
            await response.WriteAsJsonAsync(errorContent);
            return response;
        }
    }
}