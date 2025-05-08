using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Linq;

namespace Scheidingsdesk
{
    public class RemoveContentControls
    {
        private readonly ILogger<RemoveContentControls> _logger;

        public RemoveContentControls(ILogger<RemoveContentControls> logger)
        {
            _logger = logger;
        }

        [Function("RemoveContentControls")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request to remove content controls.");

            // Get the uploaded file
            var formData = await req.ReadFormAsync();
            var file = formData.Files.GetFile("document") ?? formData.Files.GetFile("file");            
            if (file == null)
            {
                return new BadRequestObjectResult("Please upload a Word document.");
            }

            // Create memory streams for input and output
            using var inputStream = new MemoryStream();
            await file.CopyToAsync(inputStream);
            inputStream.Position = 0;
            
            using var outputStream = new MemoryStream();
            
            // Process the document
            try
            {
                // Process the document directly from the input stream to avoid unnecessary copying
                using (WordprocessingDocument doc = WordprocessingDocument.Open(inputStream, false))
                {
                    _logger.LogInformation("Document opened successfully.");
                    
                    // Create a memory copy with the document to process
                    using (WordprocessingDocument outputDoc = WordprocessingDocument.Create(outputStream, doc.DocumentType))
                    {
                        // Import all parts from the original document
                        foreach (var part in doc.Parts)
                        {
                            outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                        }
                        
                        // Get the main document part
                        var mainPart = outputDoc.MainDocumentPart;
                        if (mainPart != null)
                        {
                            // Find and process all structured document tags (content controls)
                            ProcessContentControls(mainPart.Document);
                            
                            // Save the changes
                            mainPart.Document.Save();
                            _logger.LogInformation("Content controls processed successfully.");
                        }
                    }
                }
                
                // Reset the position for reading
                outputStream.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing document: {ex.Message}");
                return new ObjectResult($"Error processing document: {ex.Message}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        
            // Return the processed document
            return new FileContentResult(outputStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                FileDownloadName = "ProcessedDocument.docx"
            };
        }

        private void ProcessContentControls(DocumentFormat.OpenXml.OpenXmlElement element)
        {
            // Process in reverse order to avoid collection modification issues
            // Create a list for efficient enumeration
            var sdtElements = element.Descendants<SdtElement>().ToList();
            
            _logger.LogInformation($"Found {sdtElements.Count} content controls to process.");
            
            // Process from bottom to top to avoid issues with nested content controls
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                
                // Get the parent of the SDT element
                var parent = sdt.Parent;
                if (parent == null) continue;
                
                // Safely extract content directly from the SDT element
                // SdtContent is part of SdtElement's structure - we know it exists inside
                var contentElements = sdt.Elements().Where(e => e.LocalName == "sdtContent").FirstOrDefault();
                if (contentElements == null) continue;
                
                // Use a more efficient approach by extracting content once and inserting it as a block
                var contentToPreserve = contentElements.ChildElements.ToList();
                
                // If there are children to preserve, insert them all at once
                if (contentToPreserve.Any())
                {
                    // Insert all children in one go for better performance
                    foreach (var child in contentToPreserve)
                    {
                        parent.InsertBefore(child.CloneNode(true), sdt);
                    }
                }
                
                // Remove the SDT element
                parent.RemoveChild(sdt);
            }
        }
    }
}