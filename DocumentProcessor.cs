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
using DocumentFormat.OpenXml.Wordprocessing;


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

                        // Step 3: Renumber articles
                        RenumberArticles(doc.MainDocumentPart.Document, correlationId);

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

        // Fixed article detection that checks Word list levels
        private int GetArticleNumber(Paragraph paragraph)
        {
            var text = GetParagraphText(paragraph);

            // First check if this paragraph has numbering
            var numberingProperties = paragraph.ParagraphProperties?.NumberingProperties;
            if (numberingProperties == null)
            {
                return 0; // No numbering = not an article
            }

            // Get the list level (ilvl = indentation level)
            var level = numberingProperties.NumberingLevelReference?.Val?.Value ?? 0;

            _logger.LogDebug($"Paragraph '{text}' has numbering level: {level}");

            // Only consider level 0 (main articles), ignore level 1+ (sub-articles)
            if (level == 0)
            {
                // This is a main article, extract the number
                var match = Regex.Match(text.Trim(), @"^(\d+)\.");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    _logger.LogDebug($"Found main article {number} at list level {level}");
                    return number;
                }
            }
            else
            {
                _logger.LogDebug($"Skipping sub-article at level {level}: '{text}'");
            }

            return 0;
        }
        // Alternative: Get detailed numbering information
        private (int articleNumber, int listLevel, bool isMainArticle) GetNumberingInfo(Paragraph paragraph)
        {
            var text = GetParagraphText(paragraph);
            var numberingProperties = paragraph.ParagraphProperties?.NumberingProperties;

            if (numberingProperties == null)
            {
                return (0, -1, false);
            }

            var level = numberingProperties.NumberingLevelReference?.Val?.Value ?? 0;
            var isMainArticle = level == 0;

            // Extract number from text
            var match = Regex.Match(text.Trim(), @"^(\d+)\.");
            var articleNumber = match.Success && int.TryParse(match.Groups[1].Value, out int num) ? num : 0;

            return (articleNumber, level, isMainArticle);
        }

        // Updated ProcessPlaceholders that understands list levels
        private RemovalInfo ProcessPlaceholders(Document document, string correlationId)
        {
            var removalInfo = new RemovalInfo();
            var body = document.Body;
            if (body == null) return removalInfo;

            _logger.LogInformation($"[{correlationId}] === PROCESSING WITH LIST LEVEL DETECTION ===");

            // Debug: Show list structure
            var allParagraphs = body.Descendants<Paragraph>().ToList();
            for (int i = 0; i < Math.Min(15, allParagraphs.Count); i++)
            {
                var para = allParagraphs[i];
                var text = GetParagraphText(para);
                var (articleNum, listLevel, isMain) = GetNumberingInfo(para);

                if (articleNum > 0)
                {
                    _logger.LogInformation($"[{correlationId}] Para {i}: '{text}' → Article {articleNum}, Level {listLevel}, Main: {isMain}");
                }
            }

            // Find ^ and # markers
            foreach (var paragraph in allParagraphs)
            {
                var text = GetParagraphText(paragraph).Trim();

                if (text == "^")
                {
                    _logger.LogInformation($"[{correlationId}] Found ^ marker");

                    // Find which main article this belongs to
                    var articleNum = FindMainArticleForParagraph(paragraph, allParagraphs);
                    if (articleNum > 0)
                    {
                        removalInfo.ArticlesToRemove.Add(articleNum);
                        _logger.LogInformation($"[{correlationId}] ^ belongs to main article {articleNum} - will remove");
                    }
                }

                if (text == "#")
                {
                    _logger.LogInformation($"[{correlationId}] Found # marker - will remove this paragraph");
                    removalInfo.ParagraphsToRemove.Add(paragraph);
                }
            }

            _logger.LogInformation($"[{correlationId}] RESULT: Will remove articles [{string.Join(", ", removalInfo.ArticlesToRemove)}]");
            return removalInfo;
        }

        // Helper to find main article for any paragraph
        private int FindMainArticleForParagraph(Paragraph targetParagraph, List<Paragraph> allParagraphs)
        {
            var index = allParagraphs.IndexOf(targetParagraph);

            // Search backwards to find the most recent main article (list level 0)
            for (int i = index; i >= 0; i--)
            {
                var paragraph = allParagraphs[i];
                var (articleNum, listLevel, isMain) = GetNumberingInfo(paragraph);

                if (isMain && articleNum > 0)
                {
                    _logger.LogDebug($"Found ^ belongs to main article {articleNum}");
                    return articleNum;
                }
            }

            return 0;
        }

        // Fixed RemoveMarkedElements that actually uses GetArticleNumber
        private void RemoveMarkedElements(Document document, RemovalInfo removalInfo)
        {
            var body = document.Body;
            if (body == null) return;

            var paragraphsToProcess = body.Descendants<Paragraph>().ToList();
            var currentArticle = 0;
            var paragraphsToRemove = new List<Paragraph>();

            _logger.LogInformation($"About to remove articles: [{string.Join(", ", removalInfo.ArticlesToRemove)}]");

            foreach (var paragraph in paragraphsToProcess)
            {
                // USE GetArticleNumber instead of direct regex!
                var articleNumber = GetArticleNumber(paragraph);
                if (articleNumber > 0)
                {
                    currentArticle = articleNumber;
                    var text = GetParagraphText(paragraph);
                    _logger.LogDebug($"Found article {currentArticle}: {text}");
                }

                // Remove if:
                // 1. It's explicitly marked for removal
                // 2. It belongs to an article marked for removal
                if (removalInfo.ParagraphsToRemove.Contains(paragraph) ||
                    (currentArticle > 0 && removalInfo.ArticlesToRemove.Contains(currentArticle)))
                {
                    paragraphsToRemove.Add(paragraph);
                    var text = GetParagraphText(paragraph);
                    _logger.LogDebug($"Removing (article {currentArticle}): {text}");
                }
            }

            // Remove paragraphs
            foreach (var paragraph in paragraphsToRemove)
            {
                paragraph.Remove();
            }

            _logger.LogInformation($"Removed {paragraphsToRemove.Count} paragraphs");
        }
        private void RenumberArticles(Document document, string correlationId)
        {
            var body = document.Body;
            if (body == null) return;

            var paragraphs = body.Descendants<Paragraph>().ToList();
            var articleMapping = new Dictionary<int, int>(); // old number -> new number
            var currentNewArticle = 1;

            // First pass: build mapping of old to new article numbers
            foreach (var paragraph in paragraphs)
            {
                var text = GetParagraphText(paragraph);
                var mainMatch = MainArticlePattern.Match(text);

                if (mainMatch.Success)
                {
                    var oldArticleNumber = int.Parse(mainMatch.Groups[1].Value);
                    if (!articleMapping.ContainsKey(oldArticleNumber))
                    {
                        articleMapping[oldArticleNumber] = currentNewArticle++;
                    }
                }
            }

            _logger.LogInformation($"[{correlationId}] Article renumbering mapping: {string.Join(", ", articleMapping.Select(kvp => $"{kvp.Key}->{kvp.Value}"))}");

            // Second pass: apply renumbering
            foreach (var paragraph in paragraphs)
            {
                var runs = paragraph.Descendants<Run>().ToList();
                if (!runs.Any()) continue;

                var fullText = string.Join("", runs.Select(r => GetRunText(r)));

                // Check for main article
                var mainMatch = MainArticlePattern.Match(fullText);
                if (mainMatch.Success)
                {
                    var oldNumber = int.Parse(mainMatch.Groups[1].Value);
                    if (articleMapping.TryGetValue(oldNumber, out var newNumber))
                    {
                        ReplaceArticleNumber(paragraph, oldNumber, newNumber, false);
                    }
                    continue;
                }

                // Check for sub-article
                var subMatch = SubArticlePattern.Match(fullText);
                if (subMatch.Success)
                {
                    var mainArticle = int.Parse(subMatch.Groups[2].Value);
                    var subArticle = int.Parse(subMatch.Groups[3].Value);

                    if (articleMapping.TryGetValue(mainArticle, out var newMainNumber))
                    {
                        ReplaceSubArticleNumber(paragraph, mainArticle, subArticle, newMainNumber);
                    }
                }
            }
        }

        private void ReplaceArticleNumber(Paragraph paragraph, int oldNumber, int newNumber, bool isSubArticle)
        {
            foreach (var run in paragraph.Descendants<Run>())
            {
                foreach (var text in run.Descendants<Text>())
                {
                    if (isSubArticle)
                    {
                        text.Text = Regex.Replace(text.Text,
                            @"\b" + oldNumber + @"\.(\d+)",
                            newNumber + ".$1");
                    }
                    else
                    {
                        text.Text = Regex.Replace(text.Text,
                            @"^" + oldNumber + @"\.",
                            newNumber + ".");
                    }
                }
            }
        }

        private void ReplaceSubArticleNumber(Paragraph paragraph, int oldMainNumber, int subNumber, int newMainNumber)
        {
            foreach (var run in paragraph.Descendants<Run>())
            {
                foreach (var text in run.Descendants<Text>())
                {
                    text.Text = Regex.Replace(text.Text,
                        @"\b" + oldMainNumber + @"\." + subNumber + @"\b",
                        newMainNumber + "." + subNumber);
                }
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


        private string GetParagraphText(Paragraph paragraph)
        {
            var texts = paragraph.Descendants<Text>().Select(t => t.Text);
            return string.Join("", texts).Trim();
        }

        private string GetRunText(Run run)
        {
            var texts = run.Descendants<Text>().Select(t => t.Text);
            return string.Join("", texts);
        }
    }
}