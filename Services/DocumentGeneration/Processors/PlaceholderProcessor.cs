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

            // Add alimentatie data if available
            if (data.Alimentatie != null)
            {
                AddAlimentatieReplacements(replacements, data.Alimentatie, data.Partij1, data.Partij2, data.Kinderen);
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

        /// <summary>
        /// Add alimentatie (alimony) related replacements
        /// </summary>
        private void AddAlimentatieReplacements(
            Dictionary<string, string> replacements,
            AlimentatieData alimentatie,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen)
        {
            // Basic alimentatie data
            replacements["NettoBesteedbaarGezinsinkomen"] = DataFormatter.FormatCurrency(alimentatie.NettoBesteedbaarGezinsinkomen);
            replacements["KostenKinderen"] = DataFormatter.FormatCurrency(alimentatie.KostenKinderen);
            replacements["BijdrageKostenKinderen"] = DataFormatter.FormatCurrency(alimentatie.BijdrageKostenKinderen);
            replacements["BijdrageTemplateOmschrijving"] = alimentatie.BijdrageTemplateOmschrijving ?? "";

            // Per person contributions (eigen aandeel)
            if (alimentatie.BijdragenKostenKinderen.Any())
            {
                foreach (var bijdrage in alimentatie.BijdragenKostenKinderen)
                {
                    // Match by person ID to determine which party it is
                    if (partij1 != null && bijdrage.PersonenId == partij1.Id)
                    {
                        replacements["Partij1EigenAandeel"] = DataFormatter.FormatCurrency(bijdrage.EigenAandeel);
                    }
                    else if (partij2 != null && bijdrage.PersonenId == partij2.Id)
                    {
                        replacements["Partij2EigenAandeel"] = DataFormatter.FormatCurrency(bijdrage.EigenAandeel);
                    }
                }
            }

            // Build formatted list of all children's financial agreements
            if (alimentatie.FinancieleAfsprakenKinderen.Any() && kinderen.Any())
            {
                var kinderenAlimentatieList = new List<string>();

                foreach (var kind in kinderen)
                {
                    var afspraak = alimentatie.FinancieleAfsprakenKinderen.FirstOrDefault(f => f.KindId == kind.Id);

                    if (afspraak != null)
                    {
                        var lines = new List<string>();

                        // Kind naam
                        lines.Add($"{kind.VolledigeNaam}:");

                        // Alimentatie bedrag
                        if (afspraak.AlimentatieBedrag.HasValue)
                            lines.Add($"  - Alimentatie: {DataFormatter.FormatCurrency(afspraak.AlimentatieBedrag)}");

                        // Hoofdverblijf
                        var hoofdverblijf = GetPartijNaam(afspraak.Hoofdverblijf, partij1, partij2);
                        if (!string.IsNullOrEmpty(hoofdverblijf))
                            lines.Add($"  - Hoofdverblijf: {hoofdverblijf}");

                        // Kinderbijslag ontvanger
                        var kinderbijslag = GetKinderbijslagOntvanger(afspraak.KinderbijslagOntvanger, partij1, partij2);
                        if (!string.IsNullOrEmpty(kinderbijslag))
                            lines.Add($"  - Kinderbijslag: {kinderbijslag}");

                        // Zorgkorting percentage
                        if (afspraak.ZorgkortingPercentage.HasValue)
                            lines.Add($"  - Zorgkorting: {afspraak.ZorgkortingPercentage:0.##}%");

                        // Inschrijving
                        var inschrijving = GetPartijNaam(afspraak.Inschrijving, partij1, partij2);
                        if (!string.IsNullOrEmpty(inschrijving))
                            lines.Add($"  - Inschrijving bij: {inschrijving}");

                        // Kindgebonden budget
                        var kgb = GetKinderbijslagOntvanger(afspraak.KindgebondenBudget, partij1, partij2);
                        if (!string.IsNullOrEmpty(kgb))
                            lines.Add($"  - Kindgebonden budget: {kgb}");

                        kinderenAlimentatieList.Add(string.Join("\n", lines));
                    }
                }

                replacements["KinderenAlimentatie"] = string.Join("\n\n", kinderenAlimentatieList);
            }
            else
            {
                replacements["KinderenAlimentatie"] = "";
            }
        }

        #endregion
    }
}