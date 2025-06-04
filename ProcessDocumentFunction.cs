using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.Json;

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
            
            _logger.LogInformation($"[{correlationId}] Processing document request started - VERSION 3.0 - HEADER FIX");

            try
            {
                // Log request details safely
                try
                {
                    _logger.LogInformation($"[{correlationId}] Request URL: {req.Url}");
                    _logger.LogInformation($"[{correlationId}] Request Method: {req.Method}");
                    
                    // Log all headers for debugging
                    foreach (var header in req.Headers)
                    {
                        _logger.LogDebug($"[{correlationId}] Header: {header.Key} = {string.Join(", ", header.Value)}");
                    }
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
                
                // Get the uploaded file
                byte[] fileContent = null;
                string fileName = "document.docx";
                
                if (contentType.Contains("multipart/form-data"))
                {
                    _logger.LogInformation($"[{correlationId}] Parsing multipart form data");
                    
                    try
                    {
                        // First try to read the entire body to see what we're getting
                        using var bodyStream = new MemoryStream();
                        await req.Body.CopyToAsync(bodyStream);
                        var bodyBytes = bodyStream.ToArray();
                        _logger.LogInformation($"[{correlationId}] Request body size: {bodyBytes.Length} bytes");
                        
                        // Log first 500 chars of body for debugging (if text)
                        if (bodyBytes.Length > 0)
                        {
                            var bodyPreview = Encoding.UTF8.GetString(bodyBytes.Take(Math.Min(500, bodyBytes.Length)).ToArray());
                            _logger.LogDebug($"[{correlationId}] Body preview: {bodyPreview}");
                        }
                        
                        // Reset stream for parsing
                        req.Body = new MemoryStream(bodyBytes);
                        
                        // Parse multipart form data
                        var formData = await req.ParseMultipartAsync();
                        _logger.LogInformation($"[{correlationId}] Found {formData.Files.Count} files in form data");
                        
                        // Log all file names found
                        foreach (var f in formData.Files)
                        {
                            _logger.LogInformation($"[{correlationId}] Found file field: '{f.Name}', filename: '{f.FileName}', size: {f.Data?.Length ?? 0}");
                        }
                        
                        var file = formData.Files.FirstOrDefault(f => f.Name == "document" || f.Name == "file");
                        
                        if (file != null)
                        {
                            fileContent = file.Data;
                            fileName = file.FileName ?? fileName;
                            _logger.LogInformation($"[{correlationId}] Using file: {fileName}, Size: {file.Data?.Length ?? 0} bytes");
                        }
                        else
                        {
                            _logger.LogWarning($"[{correlationId}] No file with name 'document' or 'file' found in form data");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError($"[{correlationId}] Error parsing multipart form data: {parseEx.Message}");
                        _logger.LogError($"[{correlationId}] Parse exception stack: {parseEx.StackTrace}");
                        return await CreateErrorResponse(req, HttpStatusCode.BadRequest, new
                        {
                            error = "Failed to parse multipart form data. Please ensure the file is uploaded correctly.",
                            details = parseEx.Message,
                            correlationId = correlationId
                        });
                    }
                }
                else if (contentType.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document"))
                {
                    // Read directly from body for binary upload
                    try
                    {
                        _logger.LogInformation($"[{correlationId}] Reading binary document from request body");
                        if (req.Body != null)
                        {
                            using var memoryStream = new MemoryStream();
                            await req.Body.CopyToAsync(memoryStream);
                            fileContent = memoryStream.ToArray();
                            _logger.LogInformation($"[{correlationId}] Read {fileContent.Length} bytes from body");
                        }
                    }
                    catch (Exception bodyEx)
                    {
                        _logger.LogWarning($"[{correlationId}] Error reading request body: {bodyEx.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning($"[{correlationId}] Invalid content type: {contentType}");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, new
                    {
                        error = "Invalid content type. Expected multipart/form-data or application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        correlationId = correlationId
                    });
                }
                
                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning($"[{correlationId}] No file content found");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, new
                    {
                        error = "Please upload a Word document. No file content was received.",
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
                
                // Clear any existing headers to avoid conflicts
                response.Headers.Clear();
                
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
            try
            {
                var response = req.CreateResponse(statusCode);
                
                // Clear any existing headers to avoid conflicts
                response.Headers.Clear();
                
                // Manually write JSON response to avoid header conflicts
                var json = JsonSerializer.Serialize(errorContent);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                response.Headers.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                response.Headers.TryAddWithoutValidation("Content-Length", bytes.Length.ToString());
                
                await response.Body.WriteAsync(bytes);
                return response;
            }
            catch (Exception ex)
            {
                // If even the error response fails, create a minimal response
                var fallbackResponse = req.CreateResponse(statusCode);
                fallbackResponse.Headers.Clear();
                await fallbackResponse.WriteStringAsync($"{{\"error\":\"Failed to create error response: {ex.Message}\"}}", Encoding.UTF8);
                return fallbackResponse;
            }
        }
    }
}