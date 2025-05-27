using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;


namespace Scheidingsdesk
{
    public class DocumentProcessor
    {
        private readonly ILogger _logger;
        private const string REMOVE_PARAGRAPH_MARKER = "#";
        private const string REMOVE_ARTICLE_MARKER = "^";

        // Updated regex patterns to distinguish main articles from sub-articles
        private static readonly Regex MainArticlePattern = new Regex(@"^(\d+)\.\s*(.*)$", RegexOptions.Compiled);
        private static readonly Regex SubArticlePattern = new Regex(@"^\s+(\d+)\.\s*(.*)$", RegexOptions.Compiled);
        public DocumentProcessor(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<MemoryStream> ProcessDocumentAsync(Stream inputStream, string correlationId)
        {
            var outputStream = new MemoryStream();

            await Task.Run(() =>
            {
                try
                {
                    // Copy input to output stream for processing
                    inputStream.CopyTo(outputStream);
                    outputStream.Position = 0;

                    using (var doc = WordprocessingDocument.Open(outputStream, true))
                    {
                        if (doc.MainDocumentPart == null)
                        {
                            throw new InvalidOperationException("Document does not contain a main document part.");
                        }

                        _logger.LogInformation($"[{correlationId}] Starting document processing");

                        // Step 1: Process placeholders and mark elements for removal
                        var removalInfo = ProcessPlaceholders(doc.MainDocumentPart.Document, correlationId);

                        // Step 2: Remove marked elements
                        RemoveMarkedElements(doc.MainDocumentPart.Document, removalInfo);

                        // Step 4: Remove all content controls
                        RemoveContentControls(doc.MainDocumentPart.Document, correlationId);

                        // Save changes
                        doc.MainDocumentPart.Document.Save();
                        _logger.LogInformation($"[{correlationId}] Document processing completed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[{correlationId}] Error during document processing");
                    throw;
                }
            });

            outputStream.Position = 0;
            return outputStream;
        }



        private class RemovalInfo
        {
            public HashSet<Paragraph> ParagraphsToRemove { get; } = new HashSet<Paragraph>();
            public HashSet<int> ArticlesToRemove { get; } = new HashSet<int>();
        }

        // Ultra-basic debug version - let's see what's actually in the document
        private RemovalInfo ProcessPlaceholders(Document document, string correlationId)
        {
            var removalInfo = new RemovalInfo();
            var body = document.Body;
            if (body == null)
            {
                _logger.LogError($"[{correlationId}] Document body is null!");
                return removalInfo;
            }

            _logger.LogInformation($"[{correlationId}] === ULTRA-BASIC DEBUG START ===");

            // Step 1: Just list ALL paragraphs and their text
            var allParagraphs = body.Descendants<Paragraph>().ToList();
            _logger.LogInformation($"[{correlationId}] Total paragraphs found: {allParagraphs.Count}");

            for (int i = 0; i < Math.Min(20, allParagraphs.Count); i++)
            {
                var para = allParagraphs[i];
                var text = GetParagraphText(para);
                _logger.LogInformation($"[{correlationId}] Para {i}: '{text}'");

                // Check if this paragraph contains ^ or #
                if (text.Contains("^"))
                {
                    _logger.LogInformation($"[{correlationId}] *** FOUND ^ in paragraph {i}: '{text}'");
                }
                if (text.Contains("#"))
                {
                    _logger.LogInformation($"[{correlationId}] *** FOUND # in paragraph {i}: '{text}'");
                }
            }

            // Step 2: Look for content controls
            var contentControls = body.Descendants<SdtElement>().ToList();
            _logger.LogInformation($"[{correlationId}] Total content controls found: {contentControls.Count}");

            for (int i = 0; i < contentControls.Count; i++)
            {
                var sdt = contentControls[i];
                var text = GetContentControlText(sdt);
                _logger.LogInformation($"[{correlationId}] ContentControl {i}: '{text}'");
            }

            // Step 3: VERY SIMPLE approach - just find ^ and # anywhere
            bool foundCaret = false;
            bool foundHash = false;

            foreach (var paragraph in allParagraphs)
            {
                var text = GetParagraphText(paragraph).Trim();

                // Just look for exact matches
                if (text == "^")
                {
                    _logger.LogInformation($"[{correlationId}] FOUND EXACT ^ MATCH: '{text}'");
                    foundCaret = true;

                    // Very simple: assume this is article 1 for now
                    removalInfo.ArticlesToRemove.Add(1);
                }

                if (text == "#")
                {
                    _logger.LogInformation($"[{correlationId}] FOUND EXACT # MATCH: '{text}'");
                    foundHash = true;

                    // Just mark this paragraph for removal
                    removalInfo.ParagraphsToRemove.Add(paragraph);
                }
            }

            _logger.LogInformation($"[{correlationId}] SUMMARY:");
            _logger.LogInformation($"[{correlationId}]   Found ^ anywhere: {foundCaret}");
            _logger.LogInformation($"[{correlationId}]   Found # anywhere: {foundHash}");
            _logger.LogInformation($"[{correlationId}]   Articles to remove: [{string.Join(", ", removalInfo.ArticlesToRemove)}]");
            _logger.LogInformation($"[{correlationId}]   Paragraphs to remove: {removalInfo.ParagraphsToRemove.Count}");

            return removalInfo;
        }

        // Ultra-simple removal that just removes what we found
        private void RemoveMarkedElements(Document document, RemovalInfo removalInfo)
        {
            var body = document.Body;
            if (body == null) return;

            _logger.LogInformation($"Starting removal...");
            _logger.LogInformation($"Articles to remove: [{string.Join(", ", removalInfo.ArticlesToRemove)}]");
            _logger.LogInformation($"Individual paragraphs to remove: {removalInfo.ParagraphsToRemove.Count}");

            var allParagraphs = body.Descendants<Paragraph>().ToList();
            var paragraphsToRemove = new List<Paragraph>();

            // Step 1: Remove individual paragraphs marked with #
            foreach (var paragraph in removalInfo.ParagraphsToRemove)
            {
                paragraphsToRemove.Add(paragraph);
                var text = GetParagraphText(paragraph);
                _logger.LogInformation($"Will remove # paragraph: '{text}'");
            }

            // Step 2: If article 1 is marked for removal, remove everything that looks like it belongs to article 1
            if (removalInfo.ArticlesToRemove.Contains(1))
            {
                _logger.LogInformation($"Article 1 is marked for removal, looking for article 1 content...");

                bool inArticle1 = false;

                foreach (var paragraph in allParagraphs)
                {
                    var text = GetParagraphText(paragraph);

                    // Very simple detection: if paragraph starts with "1." it's article 1
                    if (text.Trim().StartsWith("1."))
                    {
                        inArticle1 = true;
                        _logger.LogInformation($"Found start of article 1: '{text}'");
                    }

                    // If paragraph starts with "2." we're out of article 1
                    if (text.Trim().StartsWith("2."))
                    {
                        inArticle1 = false;
                        _logger.LogInformation($"Found start of article 2, leaving article 1: '{text}'");
                    }

                    // If we're in article 1, remove this paragraph
                    if (inArticle1)
                    {
                        paragraphsToRemove.Add(paragraph);
                        _logger.LogInformation($"Removing (article 1): '{text}'");
                    }
                }
            }

            // Actually remove the paragraphs
            _logger.LogInformation($"About to remove {paragraphsToRemove.Count} paragraphs total");

            foreach (var paragraph in paragraphsToRemove)
            {
                try
                {
                    paragraph.Remove();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error removing paragraph: {ex.Message}");
                }
            }

            _logger.LogInformation($"Removal completed");
        }

        // Simple text extraction
        private string GetParagraphText(Paragraph paragraph)
        {
            try
            {
                var texts = paragraph.Descendants<Text>().Select(t => t.Text);
                return string.Join("", texts).Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting paragraph text: {ex.Message}");
                return "";
            }
        }

        // Simple content control text extraction  
        private string GetContentControlText(SdtElement sdt)
        {
            try
            {
                var texts = sdt.Descendants<Text>().Select(t => t.Text);
                var result = string.Join("", texts).Trim();

                // Remove formatting artifacts
                result = result.Replace("**", "").Replace("*", "");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting content control text: {ex.Message}");
                return "";
            }
        }
        private void RemoveContentControls(Document document, string correlationId)
        {
            var sdtElements = document.Descendants<SdtElement>().ToList();
            _logger.LogInformation($"[{correlationId}] Removing {sdtElements.Count} content controls");

            // Process from bottom to top to avoid issues with nested content controls
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                var parent = sdt.Parent;
                if (parent == null) continue;

                // Extract content from the content control
                var contentElements = sdt.Elements().Where(e => e.LocalName == "sdtContent").FirstOrDefault();
                if (contentElements == null) continue;

                var contentToPreserve = contentElements.ChildElements.ToList();

                // Insert preserved content before the content control
                foreach (var child in contentToPreserve)
                {
                    var clonedChild = child.CloneNode(true);

                    // Clean up formatting
                    foreach (var run in clonedChild.Descendants<Run>())
                    {
                        var runProps = run.RunProperties ?? run.AppendChild(new RunProperties());

                        // Remove any gray color
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

                    parent.InsertBefore(clonedChild, sdt);
                }

                // Remove the content control
                parent.RemoveChild(sdt);
            }
        }
    }
}