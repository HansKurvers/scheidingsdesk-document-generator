using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.Artikel;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Genereert artikelen content voor het ouderschapsplan/convenant
    /// Vervangt de [[ARTIKELEN]] placeholder met alle actieve artikelen
    /// </summary>
    public class ArtikelContentGenerator : ITableGenerator
    {
        private readonly ILogger<ArtikelContentGenerator> _logger;
        private readonly IArtikelService _artikelService;

        /// <summary>
        /// Placeholder replacements die worden ingesteld voor processing
        /// </summary>
        public Dictionary<string, string>? Replacements { get; set; }

        public string PlaceholderTag => "[[ARTIKELEN]]";

        public ArtikelContentGenerator(
            ILogger<ArtikelContentGenerator> logger,
            IArtikelService artikelService)
        {
            _logger = logger;
            _artikelService = artikelService;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            if (data.Artikelen == null || data.Artikelen.Count == 0)
            {
                _logger.LogWarning($"[{correlationId}] Geen artikelen beschikbaar voor document generatie");
                return elements;
            }

            _logger.LogInformation($"[{correlationId}] Genereren van {data.Artikelen.Count} artikelen");

            // Gebruik de replacements voor conditionele filtering
            var replacements = Replacements ?? new Dictionary<string, string>();

            // Filter conditionele artikelen
            var artikelen = _artikelService.FilterConditioneleArtikelen(data.Artikelen, replacements);

            _logger.LogInformation($"[{correlationId}] Na conditionele filtering: {artikelen.Count} artikelen");

            // Sorteer op volgorde
            artikelen = artikelen.OrderBy(a => a.Volgorde).ToList();

            int artikelNummer = 1;

            foreach (var artikel in artikelen)
            {
                // Genereer artikel elementen
                var artikelElements = GenerateArtikelContent(artikel, artikelNummer, replacements, correlationId);
                elements.AddRange(artikelElements);

                artikelNummer++;
            }

            _logger.LogInformation($"[{correlationId}] {artikelNummer - 1} artikelen gegenereerd");
            return elements;
        }

        /// <summary>
        /// Genereert de content voor een enkel artikel
        /// </summary>
        private List<OpenXmlElement> GenerateArtikelContent(
            ArtikelData artikel,
            int nummer,
            Dictionary<string, string> replacements,
            string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            // Verwerk artikel tekst (conditionele blokken en placeholders)
            var verwerkteTekst = _artikelService.VerwerkArtikelTekst(artikel, replacements);

            // Skip artikel als tekst leeg is na verwerking
            if (string.IsNullOrWhiteSpace(verwerkteTekst))
            {
                _logger.LogDebug($"[{correlationId}] Artikel '{artikel.ArtikelCode}' overgeslagen (lege tekst na verwerking)");
                return elements;
            }

            // Artikel kop: "Artikel X: Titel"
            var effectieveTitel = artikel.EffectieveTitel;
            var kopTekst = $"Artikel {nummer}: {effectieveTitel}";

            // Maak heading paragraph
            var heading = CreateArtikelHeading(kopTekst);
            elements.Add(heading);

            // Maak body paragraphs (split op newlines)
            var bodyParagraphs = CreateBodyParagraphs(verwerkteTekst);
            elements.AddRange(bodyParagraphs);

            // Voeg lege regel toe na artikel
            elements.Add(OpenXmlHelper.CreateEmptyParagraph());

            return elements;
        }

        /// <summary>
        /// Maakt een artikel heading met juiste styling
        /// </summary>
        private Paragraph CreateArtikelHeading(string text)
        {
            var paragraph = new Paragraph();

            // Paragraph properties
            var paragraphProps = new ParagraphProperties();

            // Spacing voor heading (ruimte boven)
            paragraphProps.Append(new SpacingBetweenLines()
            {
                Before = "200",  // 10pt ruimte boven
                After = "120"    // 6pt ruimte onder
            });

            paragraph.Append(paragraphProps);

            // Run met bold text
            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new Bold());
            runProps.Append(new FontSize() { Val = "24" }); // 12pt
            run.Append(runProps);
            run.Append(new Text(text));

            paragraph.Append(run);
            return paragraph;
        }

        /// <summary>
        /// Maakt body paragraphs van tekst, gesplitst op newlines
        /// </summary>
        private List<OpenXmlElement> CreateBodyParagraphs(string tekst)
        {
            var paragraphs = new List<OpenXmlElement>();

            // Split tekst op newlines
            var regels = tekst.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            foreach (var regel in regels)
            {
                if (string.IsNullOrWhiteSpace(regel))
                {
                    // Lege regel wordt een spacing paragraph
                    paragraphs.Add(CreateSpacingParagraph());
                }
                else
                {
                    paragraphs.Add(CreateBodyParagraph(regel));
                }
            }

            return paragraphs;
        }

        /// <summary>
        /// Maakt een body paragraph met standaard styling
        /// </summary>
        private Paragraph CreateBodyParagraph(string text)
        {
            var paragraph = new Paragraph();

            // Paragraph properties
            var paragraphProps = new ParagraphProperties();

            // Spacing voor body text
            paragraphProps.Append(new SpacingBetweenLines()
            {
                After = "60",   // 3pt ruimte onder
                Line = "276",   // 1.15 line spacing
                LineRule = LineSpacingRuleValues.Auto
            });

            // Justified alignment
            paragraphProps.Append(new Justification() { Val = JustificationValues.Both });

            paragraph.Append(paragraphProps);

            // Run met text
            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new FontSize() { Val = "22" }); // 11pt
            run.Append(runProps);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            paragraph.Append(run);
            return paragraph;
        }

        /// <summary>
        /// Maakt een lege paragraph voor spacing
        /// </summary>
        private Paragraph CreateSpacingParagraph()
        {
            var paragraph = new Paragraph();
            var paragraphProps = new ParagraphProperties();
            paragraphProps.Append(new SpacingBetweenLines() { After = "0" });
            paragraph.Append(paragraphProps);
            return paragraph;
        }
    }
}
