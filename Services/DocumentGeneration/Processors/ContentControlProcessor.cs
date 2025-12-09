using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Generators;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Simple and robust processor for handling content controls and table placeholders
    /// </summary>
    public class ContentControlProcessor : IContentControlProcessor
    {
        private readonly ILogger<ContentControlProcessor> _logger;
        private readonly IEnumerable<ITableGenerator> _tableGenerators;

        public ContentControlProcessor(
            ILogger<ContentControlProcessor> logger,
            IEnumerable<ITableGenerator> tableGenerators)
        {
            _logger = logger;
            _tableGenerators = tableGenerators;
        }

        /// <summary>
        /// Processes table and list placeholders in document body
        /// </summary>
        public void ProcessTablePlaceholders(Body body, DossierData data, string correlationId)
        {
            var paragraphs = body.Descendants<Paragraph>().ToList();
            _logger.LogInformation($"[{correlationId}] Scanning {paragraphs.Count} paragraphs for table placeholders");

            // Process paragraphs in reverse order to avoid index issues when inserting
            for (int i = paragraphs.Count - 1; i >= 0; i--)
            {
                var paragraph = paragraphs[i];
                var text = GetParagraphText(paragraph);

                // Check each table generator
                foreach (var generator in _tableGenerators)
                {
                    if (text.Contains(generator.PlaceholderTag))
                    {
                        _logger.LogInformation($"[{correlationId}] Found placeholder: {generator.PlaceholderTag}");

                        try
                        {
                            // Get original paragraph properties to preserve formatting (indentation, etc.)
                            var originalProps = paragraph.ParagraphProperties?.CloneNode(true) as ParagraphProperties;

                            // Generate the table/list elements
                            var elements = generator.Generate(data, correlationId);

                            // Apply original paragraph properties to generated paragraphs
                            if (originalProps != null)
                            {
                                foreach (var element in elements)
                                {
                                    if (element is Paragraph newParagraph)
                                    {
                                        var clonedProps = originalProps.CloneNode(true) as ParagraphProperties;

                                        if (newParagraph.ParagraphProperties != null)
                                        {
                                            // Keep existing properties but add indentation from original if not set
                                            if (newParagraph.ParagraphProperties.Indentation == null && clonedProps?.Indentation != null)
                                            {
                                                newParagraph.ParagraphProperties.Indentation =
                                                    clonedProps.Indentation.CloneNode(true) as Indentation;
                                            }
                                        }
                                        else
                                        {
                                            newParagraph.ParagraphProperties = clonedProps;
                                        }
                                    }
                                }
                            }

                            // Insert elements after the placeholder paragraph
                            foreach (var element in elements.AsEnumerable().Reverse())
                            {
                                paragraph.Parent?.InsertAfter(element, paragraph);
                            }

                            // Remove the placeholder paragraph
                            paragraph.Remove();

                            _logger.LogInformation($"[{correlationId}] Replaced {generator.PlaceholderTag} with {elements.Count} elements");
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError(ex, $"[{correlationId}] Error generating table for {generator.PlaceholderTag}");
                        }

                        break; // Only process one placeholder per paragraph
                    }
                }
            }
        }

        /// <summary>
        /// Removes all content controls while preserving their content
        /// </summary>
        public void RemoveContentControls(Document document, string correlationId)
        {
            var sdtElements = document.Descendants<SdtElement>().ToList();
            _logger.LogInformation($"[{correlationId}] Removing {sdtElements.Count} content controls");

            // Process from bottom to top to avoid issues with nested content controls
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                RemoveSingleContentControl(sdt);
            }

            _logger.LogInformation($"[{correlationId}] Content controls removal completed");
        }

        #region Private Helper Methods

        /// <summary>
        /// Gets all text from a paragraph
        /// </summary>
        private string GetParagraphText(Paragraph paragraph)
        {
            return string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
        }

        /// <summary>
        /// Removes a single content control while preserving its content
        /// </summary>
        private void RemoveSingleContentControl(SdtElement sdt)
        {
            var parent = sdt.Parent;
            if (parent == null) return;

            var contentElement = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
            if (contentElement == null)
            {
                // No content, just remove the control
                sdt.Remove();
                return;
            }

            var contentToPreserve = contentElement.ChildElements.ToList();

            if (contentToPreserve.Count > 0)
            {
                foreach (var child in contentToPreserve)
                {
                    var clonedChild = child.CloneNode(true);

                    // Fix text formatting - set to black and remove shading
                    FixTextFormatting(clonedChild);

                    parent.InsertBefore(clonedChild, sdt);
                }
            }

            // Remove the content control
            sdt.Remove();
        }

        /// <summary>
        /// Fixes text formatting in elements (sets color to black, removes shading)
        /// </summary>
        private void FixTextFormatting(OpenXmlElement element)
        {
            foreach (var run in element.Descendants<Run>())
            {
                var runProps = run.RunProperties ?? run.AppendChild(new RunProperties());

                // Remove any existing color
                var colorElements = runProps.Elements<Color>().ToList();
                foreach (var color in colorElements)
                {
                    runProps.RemoveChild(color);
                }

                // Set text color to black
                runProps.AppendChild(new Color() { Val = "000000" });

                // Remove any shading
                var shadingElements = runProps.Elements<Shading>().ToList();
                foreach (var shading in shadingElements)
                {
                    runProps.RemoveChild(shading);
                }
            }
        }

        #endregion
    }
}