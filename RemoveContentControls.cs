using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Linq;
using System.Net;

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
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request to remove content controls.");

            try
            {
                // Get content type
                var contentType = req.Headers.GetValues("Content-Type")?.FirstOrDefault() ?? string.Empty;
                
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
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteAsJsonAsync(new { error = "Please upload a Word document." });
                    return errorResponse;
                }

                // Create memory streams for input and output
                using var inputStream = new MemoryStream(fileContent);
                using var outputStream = new MemoryStream();
                
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
                
                // Return the processed document
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"ProcessedDocument.docx\"");
                
                await response.Body.WriteAsync(outputStream.ToArray());
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing document: {ex.Message}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new 
                { 
                    error = "Error processing document",
                    details = ex.Message 
                });
                return errorResponse;
            }
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
                    // Process each Run element to ensure proper formatting and color
                    foreach (var child in contentToPreserve)
                    {
                        // Deep clone to preserve all formatting and properties
                        var clonedChild = child.CloneNode(true);
                        
                        // Fix text formatting on all Run elements inside this content
                        foreach (var run in clonedChild.Descendants<Run>())
                        {
                            // Ensure run properties contain proper color settings
                            var runProps = run.RunProperties ?? run.AppendChild(new RunProperties());
                            
                            // Make sure there's no gray color applied (common for content controls)
                            // Remove any existing color and explicitly set to black
                            var colorElements = runProps.Elements<Color>().ToList();
                            foreach (var color in colorElements)
                            {
                                runProps.RemoveChild(color);
                            }
                            
                            // Explicitly set text color to black
                            runProps.AppendChild(new Color() { Val = "000000" });
                            
                            // Remove any shading that might affect text appearance
                            var shadingElements = runProps.Elements<Shading>().ToList();
                            foreach (var shading in shadingElements)
                            {
                                runProps.RemoveChild(shading);
                            }
                        }
                        
                        parent.InsertBefore(clonedChild, sdt);
                    }
                }
                
                // Remove the SDT element
                parent.RemoveChild(sdt);
            }
        }
    }
}