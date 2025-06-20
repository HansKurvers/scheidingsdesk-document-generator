using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace Scheidingsdesk
{
    public class DocumentProcessor
    {
        private readonly ILogger _logger;

        public DocumentProcessor(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<MemoryStream> ProcessDocumentAsync(Stream inputStream, string correlationId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Read input stream to byte array
                    inputStream.Position = 0;
                    using var memoryStream = new MemoryStream();
                    inputStream.CopyTo(memoryStream);
                    var fileContent = memoryStream.ToArray();
                    
                    _logger.LogInformation($"[{correlationId}] Read {fileContent.Length} bytes from input stream");
                    
                    if (fileContent.Length == 0)
                    {
                        throw new InvalidOperationException("Input stream is empty or could not be read.");
                    }

                    // Create streams for processing
                    using var sourceStream = new MemoryStream(fileContent);
                    var outputStream = new MemoryStream();
                    
                    // Open source document as READ-ONLY
                    using (WordprocessingDocument sourceDoc = WordprocessingDocument.Open(sourceStream, false))
                    {
                        _logger.LogInformation($"[{correlationId}] Source document opened successfully.");
                        
                        // Create a NEW document in the output stream
                        using (WordprocessingDocument outputDoc = WordprocessingDocument.Create(outputStream, sourceDoc.DocumentType))
                        {
                            _logger.LogInformation($"[{correlationId}] Creating new document for output.");
                            
                            // Copy all parts from source to output
                            foreach (var part in sourceDoc.Parts)
                            {
                                outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                            }
                            
                            var mainPart = outputDoc.MainDocumentPart;
                            if (mainPart != null)
                            {
                                // Process the document
                                RemoveProblematicContentControls(mainPart.Document, correlationId);
                                ProcessContentControls(mainPart.Document, correlationId);
                                
                                mainPart.Document.Save();
                                _logger.LogInformation($"[{correlationId}] Content controls processed successfully.");
                            }
                            else
                            {
                                _logger.LogWarning($"[{correlationId}] MainDocumentPart is null!");
                            }
                        }
                    }
                    
                    _logger.LogInformation($"[{correlationId}] Final output stream size: {outputStream.Length} bytes");
                    outputStream.Position = 0;
                    return outputStream;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[{correlationId}] Error during document processing: {ex.Message}");
                    _logger.LogError($"[{correlationId}] Stack trace: {ex.StackTrace}");
                    throw;
                }
            });
        }

        private void RemoveProblematicContentControls(Document document, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Scanning for empty or placeholder content controls to remove...");
            
            var sdtElements = document.Descendants<SdtElement>().ToList();
            
            _logger.LogInformation($"[{correlationId}] Found {sdtElements.Count} content controls to analyze.");
            
            foreach (var sdt in sdtElements)
            {
                var contentText = GetSdtContentText(sdt);
                
                // Check if content control contains "#" or is empty/whitespace
                if (string.IsNullOrWhiteSpace(contentText) || contentText.Contains('#'))
                {
                    _logger.LogDebug($"[{correlationId}] Found problematic content control: '{contentText}'");
                    
                    // If content contains "#", remove the entire paragraph
                    if (contentText.Contains('#'))
                    {
                        RemoveEntireParagraph(sdt, correlationId);
                    }
                    else
                    {
                        // For empty/whitespace content, just clear the content control
                        ReplaceContentControlWithEmpty(sdt, correlationId);
                    }
                }
            }
            
            _logger.LogInformation($"[{correlationId}] Processed problematic content controls by clearing their content.");
        }
        
        private void RemoveEntireParagraph(SdtElement sdt, string correlationId)
        {
            try
            {
                // Find the paragraph that contains this content control
                var paragraph = sdt.Ancestors<Paragraph>().FirstOrDefault();
                if (paragraph != null)
                {
                    _logger.LogDebug($"[{correlationId}] Removing entire paragraph containing '#' content control");
                    paragraph.Remove();
                }
                else
                {
                    // If not in a paragraph, just remove the content control itself
                    _logger.LogDebug($"[{correlationId}] Content control not in paragraph, removing content control only");
                    sdt.Remove();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[{correlationId}] Failed to remove paragraph: {ex.Message}");
            }
        }

        private void ReplaceContentControlWithEmpty(SdtElement sdt, string correlationId)
        {
            try
            {
                // Find the content element within the SDT
                var contentElement = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                if (contentElement != null)
                {
                    // Clear all content from the SDT but keep the structure
                    contentElement.RemoveAllChildren();
                    _logger.LogDebug($"[{correlationId}] Cleared content control content");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[{correlationId}] Failed to clear content control: {ex.Message}");
            }
        }
        
        private string GetSdtContentText(SdtElement sdt)
        {
            var contentElements = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
            if (contentElements == null) return "";
            
            return contentElements.Descendants<Text>().Aggregate("", (current, text) => current + text.Text);
        }

        private void ProcessContentControls(Document document, string correlationId)
        {
            var sdtElements = document.Descendants<SdtElement>().ToList();
            _logger.LogInformation($"[{correlationId}] Found {sdtElements.Count} content controls to process.");
            
            // Process from bottom to top to avoid issues with nested content controls
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                var parent = sdt.Parent;
                if (parent == null) continue;
                
                var contentElements = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                if (contentElements == null) continue;
                
                var contentToPreserve = contentElements.ChildElements.ToList();
                
                if (contentToPreserve.Count > 0)
                {
                    foreach (var child in contentToPreserve)
                    {
                        var clonedChild = child.CloneNode(true);
                        
                        // Fix text formatting on all Run elements inside this content
                        foreach (var run in clonedChild.Descendants<Run>())
                        {
                            var runProps = run.RunProperties ?? run.AppendChild(new RunProperties());
                            
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
                
                // Remove the content control
                parent.RemoveChild(sdt);
            }
        }
    }
}