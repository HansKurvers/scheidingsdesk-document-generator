using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Generates vakanties (school holidays) table with standard Dutch school vacation schedule
    /// </summary>
    public class VakantieTableGenerator : ITableGenerator
    {
        private readonly ILogger<VakantieTableGenerator> _logger;

        public string PlaceholderTag => "[[TABEL_VAKANTIES]]";

        public VakantieTableGenerator(ILogger<VakantieTableGenerator> logger)
        {
            _logger = logger;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            // Create table with 3 columns
            var columnWidths = new[] { 2500, 1750, 1750 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.Green, columnWidths);

            // Add header row
            var headers = new[] { "Schoolvakantie", "Even jaren", "Oneven jaren" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.Green, OpenXmlHelper.Colors.White);
            table.Append(headerRow);

            // Standard Dutch school holidays
            var vakanties = new[]
            {
                new { Naam = "Voorjaarsvakantie", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "Meivakantie", Even = "Ouder 2", Oneven = "Ouder 1" },
                new { Naam = "Zomervakantie - Week 1-3", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "Zomervakantie - Week 4-6", Even = "Ouder 2", Oneven = "Ouder 1" },
                new { Naam = "Herfstvakantie", Even = "Ouder 1", Oneven = "Ouder 2" },
                new { Naam = "Kerstvakantie", Even = "Ouder 2", Oneven = "Ouder 1" }
            };

            bool alternateRow = false;
            foreach (var vakantie in vakanties)
            {
                var row = new TableRow();
                var bgColor = alternateRow ? OpenXmlHelper.Colors.LightGray : null;

                row.Append(OpenXmlHelper.CreateStyledCell(vakantie.Naam, isBold: true, bgColor: bgColor));
                row.Append(OpenXmlHelper.CreateStyledCell(vakantie.Even, bgColor: bgColor));
                row.Append(OpenXmlHelper.CreateStyledCell(vakantie.Oneven, bgColor: bgColor));

                table.Append(row);
                alternateRow = !alternateRow;
            }

            elements.Add(table);
            _logger.LogInformation($"[{correlationId}] Generated vakanties table");

            return elements;
        }
    }
}