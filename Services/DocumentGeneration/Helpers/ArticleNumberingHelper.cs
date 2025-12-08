using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper voor automatische artikelnummering met Word native nummering.
    /// Nummering past zich automatisch aan bij verwijderen/toevoegen in Word.
    ///
    /// Ondersteunt:
    /// - [[ARTIKEL]] → Word native nummering "Artikel 1", "Artikel 2", etc.
    /// - [[SUBARTIKEL]] → Word native nummering "1.1", "1.2", etc.
    /// - [[ARTIKEL_NR]] → Alleen nummer (platte tekst, voor referenties)
    /// - [[SUBARTIKEL_NR]] → Alleen subnummer (platte tekst, voor referenties)
    /// - [[ARTIKEL_RESET]] → Reset alle tellers naar 1
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
        /// Verwerkt alle artikel placeholders en past Word native nummering toe.
        /// </summary>
        public static void ProcessArticlePlaceholders(
            WordprocessingDocument document,
            ILogger logger,
            string correlationId)
        {
            logger.LogInformation($"[{correlationId}] Starting article numbering with Word native lists");

            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                logger.LogWarning($"[{correlationId}] Document has no body");
                return;
            }

            // Stap 1: Zorg dat numbering definitions bestaan
            NumberingDefinitionHelper.EnsureNumberingDefinitions(document);

            // Stap 2: Verwerk placeholders
            var state = new NumberingState();
            ProcessBodyParagraphs(document, mainPart.Document.Body, state, logger, correlationId);

            // Process headers
            foreach (var headerPart in mainPart.HeaderParts)
            {
                if (headerPart.Header != null)
                {
                    ProcessBodyParagraphs(document, headerPart.Header, state, logger, correlationId);
                }
            }

            // Process footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                if (footerPart.Footer != null)
                {
                    ProcessBodyParagraphs(document, footerPart.Footer, state, logger, correlationId);
                }
            }

            logger.LogInformation(
                $"[{correlationId}] Article numbering completed. Main articles: {state.MainArticleNumber - 1}, Sub-articles: {state.TotalSubArticles}");
        }

        private static void ProcessBodyParagraphs(
            WordprocessingDocument document,
            OpenXmlElement container,
            NumberingState state,
            ILogger logger,
            string correlationId)
        {
            // We moeten paragraph-voor-paragraph werken om de volgorde te behouden
            var paragraphs = container.Descendants<Paragraph>().ToList();

            foreach (var paragraph in paragraphs)
            {
                var text = GetParagraphText(paragraph);

                // Check voor [[ARTIKEL_RESET]]
                if (ArticleResetPattern.IsMatch(text))
                {
                    logger.LogDebug($"[{correlationId}] [[ARTIKEL_RESET]] found - resetting counters");
                    state.MainArticleNumber = 1;
                    state.CurrentMainArticle = 1;
                    state.SubArticleNumber = 1;
                    state.NextNumId = 2001;

                    // Verwijder de reset placeholder
                    RemovePlaceholderFromParagraph(paragraph, ArticleResetPattern);

                    // Als paragraph nu leeg is, verwijder hem
                    if (string.IsNullOrWhiteSpace(GetParagraphText(paragraph)))
                    {
                        paragraph.Remove();
                    }
                    continue;
                }

                // Check voor [[ARTIKEL]]
                if (ArticlePattern.IsMatch(text))
                {
                    logger.LogDebug($"[{correlationId}] [[ARTIKEL]] → applying Word numbering for article {state.MainArticleNumber}");

                    // Verwijder "[[ARTIKEL]]" uit de tekst (laat rest staan, bijv. " - Respectvol ouderschap")
                    RemovePlaceholderFromParagraph(paragraph, ArticlePattern);

                    // Voeg numbering toe aan paragraph
                    ApplyArtikelNumbering(paragraph);

                    // Reset subartikel nummering voor dit nieuwe artikel
                    state.CurrentSubArtikelNumId = NumberingDefinitionHelper.CreateRestartedSubArtikelNumbering(
                        document,
                        state.MainArticleNumber,
                        state.NextNumId++);

                    state.CurrentMainArticle = state.MainArticleNumber;
                    state.SubArticleNumber = 1;
                    state.MainArticleNumber++;
                    continue;
                }

                // Check voor [[ARTIKEL_NR]] - alleen nummer (platte tekst voor referenties)
                if (ArticleNumberPattern.IsMatch(text))
                {
                    foreach (var textNode in paragraph.Descendants<Text>())
                    {
                        if (ArticleNumberPattern.IsMatch(textNode.Text))
                        {
                            var replacement = state.MainArticleNumber.ToString();
                            logger.LogDebug($"[{correlationId}] [[ARTIKEL_NR]] → '{replacement}'");
                            textNode.Text = ArticleNumberPattern.Replace(textNode.Text, replacement);

                            state.CurrentMainArticle = state.MainArticleNumber;
                            state.SubArticleNumber = 1;
                            state.MainArticleNumber++;
                        }
                    }
                    continue;
                }

                // Check voor [[SUBARTIKEL]]
                if (SubArticlePattern.IsMatch(text))
                {
                    logger.LogDebug($"[{correlationId}] [[SUBARTIKEL]] → applying Word numbering {state.CurrentMainArticle}.{state.SubArticleNumber}");

                    // Verwijder "[[SUBARTIKEL]]" uit de tekst
                    RemovePlaceholderFromParagraph(paragraph, SubArticlePattern);

                    // Voeg subartikel numbering toe
                    ApplySubArtikelNumbering(paragraph, state.CurrentSubArtikelNumId);

                    state.SubArticleNumber++;
                    state.TotalSubArticles++;
                    continue;
                }

                // Check voor [[SUBARTIKEL_NR]] - platte tekst voor referenties
                if (SubArticleNumberPattern.IsMatch(text))
                {
                    foreach (var textNode in paragraph.Descendants<Text>())
                    {
                        if (SubArticleNumberPattern.IsMatch(textNode.Text))
                        {
                            var replacement = $"{state.CurrentMainArticle}.{state.SubArticleNumber}";
                            logger.LogDebug($"[{correlationId}] [[SUBARTIKEL_NR]] → '{replacement}'");
                            textNode.Text = SubArticleNumberPattern.Replace(textNode.Text, replacement);

                            state.SubArticleNumber++;
                            state.TotalSubArticles++;
                        }
                    }
                }
            }
        }

        private static string GetParagraphText(Paragraph paragraph)
        {
            return string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
        }

        private static void RemovePlaceholderFromParagraph(Paragraph paragraph, Regex pattern)
        {
            foreach (var text in paragraph.Descendants<Text>())
            {
                if (pattern.IsMatch(text.Text))
                {
                    text.Text = pattern.Replace(text.Text, "");
                }
            }
        }

        /// <summary>
        /// Past Word nummering toe op een artikel paragraph.
        /// </summary>
        private static void ApplyArtikelNumbering(Paragraph paragraph)
        {
            // Zorg dat paragraph properties bestaan
            var pPr = paragraph.ParagraphProperties;
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                paragraph.InsertAt(pPr, 0);
            }

            // Verwijder bestaande numbering als die er is
            var existingNumPr = pPr.NumberingProperties;
            existingNumPr?.Remove();

            // Voeg onze numbering toe
            var numPr = NumberingDefinitionHelper.CreateArtikelNumberingProperties();
            pPr.Append(numPr);
        }

        /// <summary>
        /// Past Word nummering toe op een subartikel paragraph.
        /// </summary>
        private static void ApplySubArtikelNumbering(Paragraph paragraph, int numId)
        {
            var pPr = paragraph.ParagraphProperties;
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                paragraph.InsertAt(pPr, 0);
            }

            var existingNumPr = pPr.NumberingProperties;
            existingNumPr?.Remove();

            // Gebruik de specifieke numId voor dit artikel (voor juiste prefix)
            var numPr = new NumberingProperties(
                new NumberingLevelReference { Val = 0 },
                new NumberingId { Val = numId }
            );
            pPr.Append(numPr);
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

            /// <summary>Huidige subartikel numId (voor Word numbering)</summary>
            public int CurrentSubArtikelNumId { get; set; } = NumberingDefinitionHelper.SubArtikelNumberingId;

            /// <summary>Volgende vrije numId voor dynamische subartikel numbering instances</summary>
            public int NextNumId { get; set; } = 2001;
        }
    }
}
