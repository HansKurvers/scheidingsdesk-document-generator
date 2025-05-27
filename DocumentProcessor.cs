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

        // Fixed GetArticleNumber - only count NON-INDENTED numbered items as main articles
        private int GetArticleNumber(Paragraph paragraph)
        {
            var text = GetParagraphText(paragraph);

            // Check if this is a sub-article (starts with whitespace)
            if (SubArticlePattern.IsMatch(text))
            {
                _logger.LogDebug($"Skipping sub-article: '{text}'");
                return 0; // This is a sub-article, not a main article
            }

            // Check if this is a main article (starts at beginning of line)
            var match = MainArticlePattern.Match(text);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                _logger.LogDebug($"Found main article {number}: '{text}'");
                return number;
            }

            return 0;
        }

        // Alternative: Even simpler approach - just check indentation
        private int GetArticleNumberSimple(Paragraph paragraph)
        {
            var text = GetParagraphText(paragraph);

            // Only consider items that DON'T start with whitespace as main articles
            if (text.StartsWith(" ") || text.StartsWith("\t"))
            {
                return 0; // Indented = sub-article
            }

            // Look for pattern at start of line only
            var match = Regex.Match(text, @"^(\d+)\.");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                return number;
            }

            return 0;
        }
        // Updated ProcessPlaceholders to handle your document structure
        private RemovalInfo ProcessPlaceholders(Document document, string correlationId)
        {
            var removalInfo = new RemovalInfo();
            var body = document.Body;
            if (body == null) return removalInfo;

            // First, let's see what we're working with
            var allParagraphs = body.Descendants<Paragraph>().ToList();
            _logger.LogInformation($"[{correlationId}] Total paragraphs: {allParagraphs.Count}");

            // Find ^ markers and determine which MAIN article they belong to
            foreach (var paragraph in allParagraphs)
            {
                var text = GetParagraphText(paragraph).Trim();

                if (text == "^")
                {
                    _logger.LogInformation($"[{correlationId}] Found ^ marker in: '{text}'");

                    // Find which main article this belongs to by searching backwards
                    var articleNum = FindMainArticleForParagraph(paragraph, allParagraphs);
                    if (articleNum > 0)
                    {
                        removalInfo.ArticlesToRemove.Add(articleNum);
                        _logger.LogInformation($"[{correlationId}] ^ marker belongs to main article {articleNum} - will remove");
                    }
                }

                if (text == "#")
                {
                    removalInfo.ParagraphsToRemove.Add(paragraph);
                    _logger.LogInformation($"[{correlationId}] Found # marker - will remove this paragraph only");
                }
            }

            return removalInfo;
        }

        // Helper to find which MAIN article a paragraph belongs to
        private int FindMainArticleForParagraph(Paragraph targetParagraph, List<Paragraph> allParagraphs)
        {
            var index = allParagraphs.IndexOf(targetParagraph);

            // Search backwards to find the most recent MAIN article (non-indented)
            for (int i = index; i >= 0; i--)
            {
                var text = GetParagraphText(allParagraphs[i]);

                // Skip indented items (sub-articles)
                if (text.StartsWith(" ") || text.StartsWith("\t"))
                {
                    continue;
                }

                // Look for main article pattern
                var match = Regex.Match(text, @"^(\d+)\.");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int articleNum))
                {
                    _logger.LogDebug($"Found ^ belongs to main article {articleNum}");
                    return articleNum;
                }
            }

            return 0;
        }

        private void RemoveMarkedElements(Document document, RemovalInfo removalInfo)
        {
            var body = document.Body;
            if (body == null) return;

            var paragraphsToProcess = body.Descendants<Paragraph>().ToList();
            var currentArticle = 0;
            var paragraphsToRemove = new List<Paragraph>();

            foreach (var paragraph in paragraphsToProcess)
            {
                var text = GetParagraphText(paragraph);

                // Check if this is a main article
                var mainMatch = MainArticlePattern.Match(text);
                if (mainMatch.Success)
                {
                    currentArticle = int.Parse(mainMatch.Groups[1].Value);
                }

                // Remove if:
                // 1. It's explicitly marked for removal
                // 2. It belongs to an article marked for removal
                if (removalInfo.ParagraphsToRemove.Contains(paragraph) ||
                    (currentArticle > 0 && removalInfo.ArticlesToRemove.Contains(currentArticle)))
                {
                    paragraphsToRemove.Add(paragraph);
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