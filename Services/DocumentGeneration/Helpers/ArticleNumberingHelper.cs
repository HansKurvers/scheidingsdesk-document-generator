using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper voor automatische artikelnummering met multi-level juridische lijsten.
    /// Nummering past automatisch aan bij verwijderen/toevoegen - geen F9 nodig!
    ///
    /// Ondersteunt:
    /// - [[ARTIKEL]] → Multi-level list level 0: "Artikel 1", "Artikel 2", etc.
    /// - [[SUBARTIKEL]] → Multi-level list level 1: "1.1", "1.2", etc.
    /// - [[ARTIKEL_NR]] → Alleen nummer (platte tekst, voor referenties)
    /// - [[SUBARTIKEL_NR]] → Alleen subnummer (platte tekst, voor referenties)
    /// - [[ARTIKEL_RESET]] → Reset alle tellers naar 1
    /// </summary>
    public static class ArticleNumberingHelper
    {
        private static readonly Regex ArtikelPattern =
            new(@"\[\[ARTIKEL\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SubArtikelPattern =
            new(@"\[\[SUBARTIKEL\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ArtikelNrPattern =
            new(@"\[\[ARTIKEL_NR\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SubArtikelNrPattern =
            new(@"\[\[SUBARTIKEL_NR\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ArtikelResetPattern =
            new(@"\[\[ARTIKEL_RESET\]\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Verwerkt alle artikel placeholders en past multi-level nummering toe.
        /// </summary>
        public static void ProcessArticlePlaceholders(
            WordprocessingDocument document,
            ILogger logger,
            string correlationId)
        {
            logger.LogInformation($"[{correlationId}] Starting article numbering with multi-level legal list");

            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                logger.LogWarning($"[{correlationId}] Document has no body");
                return;
            }

            // Reset static counters voor nieuw document
            LegalNumberingHelper.ResetCounters();

            // Stap 1: Zorg dat onze nummering definitie bestaat
            LegalNumberingHelper.EnsureLegalNumberingDefinition(document);

            // Stap 2: Verwerk main body
            var state = new NumberingState();
            ProcessContainer(document, mainPart.Document.Body, state, logger, correlationId);

            // Verwerk headers
            foreach (var headerPart in mainPart.HeaderParts)
            {
                if (headerPart.Header != null)
                {
                    ProcessContainer(document, headerPart.Header, state, logger, correlationId);
                }
            }

            // Verwerk footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                if (footerPart.Footer != null)
                {
                    ProcessContainer(document, footerPart.Footer, state, logger, correlationId);
                }
            }

            logger.LogInformation(
                $"[{correlationId}] Article numbering completed. Artikels: {state.ArtikelCount}, SubArtikels: {state.SubArtikelCount}");
        }

        private static void ProcessContainer(
            WordprocessingDocument document,
            OpenXmlElement container,
            NumberingState state,
            ILogger logger,
            string correlationId)
        {
            var paragraphs = container.Descendants<Paragraph>().ToList();

            foreach (var paragraph in paragraphs)
            {
                ProcessParagraph(document, paragraph, state, logger, correlationId);
            }
        }

        private static void ProcessParagraph(
            WordprocessingDocument document,
            Paragraph paragraph,
            NumberingState state,
            ILogger logger,
            string correlationId)
        {
            var paragraphText = GetParagraphText(paragraph);
            if (string.IsNullOrEmpty(paragraphText)) return;

            // Check voor [[ARTIKEL_RESET]]
            if (ArtikelResetPattern.IsMatch(paragraphText))
            {
                logger.LogDebug($"[{correlationId}] [[ARTIKEL_RESET]] found - creating new numbering instance");

                // Maak nieuwe numbering instance die bij 1 begint
                state.CurrentNumId = LegalNumberingHelper.CreateRestartedNumberingInstance(document);

                // Reset ook de interne tellers voor platte tekst referenties
                state.CurrentArtikelNumber = 1;
                state.CurrentSubArtikelNumber = 1;

                // Verwijder de placeholder tekst
                RemovePlaceholderText(paragraph, ArtikelResetPattern);

                // Verwijder paragraph als die nu leeg is
                if (string.IsNullOrWhiteSpace(GetParagraphText(paragraph)))
                {
                    paragraph.Remove();
                }
                return;
            }

            // Check voor [[ARTIKEL]]
            if (ArtikelPattern.IsMatch(paragraphText))
            {
                logger.LogDebug($"[{correlationId}] [[ARTIKEL]] found - applying level 0 numbering");

                // Verwijder placeholder tekst
                RemovePlaceholderText(paragraph, ArtikelPattern);

                // Pas nummering toe (level 0 = artikel)
                ApplyNumbering(paragraph, 0, state.CurrentNumId);

                state.ArtikelCount++;
                state.CurrentArtikelNumber++;
                state.CurrentSubArtikelNumber = 1;  // Reset subartikel teller
                return;
            }

            // Check voor [[SUBARTIKEL]]
            if (SubArtikelPattern.IsMatch(paragraphText))
            {
                logger.LogDebug($"[{correlationId}] [[SUBARTIKEL]] found - applying level 1 numbering");

                // Verwijder placeholder tekst
                RemovePlaceholderText(paragraph, SubArtikelPattern);

                // Pas nummering toe (level 1 = subartikel)
                ApplyNumbering(paragraph, 1, state.CurrentNumId);

                state.SubArtikelCount++;
                state.CurrentSubArtikelNumber++;
                return;
            }

            // Check voor [[ARTIKEL_NR]] - alleen nummer als platte tekst (voor referenties)
            if (ArtikelNrPattern.IsMatch(paragraphText))
            {
                logger.LogDebug($"[{correlationId}] [[ARTIKEL_NR]] → '{state.CurrentArtikelNumber}' (plain text)");

                // Vervang met huidige artikel nummer als platte tekst
                foreach (var text in paragraph.Descendants<Text>())
                {
                    if (ArtikelNrPattern.IsMatch(text.Text))
                    {
                        text.Text = ArtikelNrPattern.Replace(text.Text, state.CurrentArtikelNumber.ToString());
                    }
                }

                state.CurrentArtikelNumber++;
                state.CurrentSubArtikelNumber = 1;
                return;
            }

            // Check voor [[SUBARTIKEL_NR]] - als platte tekst
            if (SubArtikelNrPattern.IsMatch(paragraphText))
            {
                var replacement = $"{state.CurrentArtikelNumber - 1}.{state.CurrentSubArtikelNumber}";
                logger.LogDebug($"[{correlationId}] [[SUBARTIKEL_NR]] → '{replacement}' (plain text)");

                foreach (var text in paragraph.Descendants<Text>())
                {
                    if (SubArtikelNrPattern.IsMatch(text.Text))
                    {
                        text.Text = SubArtikelNrPattern.Replace(text.Text, replacement);
                    }
                }

                state.CurrentSubArtikelNumber++;
                return;
            }
        }

        private static string GetParagraphText(Paragraph paragraph)
        {
            return string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
        }

        private static void RemovePlaceholderText(Paragraph paragraph, Regex pattern)
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
        /// Past Word nummering toe op een paragraph.
        /// </summary>
        private static void ApplyNumbering(Paragraph paragraph, int level, int numId)
        {
            // Zorg dat paragraph properties bestaan
            var pPr = paragraph.ParagraphProperties;
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                paragraph.InsertAt(pPr, 0);
            }

            // Verwijder bestaande nummering als die er is
            var existingNumPr = pPr.NumberingProperties;
            existingNumPr?.Remove();

            // Voeg onze nummering toe
            var numPr = new NumberingProperties(
                new NumberingLevelReference { Val = level },
                new NumberingId { Val = numId }
            );

            // Voeg toe aan het begin van paragraph properties
            pPr.InsertAt(numPr, 0);
        }

        private class NumberingState
        {
            public int CurrentNumId { get; set; } = LegalNumberingHelper.NumberingInstanceId;
            public int ArtikelCount { get; set; } = 0;
            public int SubArtikelCount { get; set; } = 0;
            // Voor platte tekst referenties ([[ARTIKEL_NR]] en [[SUBARTIKEL_NR]])
            public int CurrentArtikelNumber { get; set; } = 1;
            public int CurrentSubArtikelNumber { get; set; } = 1;
        }
    }
}
