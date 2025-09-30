using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Generates feestdagen (holidays) table with standard Dutch holidays
    /// </summary>
    public class FeestdagenTableGenerator : ITableGenerator
    {
        private readonly ILogger<FeestdagenTableGenerator> _logger;

        public string PlaceholderTag => "[[TABEL_FEESTDAGEN]]";

        public FeestdagenTableGenerator(ILogger<FeestdagenTableGenerator> logger)
        {
            _logger = logger;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            // Create table with 3 columns
            var columnWidths = new[] { 2500, 1750, 1750 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.Orange, columnWidths);

            // Add header row
            var headers = new[] { "Feestdag", "Even jaren", "Oneven jaren" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.Orange, OpenXmlHelper.Colors.White);
            table.Append(headerRow);

            // Standard Dutch holidays
            var feestdagen = new[]
            {
                new { Naam = "Nieuwjaarsdag", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "Goede Vrijdag", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "1e Paasdag", Even = "Ouder 2", Oneven = "Ouder 1" },
                new { Naam = "2e Paasdag", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "Koningsdag", Even = "Ouder 2", Oneven = "Ouder 1" },
                new { Naam = "Bevrijdingsdag", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "Hemelvaartsdag", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "1e Pinksterdag", Even = "Ouder 2", Oneven = "Ouder 1" },
                new { Naam = "2e Pinksterdag", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "Sinterklaas", Even = "Ouder 2", Oneven = "Ouder 1" },
                new { Naam = "1e Kerstdag", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "2e Kerstdag", Even = "Ouder 2", Oneven = "Ouder 1" },
                new { Naam = "Oudjaarsdag", Even = "Ouder 1", Oneven = "Ouder 2" }
            };

            bool alternateRow = false;
            foreach (var feestdag in feestdagen)
            {
                var row = new TableRow();
                var bgColor = alternateRow ? "FFF2E8" : null;

                row.Append(OpenXmlHelper.CreateStyledCell(feestdag.Naam, isBold: true, bgColor: bgColor));
                row.Append(OpenXmlHelper.CreateStyledCell(feestdag.Even, bgColor: bgColor));
                row.Append(OpenXmlHelper.CreateStyledCell(feestdag.Oneven, bgColor: bgColor));

                table.Append(row);
                alternateRow = !alternateRow;
            }

            elements.Add(table);
            _logger.LogInformation($"[{correlationId}] Generated feestdagen table");

            return elements;
        }
    }
}