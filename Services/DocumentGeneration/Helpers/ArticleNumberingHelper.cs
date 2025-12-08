using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper voor automatische artikelnummering met Word SEQ velden.
    /// SEQ velden zijn onafhankelijk van bullets/lijsten en updaten automatisch.
    ///
    /// Ondersteunt:
    /// - [[ARTIKEL]] → SEQ veld "Artikel 1", "Artikel 2", etc.
    /// - [[SUBARTIKEL]] → SEQ veld "1.1", "1.2", etc.
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
        /// Verwerkt alle artikel placeholders en vervangt ze door SEQ velden.
        /// </summary>
        public static void ProcessArticlePlaceholders(
            WordprocessingDocument document,
            ILogger logger,
            string correlationId)
        {
            logger.LogInformation($"[{correlationId}] Starting article numbering with SEQ fields");

            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                logger.LogWarning($"[{correlationId}] Document has no body");
                return;
            }

            var state = new NumberingState();

            // Verwerk main body
            ProcessContainer(mainPart.Document.Body, state, logger, correlationId);

            // Verwerk headers
            foreach (var headerPart in mainPart.HeaderParts)
            {
                if (headerPart.Header != null)
                {
                    ProcessContainer(headerPart.Header, state, logger, correlationId);
                }
            }

            // Verwerk footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                if (footerPart.Footer != null)
                {
                    ProcessContainer(footerPart.Footer, state, logger, correlationId);
                }
            }

            // Activeer auto-update van velden bij openen
            SeqFieldHelper.EnableAutoUpdateFields(document);

            logger.LogInformation(
                $"[{correlationId}] Article numbering completed. Main articles: {state.ArtikelNumber - 1}, Sub-articles: {state.TotalSubArtikels}");
        }

        private static void ProcessContainer(
            OpenXmlElement container,
            NumberingState state,
            ILogger logger,
            string correlationId)
        {
            // Verwerk alle paragraphs in volgorde
            var paragraphs = container.Descendants<Paragraph>().ToList();

            foreach (var paragraph in paragraphs)
            {
                ProcessParagraph(paragraph, state, logger, correlationId);
            }
        }

        private static void ProcessParagraph(
            Paragraph paragraph,
            NumberingState state,
            ILogger logger,
            string correlationId)
        {
            // We moeten runs individueel verwerken om placeholders te vinden
            // Maak een snapshot van runs omdat we de collectie gaan wijzigen
            var runs = paragraph.Elements<Run>().ToList();

            foreach (var run in runs)
            {
                // Maak een snapshot van text elements
                var textElements = run.Elements<Text>().ToList();

                foreach (var textElement in textElements)
                {
                    var text = textElement.Text;
                    if (string.IsNullOrEmpty(text)) continue;

                    // Check voor [[ARTIKEL_RESET]]
                    if (ArtikelResetPattern.IsMatch(text))
                    {
                        logger.LogDebug($"[{correlationId}] [[ARTIKEL_RESET]] found - resetting counters");
                        state.ArtikelNumber = 1;
                        state.SubArtikelNumber = 1;
                        state.IsFirstSubArtikelInArtikel = true;
                        textElement.Text = ArtikelResetPattern.Replace(text, "");
                        continue;
                    }

                    // Check voor [[ARTIKEL]]
                    if (ArtikelPattern.IsMatch(text))
                    {
                        logger.LogDebug($"[{correlationId}] [[ARTIKEL]] → SEQ field for Artikel {state.ArtikelNumber}");

                        // Bij eerste artikel of na reset, gebruik \r 1 om te resetten
                        int? resetTo = state.ArtikelNumber == 1 ? 1 : null;

                        ReplaceWithSeqField(run, textElement, ArtikelPattern,
                            SeqFieldHelper.CreateSeqField("Artikel", "Artikel ", resetTo));

                        state.CurrentArtikelNumber = state.ArtikelNumber;
                        state.ArtikelNumber++;
                        state.SubArtikelNumber = 1;
                        state.IsFirstSubArtikelInArtikel = true;
                        continue;
                    }

                    // Check voor [[SUBARTIKEL]]
                    if (SubArtikelPattern.IsMatch(text))
                    {
                        logger.LogDebug($"[{correlationId}] [[SUBARTIKEL]] → SEQ field for {state.CurrentArtikelNumber}.{state.SubArtikelNumber}");

                        ReplaceWithSeqField(run, textElement, SubArtikelPattern,
                            SeqFieldHelper.CreateSubArtikelSeqField(
                                state.CurrentArtikelNumber,
                                state.IsFirstSubArtikelInArtikel));

                        state.SubArtikelNumber++;
                        state.TotalSubArtikels++;
                        state.IsFirstSubArtikelInArtikel = false;
                        continue;
                    }

                    // Check voor [[ARTIKEL_NR]] - alleen nummer, geen "Artikel " prefix (platte tekst)
                    if (ArtikelNrPattern.IsMatch(text))
                    {
                        logger.LogDebug($"[{correlationId}] [[ARTIKEL_NR]] → '{state.ArtikelNumber}' (plain text)");
                        textElement.Text = ArtikelNrPattern.Replace(text, state.ArtikelNumber.ToString());

                        state.CurrentArtikelNumber = state.ArtikelNumber;
                        state.ArtikelNumber++;
                        state.SubArtikelNumber = 1;
                        state.IsFirstSubArtikelInArtikel = true;
                        continue;
                    }

                    // Check voor [[SUBARTIKEL_NR]] - volledig subartikel nummer (platte tekst)
                    if (SubArtikelNrPattern.IsMatch(text))
                    {
                        var replacement = $"{state.CurrentArtikelNumber}.{state.SubArtikelNumber}";
                        logger.LogDebug($"[{correlationId}] [[SUBARTIKEL_NR]] → '{replacement}' (plain text)");
                        textElement.Text = SubArtikelNrPattern.Replace(text, replacement);

                        state.SubArtikelNumber++;
                        state.TotalSubArtikels++;
                        state.IsFirstSubArtikelInArtikel = false;
                    }
                }
            }
        }

        /// <summary>
        /// Vervangt een placeholder in een Text element met SEQ veld elementen.
        /// </summary>
        private static void ReplaceWithSeqField(
            Run parentRun,
            Text textElement,
            Regex pattern,
            List<OpenXmlElement> seqFieldElements)
        {
            var text = textElement.Text;
            var match = pattern.Match(text);

            if (!match.Success) return;

            // Tekst voor de placeholder
            var beforeText = text.Substring(0, match.Index);
            // Tekst na de placeholder
            var afterText = text.Substring(match.Index + match.Length);

            // Kopieer formatting van de originele run
            var runProperties = parentRun.RunProperties?.CloneNode(true) as RunProperties;

            // Voeg tekst voor placeholder toe (als die er is)
            if (!string.IsNullOrEmpty(beforeText))
            {
                var beforeRun = new Run();
                if (runProperties != null)
                    beforeRun.RunProperties = runProperties.CloneNode(true) as RunProperties;
                beforeRun.AppendChild(new Text(beforeText) { Space = SpaceProcessingModeValues.Preserve });
                parentRun.InsertBeforeSelf(beforeRun);
            }

            // Voeg SEQ veld elementen toe met dezelfde formatting
            foreach (var element in seqFieldElements)
            {
                if (element is Run seqRun && runProperties != null)
                {
                    // Alleen formatting toevoegen aan runs die tekst of veldcodes bevatten
                    if (seqRun.GetFirstChild<Text>() != null || seqRun.GetFirstChild<FieldCode>() != null)
                    {
                        seqRun.RunProperties = runProperties.CloneNode(true) as RunProperties;
                    }
                }
                parentRun.InsertBeforeSelf(element);
            }

            // Voeg tekst na placeholder toe (als die er is)
            if (!string.IsNullOrEmpty(afterText))
            {
                var afterRun = new Run();
                if (runProperties != null)
                    afterRun.RunProperties = runProperties.CloneNode(true) as RunProperties;
                afterRun.AppendChild(new Text(afterText) { Space = SpaceProcessingModeValues.Preserve });
                parentRun.InsertBeforeSelf(afterRun);
            }

            // Verwijder de originele run
            parentRun.Remove();
        }

        private class NumberingState
        {
            public int ArtikelNumber { get; set; } = 1;
            public int CurrentArtikelNumber { get; set; } = 1;
            public int SubArtikelNumber { get; set; } = 1;
            public int TotalSubArtikels { get; set; } = 0;
            public bool IsFirstSubArtikelInArtikel { get; set; } = true;
        }
    }
}
