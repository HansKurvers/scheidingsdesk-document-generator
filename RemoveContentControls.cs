using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Processors;

namespace Scheidingsdesk
{
    /// <summary>
    /// Azure Function endpoint for removing content controls from Word documents
    /// Delegates content control processing to IContentControlProcessor service (SOLID/DRY)
    /// </summary>
    public class RemoveContentControls
    {
        private readonly ILogger<RemoveContentControls> _logger;
        private readonly IContentControlProcessor _contentControlProcessor;

        public RemoveContentControls(
            ILogger<RemoveContentControls> logger,
            IContentControlProcessor contentControlProcessor)
        {
            _logger = logger;
            _contentControlProcessor = contentControlProcessor;
        }

        [Function("RemoveContentControls")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request to remove content controls.");

            try
            {
                byte[] fileContent = null;
                string fileName = "document.docx";

                // Check if it's multipart form data
                if (req.HasFormContentType)
                {
                    var file = req.Form.Files.GetFile("document") ?? req.Form.Files.GetFile("file") ?? req.Form.Files.FirstOrDefault();
                    
                    if (file != null && file.Length > 0)
                    {
                        fileName = file.FileName;
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        fileContent = ms.ToArray();
                    }
                }
                else
                {
                    // Binary upload
                    using var ms = new MemoryStream();
                    await req.Body.CopyToAsync(ms);
                    fileContent = ms.ToArray();
                }
                
                if (fileContent == null || fileContent.Length == 0)
                {
                    return new BadRequestObjectResult(new { error = "Please upload a Word document." });
                }

                _logger.LogInformation($"Processing document with {fileContent.Length} bytes");
                
                // Create streams for processing
                using var sourceStream = new MemoryStream(fileContent);
                var outputStream = new MemoryStream();
                
                // Open source document as READ-ONLY
                using (WordprocessingDocument sourceDoc = WordprocessingDocument.Open(sourceStream, false))
                {
                    _logger.LogInformation("Source document opened successfully.");
                    
                    // Create a NEW document in the output stream
                    using (WordprocessingDocument outputDoc = WordprocessingDocument.Create(outputStream, sourceDoc.DocumentType))
                    {
                        _logger.LogInformation("Creating new document for output.");
                        
                        // Copy all parts from source to output
                        foreach (var part in sourceDoc.Parts)
                        {
                            outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                        }
                        
                        var mainPart = outputDoc.MainDocumentPart;
                        if (mainPart != null)
                        {
                            // Use shared service instead of duplicate code (DRY principle)
                            string correlationId = Guid.NewGuid().ToString();
                            _contentControlProcessor.RemoveContentControls(mainPart.Document, correlationId);
                            mainPart.Document.Save();
                            _logger.LogInformation("Content controls processed successfully.");
                        }
                        else
                        {
                            _logger.LogWarning("MainDocumentPart is null!");
                        }
                    }
                }
                
                _logger.LogInformation($"Final output stream size: {outputStream.Length} bytes");
                outputStream.Position = 0;
                
                // Return the processed document
                return new FileStreamResult(outputStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    FileDownloadName = "ProcessedDocument.docx"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing document: {ex.Message}");
                return new ObjectResult(new
                {
                    error = "Error processing document",
                    details = ex.Message
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}