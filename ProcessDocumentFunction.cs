using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "process")] HttpRequest req)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString();
            
            _logger.LogInformation($"[{correlationId}] Processing document request started - SIMPLIFIED VERSION");

            try
            {
                byte[]? fileContent = null;
                string fileName = "document.docx";

                // Check if it's multipart form data
                if (req.HasFormContentType)
                {
                    _logger.LogInformation($"[{correlationId}] Processing multipart form data");
                    
                    // Get the first file from the form
                    var file = req.Form.Files.GetFile("document") ?? req.Form.Files.GetFile("file") ?? req.Form.Files.FirstOrDefault();
                    
                    if (file != null && file.Length > 0)
                    {
                        fileName = file.FileName;
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        fileContent = ms.ToArray();
                        _logger.LogInformation($"[{correlationId}] Found file: {fileName}, Size: {fileContent.Length} bytes");
                    }
                }
                else if (req.ContentType?.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document") == true)
                {
                    // Binary upload
                    _logger.LogInformation($"[{correlationId}] Processing binary upload");
                    using var ms = new MemoryStream();
                    await req.Body.CopyToAsync(ms);
                    fileContent = ms.ToArray();
                }
                else
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid content type. Please upload a Word document.",
                        correlationId = correlationId
                    });
                }

                if (fileContent == null || fileContent.Length == 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "No file uploaded. Please select a Word document.",
                        correlationId = correlationId
                    });
                }

                // Check file size (50MB limit)
                const long maxFileSize = 50 * 1024 * 1024;
                if (fileContent.Length > maxFileSize)
                {
                    return new BadRequestObjectResult(new
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

                // Return the processed file
                return new FileStreamResult(outputStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    FileDownloadName = $"Processed_{Path.GetFileNameWithoutExtension(fileName)}.docx"
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"[{correlationId}] Document processing error: {ex.Message}");
                return new BadRequestObjectResult(new
                {
                    error = ex.Message,
                    correlationId = correlationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Unexpected error processing document");
                return new ObjectResult(new
                {
                    error = "An unexpected error occurred while processing the document.",
                    details = ex.Message,
                    correlationId = correlationId
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}