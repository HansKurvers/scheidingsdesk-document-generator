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
    /// Ondersteunt hoofdartikelen ([[ARTIKEL]]) en subartikelen ([[SUBARTIKEL]]).
    ///
    /// Voorbeeld output:
    /// - [[ARTIKEL]] → "Artikel 1", "Artikel 2", "Artikel 3"
    /// - [[SUBARTIKEL]] na Artikel 4 → "4.1", "4.2", "4.3"
    /// - [[ARTIKEL]] reset subartikel teller
    /// - [[ARTIKEL_RESET]] reset ALLE tellers naar 1
    /// </summary>
    public static class ArticleNumberingHelper
    {
        // Regex patterns voor verschillende placeholder types
        private static readonly Regex ArticlePattern =
            new(@"\[\[ARTIKEL\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SubArticlePattern =
            new(@"\[\[SUBARTIKEL\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ArticleNumberPattern =
            new(@"\[\[ARTIKEL_NR\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SubArticleNumberPattern =
            new(@"\[\[SUBARTIKEL_NR\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Reset pattern - zet artikelnummer terug naar 1
        private static readonly Regex ArticleResetPattern =
            new(@"\[\[ARTIKEL_RESET\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Verwerkt alle artikel en subartikel placeholders in het document.
        /// Moet in document-volgorde worden verwerkt zodat nummering klopt.
        /// </summary>
        public static void ProcessArticlePlaceholders(
            WordprocessingDocument document,
            ILogger logger,
            string correlationId)
        {
            logger.LogInformation($"[{correlationId}] Starting article and sub-article numbering");

            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                logger.LogWarning($"[{correlationId}] Document has no body");
                return;
            }

            // State voor nummering
            var state = new NumberingState();

            // Process main body (paragraph voor paragraph om volgorde te behouden)
            ProcessElementInOrder(mainPart.Document.Body, state, logger, correlationId);

            // Process headers
            foreach (var headerPart in mainPart.HeaderParts)
            {
                if (headerPart.Header != null)
                {
                    ProcessElementInOrder(headerPart.Header, state, logger, correlationId);
                }
            }

            // Process footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                if (footerPart.Footer != null)
                {
                    ProcessElementInOrder(footerPart.Footer, state, logger, correlationId);
                }
            }

            logger.LogInformation(
                $"[{correlationId}] Article numbering completed. Main articles: {state.MainArticleNumber - 1}, Sub-articles: {state.TotalSubArticles}");
        }

        /// <summary>
        /// Verwerkt element in document-volgorde om correcte nummering te garanderen.
        /// Dit is belangrijk omdat [[ARTIKEL]] de subartikel-teller moet resetten.
        /// </summary>
        private static void ProcessElementInOrder(
            OpenXmlElement element,
            NumberingState state,
            ILogger logger,
            string correlationId)
        {
            // Verzamel alle Text nodes in document-volgorde
            var textNodes = element.Descendants<Text>().ToList();

            foreach (var textNode in textNodes)
            {
                var text = textNode.Text;
                if (string.IsNullOrEmpty(text)) continue;

                // Check voor [[ARTIKEL_RESET]] - reset alle tellers
                if (ArticleResetPattern.IsMatch(text))
                {
                    text = ArticleResetPattern.Replace(text, m =>
                    {
                        logger.LogDebug($"[{correlationId}] [[ARTIKEL_RESET]] - Resetting counters to 1");

                        // Reset alle tellers
                        state.MainArticleNumber = 1;
                        state.CurrentMainArticle = 1;
                        state.SubArticleNumber = 1;

                        return ""; // Verwijder de placeholder uit het document
                    });
                }

                // Check voor [[ARTIKEL]] - hoofdartikel
                if (ArticlePattern.IsMatch(text))
                {
                    text = ArticlePattern.Replace(text, m =>
                    {
                        var replacement = $"Artikel {state.MainArticleNumber}";
                        logger.LogDebug($"[{correlationId}] [[ARTIKEL]] → '{replacement}'");

                        // Reset subartikel teller bij nieuw hoofdartikel
                        state.CurrentMainArticle = state.MainArticleNumber;
                        state.SubArticleNumber = 1;
                        state.MainArticleNumber++;

                        return replacement;
                    });
                }

                // Check voor [[ARTIKEL_NR]] - alleen nummer
                if (ArticleNumberPattern.IsMatch(text))
                {
                    text = ArticleNumberPattern.Replace(text, m =>
                    {
                        var replacement = state.MainArticleNumber.ToString();
                        logger.LogDebug($"[{correlationId}] [[ARTIKEL_NR]] → '{replacement}'");

                        state.CurrentMainArticle = state.MainArticleNumber;
                        state.SubArticleNumber = 1;
                        state.MainArticleNumber++;

                        return replacement;
                    });
                }

                // Check voor [[SUBARTIKEL]] - subartikel met prefix
                if (SubArticlePattern.IsMatch(text))
                {
                    text = SubArticlePattern.Replace(text, m =>
                    {
                        var replacement = $"{state.CurrentMainArticle}.{state.SubArticleNumber}";
                        logger.LogDebug($"[{correlationId}] [[SUBARTIKEL]] → '{replacement}'");

                        state.SubArticleNumber++;
                        state.TotalSubArticles++;

                        return replacement;
                    });
                }

                // Check voor [[SUBARTIKEL_NR]] - zelfde als [[SUBARTIKEL]]
                if (SubArticleNumberPattern.IsMatch(text))
                {
                    text = SubArticleNumberPattern.Replace(text, m =>
                    {
                        var replacement = $"{state.CurrentMainArticle}.{state.SubArticleNumber}";
                        logger.LogDebug($"[{correlationId}] [[SUBARTIKEL_NR]] → '{replacement}'");

                        state.SubArticleNumber++;
                        state.TotalSubArticles++;

                        return replacement;
                    });
                }

                textNode.Text = text;
            }
        }

        /// <summary>
        /// State object voor het bijhouden van nummering tijdens verwerking
        /// </summary>
        private class NumberingState
        {
            /// <summary>Volgende hoofdartikel nummer (1, 2, 3...)</summary>
            public int MainArticleNumber { get; set; } = 1;

            /// <summary>Huidige hoofdartikel (voor subartikel prefix)</summary>
            public int CurrentMainArticle { get; set; } = 1;

            /// <summary>Volgende subartikel nummer binnen huidig hoofdartikel</summary>
            public int SubArticleNumber { get; set; } = 1;

            /// <summary>Totaal aantal verwerkte subartikelen (voor logging)</summary>
            public int TotalSubArticles { get; set; } = 0;
        }
    }
}
