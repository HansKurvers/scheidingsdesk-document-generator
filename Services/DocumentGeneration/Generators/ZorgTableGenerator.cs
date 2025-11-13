using DocumentFormat.OpenXml;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Generates zorg (care) tables - one table per category
    /// </summary>
    public class ZorgTableGenerator : ITableGenerator
    {
        private readonly ILogger<ZorgTableGenerator> _logger;

        public string PlaceholderTag => "[[TABEL_ZORG]]";

        public ZorgTableGenerator(ILogger<ZorgTableGenerator> logger)
        {
            _logger = logger;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            if (data.Zorg == null || data.Zorg.Count == 0)
            {
                _logger.LogWarning($"[{correlationId}] No zorg data available");
                return elements;
            }

            // Separate overige afspraken (situatieId 15) from other categories
            var overigeAfspraken = data.Zorg.Where(z => z.ZorgSituatieId == 15).ToList();
            var regularZorg = data.Zorg.Where(z => z.ZorgSituatieId != 15).ToList();

            // First, handle regular zorg categories (grouped)
            var groupedByCategory = regularZorg.GroupBy(z => z.ZorgCategorieNaam).OrderBy(g => g.Key);

            foreach (var categoryGroup in groupedByCategory)
            {
                var categoryName = categoryGroup.Key ?? "Afspraken";

                // Add heading for this category
                elements.Add(OpenXmlHelper.CreateStyledHeading(categoryName));

                // Create table for this category
                var table = CreateZorgTable(categoryGroup.ToList());
                elements.Add(table);

                // Add spacing
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            // Then, handle overige afspraken separately (each gets its own section)
            if (overigeAfspraken.Any())
            {
                // Add main heading for overige afspraken
                elements.Add(OpenXmlHelper.CreateStyledHeading("Overige afspraken"));

                foreach (var overigeAfspraak in overigeAfspraken)
                {
                    // Use SituatieAnders as section title if available
                    if (!string.IsNullOrWhiteSpace(overigeAfspraak.SituatieAnders))
                    {
                        elements.Add(OpenXmlHelper.CreateStyledSubHeading(overigeAfspraak.SituatieAnders));
                    }

                    // Create a single-row table for this afspraak
                    var table = CreateSingleAfspraakTable(overigeAfspraak);
                    elements.Add(table);

                    // Add spacing
                    elements.Add(OpenXmlHelper.CreateEmptyParagraph());
                }
            }

            var totalTables = groupedByCategory.Count() + (overigeAfspraken.Any() ? 1 : 0);
            _logger.LogInformation($"[{correlationId}] Generated {totalTables} zorg sections with {overigeAfspraken.Count} overige afspraken");
            return elements;
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateZorgTable(List<ZorgData> categoryData)
        {
            // Create table with 2 columns
            var columnWidths = new[] { 2500, 3500 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.DarkBlue, columnWidths);

            // Add header row
            var headers = new[] { "Situatie", "Afspraak" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.DarkBlue, OpenXmlHelper.Colors.White);
            table.Append(headerRow);

            // Add data rows
            foreach (var zorg in categoryData)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell(zorg.EffectieveSituatie, alignment: DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Left));
                row.Append(OpenXmlHelper.CreateStyledCell(zorg.Overeenkomst ?? "", alignment: DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Left));
                table.Append(row);
            }

            return table;
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateSingleAfspraakTable(ZorgData afspraak)
        {
            // Create table with 1 column (full width)
            var columnWidths = new[] { 6000 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.DarkBlue, columnWidths);

            // Add single row with the overeenkomst text
            var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
            row.Append(OpenXmlHelper.CreateStyledCell(afspraak.Overeenkomst ?? "", alignment: DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Left));
            table.Append(row);

            return table;
        }
    }
}