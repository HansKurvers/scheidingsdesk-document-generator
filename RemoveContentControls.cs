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
using System.Collections.Generic;
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
                            
                            // After removing empty content controls, renormalize numbering
                            RenormalizeNumbering(mainPart.Document);
                            
                            // Save the changes
                            mainPart.Document.Save();
                            _logger.LogInformation("Content controls processed and numbering normalized successfully.");
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
            var sdtElements = element.Descendants<SdtElement>().ToList();
            var paragraphsToRemove = new List<Paragraph>();
            
            _logger.LogInformation($"Found {sdtElements.Count} content controls to process.");
            
            // Process from bottom to top to avoid issues with nested content controls
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                
                // Check if the content control is empty
                bool isEmpty = IsContentControlEmpty(sdt);
                
                if (isEmpty)
                {
                    // Find the paragraph that contains this SDT
                    var containingParagraph = FindContainingParagraph(sdt);
                    if (containingParagraph != null && !paragraphsToRemove.Contains(containingParagraph))
                    {
                        paragraphsToRemove.Add(containingParagraph);
                        _logger.LogInformation($"Marked paragraph for removal due to empty content control.");
                    }
                }
                else
                {
                    // Process non-empty content controls as before
                    ProcessNonEmptyContentControl(sdt);
                }
            }
            
            // Remove paragraphs that contained empty content controls
            foreach (var paragraph in paragraphsToRemove)
            {
                paragraph.Remove();
            }
            
            _logger.LogInformation($"Removed {paragraphsToRemove.Count} paragraphs with empty content controls.");
        }

        private bool IsContentControlEmpty(SdtElement sdt)
        {
            // Get the content from the SDT element
            var contentElements = sdt.Elements().Where(e => e.LocalName == "sdtContent").FirstOrDefault();
            if (contentElements == null) return true;
            
            // Check if there's any meaningful text content
            var textContent = string.Join("", contentElements.Descendants<Text>().Select(t => t.Text ?? "")).Trim();
            
            // Consider it empty if there's no text or only whitespace
            return string.IsNullOrWhiteSpace(textContent);
        }

        private Paragraph FindContainingParagraph(OpenXmlElement element)
        {
            var current = element.Parent;
            while (current != null)
            {
                if (current is Paragraph paragraph)
                {
                    return paragraph;
                }
                current = current.Parent;
            }
            return null;
        }

        private void ProcessNonEmptyContentControl(SdtElement sdt)
        {
            // Get the parent of the SDT element
            var parent = sdt.Parent;
            if (parent == null) return;
            
            // Safely extract content directly from the SDT element
            var contentElements = sdt.Elements().Where(e => e.LocalName == "sdtContent").FirstOrDefault();
            if (contentElements == null) return;
            
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

        private void RenormalizeNumbering(Document document)
        {
            _logger.LogInformation("Starting numbering renormalization.");
            
            // Pattern to match numbering like "2.1", "2.2", "10.1", etc.
            var numberingPattern = new Regex(@"^(\d+)\.(\d+)(?:\s|$)");
            
            // Dictionary to track the current sub-number for each main number
            var numberingMap = new Dictionary<int, int>();
            
            // Get all paragraphs
            var paragraphs = document.Descendants<Paragraph>().ToList();
            
            foreach (var paragraph in paragraphs)
            {
                // Get the text content of the paragraph
                var texts = paragraph.Descendants<Text>().ToList();
                if (!texts.Any()) continue;
                
                // Check the first text element for numbering pattern
                var firstText = texts.First();
                var match = numberingPattern.Match(firstText.Text);
                
                if (match.Success)
                {
                    int mainNumber = int.Parse(match.Groups[1].Value);
                    int subNumber = int.Parse(match.Groups[2].Value);
                    
                    // Initialize or increment the sub-number for this main number
                    if (!numberingMap.ContainsKey(mainNumber))
                    {
                        numberingMap[mainNumber] = 1;
                    }
                    else
                    {
                        numberingMap[mainNumber]++;
                    }
                    
                    // Check if renumbering is needed
                    int expectedSubNumber = numberingMap[mainNumber];
                    if (subNumber != expectedSubNumber)
                    {
                        // Update the numbering
                        string oldNumbering = match.Value;
                        string newNumbering = $"{mainNumber}.{expectedSubNumber} ";
                        
                        // Replace the old numbering with the new one
                        firstText.Text = firstText.Text.Replace(oldNumbering, newNumbering);
                        
                        _logger.LogInformation($"Renumbered {oldNumbering.Trim()} to {newNumbering.Trim()}");
                    }
                }
            }
            
            _logger.LogInformation("Numbering renormalization completed.");
        }
    }
}