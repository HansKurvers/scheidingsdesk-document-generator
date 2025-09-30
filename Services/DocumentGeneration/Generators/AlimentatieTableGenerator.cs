using DocumentFormat.OpenXml;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Generates alimentatie (alimony/financial agreements) table
    /// </summary>
    public class AlimentatieTableGenerator : ITableGenerator
    {
        private readonly ILogger<AlimentatieTableGenerator> _logger;

        public string PlaceholderTag => "[[TABEL_ALIMENTATIE]]";

        public AlimentatieTableGenerator(ILogger<AlimentatieTableGenerator> logger)
        {
            _logger = logger;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            if (data.Alimentatie == null)
            {
                _logger.LogWarning($"[{correlationId}] No alimentatie data available");
                return elements;
            }

            var alimentatie = data.Alimentatie;

            // Add general alimentatie information section
            if (alimentatie.NettoBesteedbaarGezinsinkomen.HasValue ||
                alimentatie.KostenKinderen.HasValue ||
                alimentatie.BijdrageKostenKinderen.HasValue)
            {
                elements.Add(OpenXmlHelper.CreateStyledHeading("Algemene financiële gegevens"));

                var generalTable = CreateGeneralAlimentatieTable(alimentatie);
                elements.Add(generalTable);
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            // Add per person contributions table
            if (alimentatie.BijdragenKostenKinderen.Any())
            {
                elements.Add(OpenXmlHelper.CreateStyledHeading("Eigen aandeel per partij"));

                var contributionsTable = CreateContributionsTable(alimentatie.BijdragenKostenKinderen);
                elements.Add(contributionsTable);
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            // Add per child financial agreements table
            if (alimentatie.FinancieleAfsprakenKinderen.Any() && data.Kinderen.Any())
            {
                elements.Add(OpenXmlHelper.CreateStyledHeading("Financiële afspraken per kind"));

                var childAgreementsTable = CreateChildAgreementsTable(
                    alimentatie.FinancieleAfsprakenKinderen,
                    data.Kinderen,
                    data.Partij1,
                    data.Partij2);
                elements.Add(childAgreementsTable);
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            _logger.LogInformation($"[{correlationId}] Generated alimentatie tables");
            return elements;
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateGeneralAlimentatieTable(AlimentatieData alimentatie)
        {
            var columnWidths = new[] { 3500, 2500 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.DarkBlue, columnWidths);

            // Add header row
            var headers = new[] { "Omschrijving", "Bedrag" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.DarkBlue, OpenXmlHelper.Colors.White);
            table.Append(headerRow);

            // Add data rows
            if (alimentatie.NettoBesteedbaarGezinsinkomen.HasValue)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell("Netto besteedbaar gezinsinkomen"));
                row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(alimentatie.NettoBesteedbaarGezinsinkomen)));
                table.Append(row);
            }

            if (alimentatie.KostenKinderen.HasValue)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell("Kosten kinderen"));
                row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(alimentatie.KostenKinderen)));
                table.Append(row);
            }

            if (alimentatie.BijdrageKostenKinderen.HasValue)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell("Bijdrage kosten kinderen"));
                row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(alimentatie.BijdrageKostenKinderen)));
                table.Append(row);
            }

            if (!string.IsNullOrEmpty(alimentatie.BijdrageTemplateOmschrijving))
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell("Bijdrage template"));
                row.Append(OpenXmlHelper.CreateStyledCell(alimentatie.BijdrageTemplateOmschrijving));
                table.Append(row);
            }

            return table;
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateContributionsTable(List<BijdrageKostenKinderenData> bijdragen)
        {
            var columnWidths = new[] { 3500, 2500 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.DarkBlue, columnWidths);

            // Add header row
            var headers = new[] { "Partij", "Eigen aandeel" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.DarkBlue, OpenXmlHelper.Colors.White);
            table.Append(headerRow);

            // Add data rows
            foreach (var bijdrage in bijdragen)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell(bijdrage.PersoonNaam ?? "Onbekend"));
                row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(bijdrage.EigenAandeel)));
                table.Append(row);
            }

            return table;
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateChildAgreementsTable(
            List<FinancieleAfsprakenKinderenData> afspraken,
            List<ChildData> kinderen,
            PersonData? partij1,
            PersonData? partij2)
        {
            var columnWidths = new[] { 2000, 1500, 1500, 1500, 1500 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.DarkBlue, columnWidths);

            // Add header row
            var headers = new[] { "Kind", "Alimentatie", "Hoofdverblijf", "Kinderbijslag", "Zorgkorting %" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.DarkBlue, OpenXmlHelper.Colors.White);
            table.Append(headerRow);

            // Add data rows - iterate through children to maintain order
            foreach (var kind in kinderen)
            {
                var afspraak = afspraken.FirstOrDefault(a => a.KindId == kind.Id);

                if (afspraak != null)
                {
                    var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();

                    row.Append(OpenXmlHelper.CreateStyledCell(kind.VolledigeNaam ?? ""));
                    row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(afspraak.AlimentatieBedrag)));
                    row.Append(OpenXmlHelper.CreateStyledCell(GetPartijNaam(afspraak.Hoofdverblijf, partij1, partij2)));
                    row.Append(OpenXmlHelper.CreateStyledCell(GetKinderbijslagOntvanger(afspraak.KinderbijslagOntvanger, partij1, partij2)));
                    row.Append(OpenXmlHelper.CreateStyledCell(afspraak.ZorgkortingPercentage.HasValue ? $"{afspraak.ZorgkortingPercentage:0.##}%" : ""));

                    table.Append(row);
                }
            }

            return table;
        }

        private string GetPartijNaam(int? partijNummer, PersonData? partij1, PersonData? partij2)
        {
            return partijNummer switch
            {
                1 => partij1?.Roepnaam ?? partij1?.Voornamen ?? "Partij 1",
                2 => partij2?.Roepnaam ?? partij2?.Voornamen ?? "Partij 2",
                _ => ""
            };
        }

        private string GetKinderbijslagOntvanger(int? partijNummer, PersonData? partij1, PersonData? partij2)
        {
            return partijNummer switch
            {
                1 => partij1?.Roepnaam ?? partij1?.Voornamen ?? "Partij 1",
                2 => partij2?.Roepnaam ?? partij2?.Voornamen ?? "Partij 2",
                3 => "Kinderrekening",
                _ => ""
            };
        }
    }
}