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
using System.Text.RegularExpressions;

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
                // Process the document directly from the input stream
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
                            // Process everything in one pass
                            ProcessDocument(mainPart.Document);
                            
                            // Save the changes
                            mainPart.Document.Save();
                            _logger.LogInformation("Document processed successfully.");
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

        private void ProcessDocument(Document document)
        {
            // Step 1: Remove content controls and delete empty paragraphs
            var sdtElements = document.Descendants<SdtElement>().ToList();
            
            // Process from bottom to top
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                var parent = sdt.Parent;
                if (parent == null) continue;
                
                // Get content
                var contentElement = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                if (contentElement == null) continue;
                
                // Check if empty
                var textContent = string.Join("", contentElement.Descendants<Text>().Select(t => t.Text ?? "")).Trim();
                
                if (string.IsNullOrWhiteSpace(textContent))
                {
                    // If empty, remove the entire paragraph containing this SDT
                    var paragraph = sdt.Ancestors<Paragraph>().FirstOrDefault();
                    paragraph?.Remove();
                }
                else
                {
                    // If not empty, just unwrap the content control
                    foreach (var child in contentElement.ChildElements.ToList())
                    {
                        parent.InsertBefore(child.CloneNode(true), sdt);
                    }
                    parent.RemoveChild(sdt);
                }
            }
            
            // Step 2: Renumber remaining paragraphs
            RenumberParagraphs(document);
        }

        private void RenumberParagraphs(Document document)
        {
            var paragraphs = document.Descendants<Paragraph>().ToList();
            var lastMainNumber = 0;
            var subNumber = 0;
            
            foreach (var paragraph in paragraphs)
            {
                var firstText = paragraph.Descendants<Text>().FirstOrDefault();
                if (firstText == null) continue;
                
                // Simple pattern matching for "X.Y" format
                var match = Regex.Match(firstText.Text, @"^(\d+)\.(\d+)");
                if (match.Success)
                {
                    int currentMainNumber = int.Parse(match.Groups[1].Value);
                    
                    // Reset sub-number if main number changes
                    if (currentMainNumber != lastMainNumber)
                    {
                        lastMainNumber = currentMainNumber;
                        subNumber = 0;
                    }
                    
                    // Increment and update
                    subNumber++;
                    firstText.Text = Regex.Replace(firstText.Text, @"^\d+\.\d+", $"{currentMainNumber}.{subNumber}");
                }
            }
        }
    }
}