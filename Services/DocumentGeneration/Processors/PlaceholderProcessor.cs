using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Simple and robust processor for replacing placeholders in Word documents
    /// </summary>
    public class PlaceholderProcessor : IPlaceholderProcessor
    {
        private readonly ILogger<PlaceholderProcessor> _logger;

        public PlaceholderProcessor(ILogger<PlaceholderProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Builds all placeholder replacements from dossier data
        /// </summary>
        public Dictionary<string, string> BuildReplacements(DossierData data, Dictionary<string, string> grammarRules)
        {
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Add grammar rules
            foreach (var rule in grammarRules)
            {
                replacements[rule.Key] = rule.Value;
            }

            // Add party data
            if (data.Partij1 != null)
            {
                AddPersonReplacements(replacements, "Partij1", data.Partij1);
            }

            if (data.Partij2 != null)
            {
                AddPersonReplacements(replacements, "Partij2", data.Partij2);
            }

            // Add dossier data
            replacements["DossierNummer"] = data.DossierNummer ?? "";
            replacements["DossierDatum"] = DataFormatter.FormatDate(data.AangemaaktOp);
            replacements["HuidigeDatum"] = DataFormatter.FormatDateDutchLong(DateTime.Now);
            replacements["IsAnoniem"] = DataFormatter.ConvertToString(data.IsAnoniem);

            // Add children data
            if (data.Kinderen.Any())
            {
                AddChildrenReplacements(replacements, data.Kinderen);
            }

            // Add ouderschapsplan info if available
            if (data.OuderschapsplanInfo != null)
            {
                AddOuderschapsplanInfoReplacements(replacements, data.OuderschapsplanInfo, data.Partij1, data.Partij2);
            }

            return replacements;
        }

        /// <summary>
        /// Processes the document and replaces all placeholders
        /// </summary>
        public void ProcessDocument(Body body, Dictionary<string, string> replacements, string correlationId)
        {
            var paragraphs = body.Descendants<Paragraph>().ToList();
            _logger.LogInformation($"[{correlationId}] Processing {paragraphs.Count} paragraphs");

            foreach (var paragraph in paragraphs)
            {
                ProcessParagraph(paragraph, replacements);
            }

            // Also process tables
            var tables = body.Descendants<Table>().ToList();
            _logger.LogInformation($"[{correlationId}] Processing {tables.Count} tables");

            foreach (var table in tables)
            {
                foreach (var cell in table.Descendants<TableCell>())
                {
                    foreach (var para in cell.Descendants<Paragraph>())
                    {
                        ProcessParagraph(para, replacements);
                    }
                }
            }
        }

        /// <summary>
        /// Processes headers and footers
        /// </summary>
        public void ProcessHeadersAndFooters(MainDocumentPart mainPart, Dictionary<string, string> replacements, string correlationId)
        {
            // Process headers
            foreach (var headerPart in mainPart.HeaderParts)
            {
                foreach (var paragraph in headerPart.Header.Descendants<Paragraph>())
                {
                    ProcessParagraph(paragraph, replacements);
                }
            }

            // Process footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                foreach (var paragraph in footerPart.Footer.Descendants<Paragraph>())
                {
                    ProcessParagraph(paragraph, replacements);
                }
            }

            _logger.LogInformation($"[{correlationId}] Processed headers and footers");
        }

        #region Private Helper Methods

        /// <summary>
        /// Process a single paragraph and replace placeholders
        /// </summary>
        private void ProcessParagraph(Paragraph paragraph, Dictionary<string, string> replacements)
        {
            var texts = paragraph.Descendants<Text>().ToList();
            if (!texts.Any()) return;

            // Combine all text to handle placeholders that might be split
            var fullText = string.Join("", texts.Select(t => t.Text));

            // Check if this paragraph contains any placeholders
            bool hasPlaceholders = replacements.Keys.Any(key =>
                fullText.Contains($"[[{key}]]") ||
                fullText.Contains($"{{{key}}}") ||
                fullText.Contains($"<<{key}>>") ||
                fullText.Contains($"[{key}]"));

            if (!hasPlaceholders) return;

            // Apply replacements with different placeholder formats
            var newText = fullText;
            foreach (var replacement in replacements)
            {
                newText = newText.Replace($"[[{replacement.Key}]]", replacement.Value);
                newText = newText.Replace($"{{{replacement.Key}}}", replacement.Value);
                newText = newText.Replace($"<<{replacement.Key}>>", replacement.Value);
                newText = newText.Replace($"[{replacement.Key}]", replacement.Value);
            }

            if (newText != fullText)
            {
                // Clear existing text elements (keep first one)
                texts.Skip(1).ToList().ForEach(t => t.Remove());

                // Update the first text element with the new text
                if (texts.Any())
                {
                    texts[0].Text = newText;
                }
            }
        }

        /// <summary>
        /// Add person-related replacements
        /// </summary>
        private void AddPersonReplacements(Dictionary<string, string> replacements, string prefix, PersonData person)
        {
            replacements[$"{prefix}Naam"] = person.VolledigeNaam ?? "";
            replacements[$"{prefix}Voornaam"] = person.Voornamen ?? "";
            replacements[$"{prefix}Roepnaam"] = person.Roepnaam ?? person.Voornamen?.Split(' ').FirstOrDefault() ?? "";
            replacements[$"{prefix}Achternaam"] = person.Achternaam ?? "";
            replacements[$"{prefix}Tussenvoegsel"] = person.Tussenvoegsel ?? "";
            replacements[$"{prefix}Adres"] = person.Adres ?? "";
            replacements[$"{prefix}Postcode"] = person.Postcode ?? "";
            replacements[$"{prefix}Plaats"] = person.Plaats ?? "";
            replacements[$"{prefix}Geboorteplaats"] = person.GeboortePlaats ?? "";
            replacements[$"{prefix}Telefoon"] = person.Telefoon ?? "";
            replacements[$"{prefix}Email"] = person.Email ?? "";
            replacements[$"{prefix}Geboortedatum"] = DataFormatter.FormatDate(person.GeboorteDatum);

            // Combined address
            replacements[$"{prefix}VolledigAdres"] = DataFormatter.FormatAddress(
                person.Adres,
                person.Postcode,
                person.Plaats
            );
        }

        /// <summary>
        /// Add children-related replacements
        /// </summary>
        private void AddChildrenReplacements(Dictionary<string, string> replacements, List<ChildData> kinderen)
        {
            replacements["AantalKinderen"] = kinderen.Count.ToString();

            // Individual children
            for (int i = 0; i < kinderen.Count; i++)
            {
                var child = kinderen[i];
                var prefix = $"Kind{i + 1}";

                replacements[$"{prefix}Naam"] = child.VolledigeNaam ?? "";
                replacements[$"{prefix}Voornaam"] = child.Voornamen ?? "";
                replacements[$"{prefix}Roepnaam"] = child.Roepnaam ?? child.Voornamen?.Split(' ').FirstOrDefault() ?? "";
                replacements[$"{prefix}Achternaam"] = child.Achternaam ?? "";
                replacements[$"{prefix}Geboortedatum"] = DataFormatter.FormatDate(child.GeboorteDatum);
                replacements[$"{prefix}Leeftijd"] = child.Leeftijd?.ToString() ?? "";
                replacements[$"{prefix}Geslacht"] = child.Geslacht ?? "";
            }

            // Lists with proper Dutch grammar
            var voornamenList = kinderen.Select(k => k.Voornamen ?? k.VolledigeNaam).ToList();
            var roepnamenList = kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList();
            var volledigeNamenList = kinderen.Select(k => k.VolledigeNaam).ToList();

            replacements["KinderenNamen"] = DutchLanguageHelper.FormatList(voornamenList);
            replacements["KinderenRoepnamen"] = DutchLanguageHelper.FormatList(roepnamenList);
            replacements["KinderenVolledigeNamen"] = DutchLanguageHelper.FormatList(volledigeNamenList);

            // Minor children (under 18)
            var minderjarigeKinderen = kinderen.Where(k => k.Leeftijd.HasValue && k.Leeftijd.Value < 18).ToList();
            var roepnamenMinderjaarigenList = minderjarigeKinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList();

            replacements["AantalMinderjarigeKinderen"] = minderjarigeKinderen.Count.ToString();
            replacements["RoepnamenMinderjarigeKinderen"] = DutchLanguageHelper.FormatList(roepnamenMinderjaarigenList);
        }

        /// <summary>
        /// Add ouderschapsplan info replacements
        /// </summary>
        private void AddOuderschapsplanInfoReplacements(
            Dictionary<string, string> replacements,
            OuderschapsplanInfoData info,
            PersonData? partij1,
            PersonData? partij2)
        {
            replacements["SoortRelatie"] = info.SoortRelatie ?? "";
            replacements["SoortRelatieVerbreking"] = info.SoortRelatieVerbreking ?? "";
            replacements["BetrokkenheidKind"] = info.BetrokkenheidKind ?? "";
            replacements["Kiesplan"] = info.Kiesplan ?? "";

            // Party choices - use display names
            replacements["GezagPartij"] = GetPartijNaam(info.GezagPartij, partij1, partij2);
            replacements["WaOpNaamVan"] = GetPartijNaam(info.WaOpNaamVanPartij, partij1, partij2);
            replacements["ZorgverzekeringOpNaamVan"] = GetPartijNaam(info.ZorgverzekeringOpNaamVanPartij, partij1, partij2);
            replacements["KinderbijslagOntvanger"] = GetKinderbijslagOntvanger(info.KinderbijslagPartij, partij1, partij2);

            replacements["KeuzeDevices"] = info.KeuzeDevices ?? "";
            replacements["Hoofdverblijf"] = info.Hoofdverblijf ?? "";
            replacements["Zorgverdeling"] = info.Zorgverdeling ?? "";
            replacements["OpvangKinderen"] = info.OpvangKinderen ?? "";
            replacements["BankrekeningnummersKind"] = info.BankrekeningnummersOpNaamVanKind ?? "";
            replacements["ParentingCoordinator"] = info.ParentingCoordinator ?? "";
        }

        /// <summary>
        /// Get party name based on party number
        /// </summary>
        private string GetPartijNaam(int? partijNummer, PersonData? partij1, PersonData? partij2)
        {
            return partijNummer switch
            {
                1 => partij1?.Roepnaam ?? partij1?.Voornamen ?? "Partij 1",
                2 => partij2?.Roepnaam ?? partij2?.Voornamen ?? "Partij 2",
                _ => ""
            };
        }

        /// <summary>
        /// Get kinderbijslag recipient
        /// </summary>
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

        #endregion
    }
}