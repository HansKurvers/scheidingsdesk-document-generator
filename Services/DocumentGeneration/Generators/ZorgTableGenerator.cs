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

            // Group by category
            var groupedByCategory = data.Zorg.GroupBy(z => z.ZorgCategorieNaam).OrderBy(g => g.Key);

            foreach (var categoryGroup in groupedByCategory)
            {
                var categoryName = categoryGroup.Key ?? "Overige afspraken";

                // Add heading for this category
                elements.Add(OpenXmlHelper.CreateStyledHeading(categoryName));

                // Create table for this category
                var table = CreateZorgTable(categoryGroup.ToList());
                elements.Add(table);

                // Add spacing
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            _logger.LogInformation($"[{correlationId}] Generated {groupedByCategory.Count()} zorg tables");
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
    }
}