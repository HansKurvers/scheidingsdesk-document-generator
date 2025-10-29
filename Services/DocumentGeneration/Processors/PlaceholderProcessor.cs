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
                AddPersonReplacements(replacements, "Partij1", data.Partij1, data.IsAnoniem);
            }

            if (data.Partij2 != null)
            {
                AddPersonReplacements(replacements, "Partij2", data.Partij2, data.IsAnoniem);
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
                AddOuderschapsplanInfoReplacements(replacements, data.OuderschapsplanInfo, data.Partij1, data.Partij2, data.Kinderen);
            }

            // Add alimentatie data (always add placeholders, even if empty)
            AddAlimentatieReplacements(replacements, data.Alimentatie, data.Partij1, data.Partij2, data.Kinderen);

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
                // Log which placeholders were replaced for debugging
                var replacedKeys = replacements.Keys.Where(key =>
                    fullText.Contains($"[[{key}]]") || fullText.Contains($"{{{key}}}") ||
                    fullText.Contains($"<<{key}>>") || fullText.Contains($"[{key}]")).ToList();

                if (replacedKeys.Any(k => k.Contains("Alimentatie") || k.Contains("Eigen") || k.Contains("Kosten") || k.Contains("Gezins")))
                {
                    _logger.LogInformation("Replaced alimentatie placeholders: {Keys}", string.Join(", ", replacedKeys));
                    _logger.LogDebug("Original text: {Original}", fullText.Substring(0, Math.Min(200, fullText.Length)));
                    _logger.LogDebug("New text: {New}", newText.Substring(0, Math.Min(200, newText.Length)));
                }

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
        private void AddPersonReplacements(Dictionary<string, string> replacements, string prefix, PersonData person, bool? isAnoniem = null)
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

            // Full name with middle name (tussenvoegsel): voornamen + tussenvoegsel + achternaam
            replacements[$"{prefix}VolledigeNaamMetTussenvoegsel"] = GetVolledigeNaamMetTussenvoegsel(person);

            // Full last name with middle name (tussenvoegsel): tussenvoegsel + achternaam
            replacements[$"{prefix}VolledigeAchternaam"] = GetVolledigeAchternaam(person);

            // Benaming placeholder (contextual party designation)
            replacements[$"{prefix}Benaming"] = GetPartijBenaming(person, isAnoniem);
        }

        /// <summary>
        /// Gets the appropriate designation for a party based on anonymity and gender
        /// </summary>
        private static string GetPartijBenaming(PersonData? person, bool? isAnoniem)
        {
            if (person == null) return "";

            // If anonymous, use parental role-based designation
            if (isAnoniem == true)
            {
                var geslacht = person.Geslacht?.Trim().ToLowerInvariant();
                return geslacht switch
                {
                    "m" or "man" => "de vader",
                    "v" or "vrouw" => "de moeder",
                    _ => "de persoon" // Fallback for unknown gender
                };
            }

            // If not anonymous, use roepnaam (or first name as fallback)
            return person.Naam; // Uses existing property that handles roepnaam fallback
        }

        /// <summary>
        /// Gets the full name with middle name (tussenvoegsel): voornamen + tussenvoegsel + achternaam
        /// Example: "Jan Peter de Vries"
        /// </summary>
        private static string GetVolledigeNaamMetTussenvoegsel(PersonData? person)
        {
            if (person == null) return "";

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(person.Voornamen))
                parts.Add(person.Voornamen.Trim());

            if (!string.IsNullOrWhiteSpace(person.Tussenvoegsel))
                parts.Add(person.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(person.Achternaam))
                parts.Add(person.Achternaam.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Gets the full last name with middle name (tussenvoegsel): tussenvoegsel + achternaam
        /// Example: "de Vries"
        /// </summary>
        private static string GetVolledigeAchternaam(PersonData? person)
        {
            if (person == null) return "";

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(person.Tussenvoegsel))
                parts.Add(person.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(person.Achternaam))
                parts.Add(person.Achternaam.Trim());

            return string.Join(" ", parts);
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
                replacements[$"{prefix}Geboorteplaats"] = child.GeboortePlaats ?? "";
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
            PersonData? partij2,
            List<ChildData> kinderen)
        {
            replacements["SoortRelatie"] = info.SoortRelatie ?? "";
            replacements["DatumAanvangRelatie"] = DataFormatter.FormatDate(info.DatumAanvangRelatie);
            replacements["PlaatsRelatie"] = info.PlaatsRelatie ?? "";
            replacements["BetrokkenheidKind"] = info.BetrokkenheidKind ?? "";
            replacements["Kiesplan"] = info.Kiesplan ?? "";

            // Derived placeholders: Map SoortRelatie to the appropriate terms
            replacements["SoortRelatieVoorwaarden"] = GetRelatieVoorwaarden(info.SoortRelatie);
            replacements["SoortRelatieVerbreking"] = GetRelatieVerbreking(info.SoortRelatie);
            replacements["RelatieAanvangZin"] = GetRelatieAanvangZin(info.SoortRelatie, info.DatumAanvangRelatie, info.PlaatsRelatie);
            replacements["OuderschapsplanDoelZin"] = GetOuderschapsplanDoelZin(info.SoortRelatie, kinderen.Count);

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
        /// Get the appropriate legal agreement term based on relationship type
        /// </summary>
        private string GetRelatieVoorwaarden(string? soortRelatie)
        {
            if (string.IsNullOrEmpty(soortRelatie))
                return "";

            return soortRelatie.ToLowerInvariant() switch
            {
                "gehuwd" => "huwelijkse voorwaarden",
                "geregistreerd_partnerschap" => "partnerschapsvoorwaarden",
                "samenwonend" => "samenlevingsovereenkomst",
                _ => "overeenkomst"
            };
        }

        /// <summary>
        /// Get the appropriate relationship termination term based on relationship type
        /// </summary>
        private string GetRelatieVerbreking(string? soortRelatie)
        {
            if (string.IsNullOrEmpty(soortRelatie))
                return "";

            return soortRelatie.ToLowerInvariant() switch
            {
                "gehuwd" => "echtscheiding",
                "geregistreerd_partnerschap" => "ontbinding van het geregistreerd partnerschap",
                "samenwonend" => "beëindiging van de samenleving",
                _ => ""
            };
        }

        /// <summary>
        /// Get the complete sentence describing the start of the relationship based on relationship type
        /// </summary>
        private string GetRelatieAanvangZin(string? soortRelatie, DateTime? datumAanvangRelatie, string? plaatsRelatie)
        {
            if (string.IsNullOrEmpty(soortRelatie))
                return "";

            var datum = DataFormatter.FormatDate(datumAanvangRelatie);
            var plaats = !string.IsNullOrEmpty(plaatsRelatie) ? $" te {plaatsRelatie}" : "";

            return soortRelatie.ToLowerInvariant() switch
            {
                "gehuwd" => $"Wij zijn op {datum}{plaats} met elkaar gehuwd.",
                "geregistreerd_partnerschap" => $"Wij zijn op {datum}{plaats} met elkaar een geregistreerd partnerschap aangegaan.",
                "samenwonend" => "Wij hebben een affectieve relatie gehad.",
                "lat_relatie" or "lat-relatie" => "Wij hebben een affectieve relatie gehad.",
                "ex_partners" or "ex-partners" => "Wij hebben een affectieve relatie gehad.",
                "anders" => "Wij hebben een affectieve relatie gehad.",
                _ => "Wij hebben een affectieve relatie gehad."
            };
        }

        /// <summary>
        /// Get the ouderschapsplan purpose sentence based on relationship type and number of children
        /// </summary>
        private string GetOuderschapsplanDoelZin(string? soortRelatie, int aantalKinderen)
        {
            if (string.IsNullOrEmpty(soortRelatie))
                return "";

            var kindTekst = aantalKinderen == 1 ? "ons kind" : "onze kinderen";

            var redenTekst = soortRelatie.ToLowerInvariant() switch
            {
                "gehuwd" => " omdat we gaan scheiden",
                "geregistreerd_partnerschap" => " omdat we ons geregistreerd partnerschap willen laten ontbinden",
                "samenwonend" => " omdat we onze samenleving willen beëindigen",
                _ => ""
            };

            return $"In dit ouderschapsplan hebben we afspraken gemaakt over {kindTekst}{redenTekst}.";
        }

        /// <summary>
        /// Formats a list of kostensoorten as a bulleted list
        /// </summary>
        private string FormatKostensoortenList(List<string> kostensoorten)
        {
            if (kostensoorten == null || !kostensoorten.Any())
                return "";

            return string.Join("\n", kostensoorten.Select(k => $"- {k}"));
        }

        /// <summary>
        /// Add alimentatie (alimony) related replacements
        /// </summary>
        private void AddAlimentatieReplacements(
            Dictionary<string, string> replacements,
            AlimentatieData? alimentatie,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen)
        {
            // Initialize all placeholders with empty values first
            replacements["NettoBesteedbaarGezinsinkomen"] = "";
            replacements["KostenKinderen"] = "";
            replacements["BijdrageKostenKinderen"] = "";
            replacements["BijdrageTemplateOmschrijving"] = "";
            replacements["Partij1EigenAandeel"] = "";
            replacements["Partij2EigenAandeel"] = "";
            replacements["KinderenAlimentatie"] = "";

            // Initialize new kinderrekening placeholders
            replacements["StortingOuder1Kinderrekening"] = "";
            replacements["StortingOuder2Kinderrekening"] = "";
            replacements["KinderrekeningKostensoorten"] = "";
            replacements["KinderrekeningMaximumOpname"] = "";
            replacements["KinderrekeningMaximumOpnameBedrag"] = "";
            replacements["KinderbijslagStortenOpKinderrekening"] = "";
            replacements["KindgebondenBudgetStortenOpKinderrekening"] = "";
            replacements["BedragenAlleKinderenGelijk"] = "";
            replacements["AlimentatiebedragPerKind"] = "";
            replacements["Alimentatiegerechtigde"] = "";
            replacements["IsKinderrekeningBetaalwijze"] = "";
            replacements["IsAlimentatieplichtBetaalwijze"] = "";

            // If no alimentatie data, return early
            if (alimentatie == null)
            {
                _logger.LogDebug("No alimentatie data available, placeholders set to empty strings");
                return;
            }

            // Basic alimentatie data
            replacements["NettoBesteedbaarGezinsinkomen"] = DataFormatter.FormatCurrency(alimentatie.NettoBesteedbaarGezinsinkomen);
            replacements["KostenKinderen"] = DataFormatter.FormatCurrency(alimentatie.KostenKinderen);
            replacements["BijdrageKostenKinderen"] = DataFormatter.FormatCurrency(alimentatie.BijdrageKostenKinderen);
            replacements["BijdrageTemplateOmschrijving"] = alimentatie.BijdrageTemplateOmschrijving ?? "";

            // Kinderrekening velden
            replacements["StortingOuder1Kinderrekening"] = DataFormatter.FormatCurrency(alimentatie.StortingOuder1Kinderrekening);
            replacements["StortingOuder2Kinderrekening"] = DataFormatter.FormatCurrency(alimentatie.StortingOuder2Kinderrekening);
            replacements["KinderrekeningKostensoorten"] = FormatKostensoortenList(alimentatie.KinderrekeningKostensoorten);
            replacements["KinderrekeningMaximumOpname"] = DataFormatter.ConvertToString(alimentatie.KinderrekeningMaximumOpname);
            replacements["KinderrekeningMaximumOpnameBedrag"] = DataFormatter.FormatCurrency(alimentatie.KinderrekeningMaximumOpnameBedrag);
            replacements["KinderbijslagStortenOpKinderrekening"] = DataFormatter.ConvertToString(alimentatie.KinderbijslagStortenOpKinderrekening);
            replacements["KindgebondenBudgetStortenOpKinderrekening"] = DataFormatter.ConvertToString(alimentatie.KindgebondenBudgetStortenOpKinderrekening);

            // Alimentatie settings
            replacements["BedragenAlleKinderenGelijk"] = DataFormatter.ConvertToString(alimentatie.BedragenAlleKinderenGelijk);
            replacements["AlimentatiebedragPerKind"] = DataFormatter.FormatCurrency(alimentatie.AlimentatiebedragPerKind);
            replacements["Alimentatiegerechtigde"] = alimentatie.Alimentatiegerechtigde ?? "";

            // Template detection flags
            replacements["IsKinderrekeningBetaalwijze"] = DataFormatter.ConvertToString(alimentatie.IsKinderrekeningBetaalwijze);
            replacements["IsAlimentatieplichtBetaalwijze"] = DataFormatter.ConvertToString(alimentatie.IsAlimentatieplichtBetaalwijze);

            _logger.LogDebug("Added alimentatie basic data: Gezinsinkomen={Gezinsinkomen}, KostenKinderen={KostenKinderen}, IsKinderrekening={IsKinderrekening}",
                replacements["NettoBesteedbaarGezinsinkomen"],
                replacements["KostenKinderen"],
                replacements["IsKinderrekeningBetaalwijze"]);

            // Per person contributions (eigen aandeel)
            if (alimentatie.BijdragenKostenKinderen.Any())
            {
                foreach (var bijdrage in alimentatie.BijdragenKostenKinderen)
                {
                    // Match by person ID to determine which party it is
                    if (partij1 != null && bijdrage.PersonenId == partij1.Id)
                    {
                        replacements["Partij1EigenAandeel"] = DataFormatter.FormatCurrency(bijdrage.EigenAandeel);
                        _logger.LogDebug("Set Partij1EigenAandeel to {Amount}", replacements["Partij1EigenAandeel"]);
                    }
                    else if (partij2 != null && bijdrage.PersonenId == partij2.Id)
                    {
                        replacements["Partij2EigenAandeel"] = DataFormatter.FormatCurrency(bijdrage.EigenAandeel);
                        _logger.LogDebug("Set Partij2EigenAandeel to {Amount}", replacements["Partij2EigenAandeel"]);
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
                        if (!string.IsNullOrEmpty(afspraak.Hoofdverblijf))
                            lines.Add($"  - Hoofdverblijf: {afspraak.Hoofdverblijf}");

                        // Kinderbijslag ontvanger
                        if (!string.IsNullOrEmpty(afspraak.KinderbijslagOntvanger))
                            lines.Add($"  - Kinderbijslag: {afspraak.KinderbijslagOntvanger}");

                        // Zorgkorting percentage
                        if (afspraak.ZorgkortingPercentage.HasValue)
                            lines.Add($"  - Zorgkorting: {afspraak.ZorgkortingPercentage:0.##}%");

                        // Inschrijving
                        if (!string.IsNullOrEmpty(afspraak.Inschrijving))
                            lines.Add($"  - Inschrijving bij: {afspraak.Inschrijving}");

                        // Kindgebonden budget
                        if (!string.IsNullOrEmpty(afspraak.KindgebondenBudget))
                            lines.Add($"  - Kindgebonden budget: {afspraak.KindgebondenBudget}");

                        kinderenAlimentatieList.Add(string.Join("\n", lines));
                    }
                }

                replacements["KinderenAlimentatie"] = string.Join("\n\n", kinderenAlimentatieList);
                _logger.LogDebug("Added KinderenAlimentatie with {Count} children", kinderenAlimentatieList.Count);
            }
        }

        #endregion
    }
}