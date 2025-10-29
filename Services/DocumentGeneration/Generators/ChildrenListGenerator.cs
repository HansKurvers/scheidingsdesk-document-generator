using DocumentFormat.OpenXml;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Generates a formatted list of children with their details
    /// </summary>
    public class ChildrenListGenerator : ITableGenerator
    {
        private readonly ILogger<ChildrenListGenerator> _logger;

        public string PlaceholderTag => "[[LIJST_KINDEREN]]";

        public ChildrenListGenerator(ILogger<ChildrenListGenerator> logger)
        {
            _logger = logger;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            if (data.Kinderen == null || data.Kinderen.Count == 0)
            {
                _logger.LogWarning($"[{correlationId}] No children data available");
                return elements;
            }

            foreach (var kind in data.Kinderen)
            {
                // Format: "- Roepnaam (volledige naam), geboren op datum te plaats, leeftijd jaar"
                var roepnaam = kind.Roepnaam ?? kind.Voornamen?.Split(' ')[0] ?? kind.Achternaam;
                var geboortedatum = DataFormatter.FormatDate(kind.GeboorteDatum);

                var text = $"- {roepnaam} ({kind.VolledigeNaam}), geboren op {geboortedatum}";

                if (!string.IsNullOrEmpty(kind.GeboortePlaats))
                {
                    text += $" te {kind.GeboortePlaats}";
                }

                if (kind.Leeftijd.HasValue)
                {
                    text += $", {kind.Leeftijd} jaar";
                }

                elements.Add(OpenXmlHelper.CreateSimpleParagraph(text));
            }

            _logger.LogInformation($"[{correlationId}] Generated list for {data.Kinderen.Count} children");
            return elements;
        }
    }
}