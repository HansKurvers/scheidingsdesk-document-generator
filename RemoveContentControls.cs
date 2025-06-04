using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
                            ProcessContentControls(mainPart.Document);
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

        private void ProcessContentControls(DocumentFormat.OpenXml.OpenXmlElement element)
        {
            var sdtElements = element.Descendants<SdtElement>().ToList();
            
            _logger.LogInformation($"Found {sdtElements.Count} content controls to process.");
            
            // Process from bottom to top to avoid issues with nested content controls
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                var parent = sdt.Parent;
                if (parent == null) continue;
                
                var contentElements = sdt.Elements().Where(e => e.LocalName == "sdtContent").FirstOrDefault();
                if (contentElements == null) continue;
                
                var contentToPreserve = contentElements.ChildElements.ToList();
                
                if (contentToPreserve.Any())
                {
                    foreach (var child in contentToPreserve)
                    {
                        var clonedChild = child.CloneNode(true);
                        
                        // Fix text formatting on all Run elements
                        foreach (var run in clonedChild.Descendants<Run>())
                        {
                            var runProps = run.RunProperties ?? run.AppendChild(new RunProperties());
                            
                            // Remove existing colors and set to black
                            var colorElements = runProps.Elements<Color>().ToList();
                            foreach (var color in colorElements)
                            {
                                runProps.RemoveChild(color);
                            }
                            runProps.AppendChild(new Color() { Val = "000000" });
                            
                            // Remove any shading
                            var shadingElements = runProps.Elements<Shading>().ToList();
                            foreach (var shading in shadingElements)
                            {
                                runProps.RemoveChild(shading);
                            }
                        }
                        
                        parent.InsertBefore(clonedChild, sdt);
                    }
                }
                
                parent.RemoveChild(sdt);
            }
        }
    }
}