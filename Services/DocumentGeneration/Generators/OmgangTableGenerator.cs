using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Generates omgang (visitation) tables - one table per week arrangement
    /// </summary>
    public class OmgangTableGenerator : ITableGenerator
    {
        private readonly ILogger<OmgangTableGenerator> _logger;

        public string PlaceholderTag => "[[TABEL_OMGANG]]";

        public OmgangTableGenerator(ILogger<OmgangTableGenerator> logger)
        {
            _logger = logger;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            if (data.Omgang == null || data.Omgang.Count == 0)
            {
                _logger.LogWarning($"[{correlationId}] No omgang data available");
                return elements;
            }

            // Group by week arrangement
            var groupedByWeek = data.Omgang.GroupBy(o => o.WeekRegelingId).OrderBy(g => g.Key);

            foreach (var weekGroup in groupedByWeek)
            {
                var weekRegeling = weekGroup.First().EffectieveRegeling;

                // Add heading for this week arrangement
                elements.Add(OpenXmlHelper.CreateStyledHeading(weekRegeling));

                // Create table for this week
                var table = CreateOmgangTable(weekGroup.ToList(), data.Partijen, correlationId);
                elements.Add(table);

                // Add spacing
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            _logger.LogInformation($"[{correlationId}] Generated {groupedByWeek.Count()} omgang tables");
            return elements;
        }

        private Table CreateOmgangTable(List<OmgangData> weekData, List<PersonData> partijen, string correlationId)
        {
            // Create table with 6 columns
            var columnWidths = new[] { 1200, 1000, 1000, 1000, 1000, 800 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.Blue, columnWidths);

            // Add header row
            var headers = new[] { "Dag", "Ochtend", "Middag", "Avond", "Nacht", "Wissel" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.Blue, OpenXmlHelper.Colors.White);
            table.Append(headerRow);

            // Days of the week
            string[] dagen = { "Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag", "Zaterdag", "Zondag" };

            // Process each day (1-7)
            for (int dagId = 1; dagId <= 7; dagId++)
            {
                var row = new TableRow();

                // Day name cell
                var dagNaam = dagId <= dagen.Length ? dagen[dagId - 1] : $"Dag {dagId}";
                row.Append(OpenXmlHelper.CreateStyledCell(dagNaam, isBold: true, bgColor: OpenXmlHelper.Colors.LightGray));

                // Morning (1), Afternoon (2), Evening (3), Night (4) cells
                string? dayWisseltijd = null;
                for (int dagdeelId = 1; dagdeelId <= 4; dagdeelId++)
                {
                    var omgang = weekData.FirstOrDefault(o => o.DagId == dagId && o.DagdeelId == dagdeelId);
                    if (omgang != null)
                    {
                        // Get person's roepnaam or voornamen
                        var persoon = partijen.FirstOrDefault(p => p.Id == omgang.VerzorgerId);
                        var naam = persoon?.Roepnaam ?? persoon?.Voornamen ?? "";
                        row.Append(OpenXmlHelper.CreateStyledCell(naam));

                        // Capture wisseltijd if present
                        if (!string.IsNullOrEmpty(omgang.WisselTijd) && string.IsNullOrEmpty(dayWisseltijd))
                        {
                            dayWisseltijd = omgang.WisselTijd;
                        }
                    }
                    else
                    {
                        row.Append(OpenXmlHelper.CreateStyledCell(""));
                    }
                }

                // Add wisseltijd column
                row.Append(OpenXmlHelper.CreateStyledCell(dayWisseltijd ?? ""));

                table.Append(row);
            }

            return table;
        }
    }
}