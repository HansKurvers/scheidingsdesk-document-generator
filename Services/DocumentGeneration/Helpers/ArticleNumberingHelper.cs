using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper voor automatische artikelnummering in Word documenten.
    /// Vervangt [[ARTIKEL]] placeholders door oplopende nummers.
    /// </summary>
    public static class ArticleNumberingHelper
    {
        private static readonly Regex ArticlePlaceholderPattern =
            new(@"\[\[ARTIKEL\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ArticleNumberPlaceholderPattern =
            new(@"\[\[ARTIKEL_NR\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Vervangt alle [[ARTIKEL]] placeholders door oplopende nummers ("Artikel 1", "Artikel 2", etc.)
        /// </summary>
        public static void ProcessArticlePlaceholders(
            WordprocessingDocument document,
            ILogger logger,
            string correlationId)
        {
            logger.LogInformation($"[{correlationId}] Starting article numbering");

            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                logger.LogWarning($"[{correlationId}] Document has no body");
                return;
            }

            int articleNumber = 1;

            // Process main body
            articleNumber = ProcessElementArticles(mainPart.Document.Body, articleNumber, logger, correlationId);

            // Process headers (meestal geen artikelnummers, maar voor volledigheid)
            foreach (var headerPart in mainPart.HeaderParts)
            {
                if (headerPart.Header != null)
                {
                    articleNumber = ProcessElementArticles(headerPart.Header, articleNumber, logger, correlationId);
                }
            }

            // Process footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                if (footerPart.Footer != null)
                {
                    articleNumber = ProcessElementArticles(footerPart.Footer, articleNumber, logger, correlationId);
                }
            }

            logger.LogInformation($"[{correlationId}] Article numbering completed. Total articles: {articleNumber - 1}");
        }

        private static int ProcessElementArticles(
            OpenXmlElement element,
            int startNumber,
            ILogger logger,
            string correlationId)
        {
            int currentNumber = startNumber;

            // Zoek alle Text elements die [[ARTIKEL]] bevatten
            var textElements = element.Descendants<Text>()
                .Where(t => ArticlePlaceholderPattern.IsMatch(t.Text))
                .ToList();

            foreach (var textElement in textElements)
            {
                var originalText = textElement.Text;

                // Vervang alle placeholders in deze text node
                int tempNumber = currentNumber;
                textElement.Text = ArticlePlaceholderPattern.Replace(originalText, m =>
                {
                    var replacement = $"Artikel {tempNumber}";
                    logger.LogDebug($"[{correlationId}] Replacing [[ARTIKEL]] with '{replacement}'");
                    tempNumber++;
                    return replacement;
                });

                // Update currentNumber voor volgende text element
                var matchCount = ArticlePlaceholderPattern.Matches(originalText).Count;
                currentNumber += matchCount;
            }

            return currentNumber;
        }

        /// <summary>
        /// Alternatieve versie die alleen het nummer vervangt (voor als "Artikel" al in template staat)
        /// Gebruik [[ARTIKEL_NR]] in template
        /// </summary>
        public static void ProcessArticleNumberPlaceholders(
            WordprocessingDocument document,
            ILogger logger,
            string correlationId)
        {
            logger.LogInformation($"[{correlationId}] Starting article number processing");

            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                logger.LogWarning($"[{correlationId}] Document has no body");
                return;
            }

            int articleNumber = 1;

            var textElements = mainPart.Document.Body.Descendants<Text>()
                .Where(t => ArticleNumberPlaceholderPattern.IsMatch(t.Text))
                .ToList();

            foreach (var textElement in textElements)
            {
                var originalText = textElement.Text;
                textElement.Text = ArticleNumberPlaceholderPattern.Replace(originalText, m =>
                {
                    var num = articleNumber.ToString();
                    logger.LogDebug($"[{correlationId}] Replacing [[ARTIKEL_NR]] with '{num}'");
                    articleNumber++;
                    return num;
                });
            }

            logger.LogInformation($"[{correlationId}] Article number processing completed. Total: {articleNumber - 1}");
        }
    }
}
