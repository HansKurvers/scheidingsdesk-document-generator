using DocumentFormat.OpenXml;
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
                AddOuderschapsplanInfoReplacements(replacements, data.OuderschapsplanInfo, data.Partij1, data.Partij2, data.Kinderen, data);
            }
            else
            {
                // Add default values for required placeholders
                replacements["RelatieAanvangZin"] = "Wij hebben een relatie met elkaar gehad.";
                replacements["OuderschapsplanDoelZin"] = $"In dit ouderschapsplan hebben we afspraken gemaakt over {(data.Kinderen.Count == 1 ? "ons kind" : "onze kinderen")}.";

                // Generate default gezag text with actual children names
                string kinderenNamen;
                if (data.Kinderen.Count == 0)
                {
                    kinderenNamen = "de kinderen";
                }
                else if (data.Kinderen.Count == 1)
                {
                    kinderenNamen = data.Kinderen[0].Roepnaam ?? data.Kinderen[0].Voornamen ?? "het kind";
                }
                else
                {
                    kinderenNamen = DutchLanguageHelper.FormatList(data.Kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());
                }
                var defaultGezagText = $"De ouders hebben gezamenlijk gezag over {kinderenNamen}.";
                replacements["GezagZin"] = defaultGezagText;
                replacements["GezagRegeling"] = defaultGezagText;
                
                // Add default woonplaats text
                replacements["WoonplaatsRegeling"] = "Het is nog onduidelijk waar de ouders zullen gaan wonen nadat zij uit elkaar gaan.";
                
                // Add default hoofdverblijf text
                replacements["Hoofdverblijf"] = "";
            }

            // Add alimentatie data (always add placeholders, even if empty)
            AddAlimentatieReplacements(replacements, data.Alimentatie, data.Partij1, data.Partij2, data.Kinderen, data.IsAnoniem);

            // Add hoofdverblijf verdeling (distribution of children's primary residence)
            replacements["HoofdverblijfVerdeling"] = GetHoofdverblijfVerdeling(data.Alimentatie, data.Partij1, data.Partij2, data.Kinderen, data.IsAnoniem);

            // Add inschrijving verdeling (distribution of children's BRP registration)
            replacements["InschrijvingVerdeling"] = GetInschrijvingVerdeling(data.Alimentatie, data.Partij1, data.Partij2, data.Kinderen, data.IsAnoniem);

            // Add communicatie afspraken data (always add placeholders, even if empty)
            AddCommunicatieAfsprakenReplacements(replacements, data.CommunicatieAfspraken, data.Partij1, data.Partij2, data.Kinderen);

            // Add custom placeholders from the placeholder_catalogus
            // These have priority: dossier > gebruiker > systeem > standaard_waarde
            if (data.CustomPlaceholders.Any())
            {
                foreach (var placeholder in data.CustomPlaceholders)
                {
                    // Only add if not already set by a system placeholder
                    if (!replacements.ContainsKey(placeholder.Key))
                    {
                        replacements[placeholder.Key] = placeholder.Value;
                    }
                }
                _logger.LogInformation("Added {Count} custom placeholders", data.CustomPlaceholders.Count);
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
                // Log which placeholders were replaced for debugging
                var replacedKeys = replacements.Keys.Where(key =>
                    fullText.Contains($"[[{key}]]") || fullText.Contains($"{{{key}}}") ||
                    fullText.Contains($"<<{key}>>") || fullText.Contains($"[{key}]")).ToList();

                // Log replaced placeholders for debugging (only alimentatie-related)
                if (replacedKeys.Any(k => k.Contains("Alimentatie") || k.Contains("Eigen") || k.Contains("Kosten") || k.Contains("Gezins")))
                {
                    _logger.LogInformation("Replaced alimentatie placeholders: {Keys}", string.Join(", ", replacedKeys));
                }

                // Check if the new text contains line breaks
                if (newText.Contains("\n"))
                {
                    // Handle line breaks by creating proper Word line breaks
                    ReplaceTextWithLineBreaks(paragraph, texts, newText);
                }
                else
                {
                    // Simple replacement without line breaks
                    texts.Skip(1).ToList().ForEach(t => t.Remove());
                    if (texts.Any())
                    {
                        texts[0].Text = newText;
                    }
                }
            }
        }

        /// <summary>
        /// Replace text in paragraph with proper Word line breaks for \n characters
        /// </summary>
        private void ReplaceTextWithLineBreaks(Paragraph paragraph, List<Text> originalTexts, string newText)
        {
            // Get the parent Run of the first text element to copy its properties
            var firstText = originalTexts.FirstOrDefault();
            var parentRun = firstText?.Parent as Run;
            var runProperties = parentRun?.RunProperties?.CloneNode(true) as RunProperties;

            // Remove all original text elements
            foreach (var text in originalTexts)
            {
                text.Remove();
            }

            // Split the text by newlines and create new elements
            var lines = newText.Split(new[] { "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                // Create a new Run for each line
                var newRun = new Run();
                if (runProperties != null)
                {
                    newRun.RunProperties = runProperties.CloneNode(true) as RunProperties;
                }

                // Add the text
                var textElement = new Text(lines[i]);
                if (lines[i].StartsWith(" ") || lines[i].EndsWith(" "))
                {
                    textElement.Space = SpaceProcessingModeValues.Preserve;
                }
                newRun.Append(textElement);

                // Add a line break after each line except the last
                if (i < lines.Length - 1)
                {
                    newRun.Append(new Break());
                }

                // Insert the run into the paragraph
                if (parentRun != null)
                {
                    parentRun.Parent?.InsertBefore(newRun, parentRun);
                }
                else
                {
                    paragraph.Append(newRun);
                }
            }

            // Remove the original parent run if it's empty
            if (parentRun != null && !parentRun.Descendants<Text>().Any())
            {
                parentRun.Remove();
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

            // Voorletters + tussenvoegsel + achternaam
            replacements[$"{prefix}VoorlettersAchternaam"] = GetVoorlettersAchternaam(person);

            // Nationaliteit (basisvorm en bijvoeglijke vorm)
            replacements[$"{prefix}Nationaliteit1"] = person.Nationaliteit1 ?? "";
            replacements[$"{prefix}Nationaliteit2"] = person.Nationaliteit2 ?? "";
            replacements[$"{prefix}Nationaliteit1Bijvoeglijk"] = DutchLanguageHelper.ToNationalityAdjective(person.Nationaliteit1);
            replacements[$"{prefix}Nationaliteit2Bijvoeglijk"] = DutchLanguageHelper.ToNationalityAdjective(person.Nationaliteit2);
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
                replacements[$"{prefix}Tussenvoegsel"] = child.Tussenvoegsel ?? "";
                replacements[$"{prefix}RoepnaamAchternaam"] = GetKindRoepnaamAchternaam(child);
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
            List<ChildData> kinderen,
            DossierData data)
        {
            replacements["SoortRelatie"] = info.SoortRelatie ?? "";
            replacements["DatumAanvangRelatie"] = DataFormatter.FormatDate(info.DatumAanvangRelatie);
            replacements["PlaatsRelatie"] = info.PlaatsRelatie ?? "";
            replacements["BetrokkenheidKind"] = info.BetrokkenheidKind ?? "";
            replacements["Kiesplan"] = info.Kiesplan ?? "";
            replacements["KiesplanZin"] = GetKiesplanZin(info.Kiesplan, kinderen);

            // Derived placeholders: Map SoortRelatie to the appropriate terms
            replacements["SoortRelatieVoorwaarden"] = GetRelatieVoorwaarden(info.SoortRelatie);
            replacements["SoortRelatieVerbreking"] = GetRelatieVerbreking(info.SoortRelatie);

            // Generate relationship and parenting plan sentences dynamically
            replacements["RelatieAanvangZin"] = GetRelatieAanvangZin(info.SoortRelatie, info.DatumAanvangRelatie, info.PlaatsRelatie);
            replacements["OuderschapsplanDoelZin"] = GetOuderschapsplanDoelZin(info.SoortRelatie, kinderen.Count);

            // Generate gezag (parental authority) sentence dynamically
            replacements["GezagRegeling"] = GetGezagRegeling(info.GezagPartij, info.GezagTermijnWeken, partij1, partij2, kinderen);
            replacements["GezagZin"] = replacements["GezagRegeling"];  // Alias for backward compatibility

            replacements["GezagPartij"] = info.GezagPartij?.ToString() ?? "";
            replacements["GezagTermijnWeken"] = info.GezagTermijnWeken?.ToString() ?? "";

            // Woonplaats (residence) placeholders
            replacements["WoonplaatsRegeling"] = GetWoonplaatsRegeling(info.WoonplaatsOptie, info.WoonplaatsPartij1, info.WoonplaatsPartij2, partij1, partij2, info.SoortRelatie);
            replacements["WoonplaatsOptie"] = info.WoonplaatsOptie?.ToString() ?? "";
            replacements["WoonplaatsPartij1"] = info.WoonplaatsPartij1 ?? "";
            replacements["WoonplaatsPartij2"] = info.WoonplaatsPartij2 ?? "";
            replacements["HuidigeWoonplaatsPartij1"] = partij1?.Plaats ?? "";
            replacements["HuidigeWoonplaatsPartij2"] = partij2?.Plaats ?? "";

            // Party choices - use display names
            replacements["WaOpNaamVan"] = GetPartijNaam(info.WaOpNaamVanPartij, partij1, partij2);
            replacements["ZorgverzekeringOpNaamVan"] = GetPartijNaam(info.ZorgverzekeringOpNaamVanPartij, partij1, partij2);
            replacements["KinderbijslagOntvanger"] = GetKinderbijslagOntvanger(info.KinderbijslagPartij, partij1, partij2);

            replacements["KeuzeDevices"] = info.KeuzeDevices ?? "";
            replacements["Hoofdverblijf"] = GetHoofdverblijfText(info.Hoofdverblijf, partij1, partij2, kinderen, data.IsAnoniem);
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
            {
                return "Wij hebben een relatie met elkaar gehad.";
            }

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
            var kindTekst = aantalKinderen == 1 ? "ons kind" : "onze kinderen";
            
            if (string.IsNullOrEmpty(soortRelatie))
            {
                return $"In dit ouderschapsplan hebben we afspraken gemaakt over {kindTekst}.";
            }

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
        /// Get the gezag (parental authority) arrangement sentence based on gezagPartij value
        /// </summary>
        private string GetGezagRegeling(
            int? gezagPartij,
            int? gezagTermijnWeken,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen)
        {
            if (kinderen.Count == 0)
                return "";

            // Default to shared custody if gezag_partij is not set
            if (!gezagPartij.HasValue)
            {
                var defaultKinderenTekst = kinderen.Count == 1
                    ? kinderen[0].Roepnaam ?? kinderen[0].Voornamen ?? "het kind"
                    : DutchLanguageHelper.FormatList(kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());
                return $"De ouders hebben gezamenlijk gezag over {defaultKinderenTekst}.";
            }

            var partij1Naam = partij1?.Roepnaam ?? partij1?.Voornamen ?? "Partij 1";
            var partij2Naam = partij2?.Roepnaam ?? partij2?.Voornamen ?? "Partij 2";

            // Determine if singular or plural for children
            var kinderenTekst = kinderen.Count == 1
                ? kinderen[0].Roepnaam ?? kinderen[0].Voornamen ?? "het kind"
                : DutchLanguageHelper.FormatList(kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());

            var weken = gezagTermijnWeken ?? 2;

            return gezagPartij.Value switch
            {
                1 => $"{partij1Naam} en {partij2Naam} hebben samen het ouderlijk gezag over {kinderenTekst}. Na de scheiding blijft dit zo.",
                2 => $"{partij1Naam} heeft alleen het ouderlijk gezag over {kinderenTekst}. Dit blijft zo.",
                3 => $"{partij2Naam} heeft alleen het ouderlijk gezag over {kinderenTekst}. Dit blijft zo.",
                4 => $"{partij1Naam} heeft alleen het ouderlijk gezag over {kinderenTekst}. Partijen spreken af dat zij binnen {weken} weken na ondertekening van dit ouderschapsplan gezamenlijk gezag zullen regelen.",
                5 => $"{partij2Naam} heeft alleen het ouderlijk gezag over {kinderenTekst}. Partijen spreken af dat zij binnen {weken} weken na ondertekening van dit ouderschapsplan gezamenlijk gezag zullen regelen.",
                _ => ""
            };
        }

        /// <summary>
        /// Get the KIES plan sentence based on the chosen kiesplan option
        /// </summary>
        private string GetKiesplanZin(string? kiesplan, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(kiesplan) || kiesplan == "nee")
                return "";

            if (kinderen.Count == 0)
                return "";

            var kinderenNamen = DutchLanguageHelper.FormatList(
                kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());

            var isEnkelvoud = kinderen.Count == 1;

            // Werkwoord vormen
            var isZijn = isEnkelvoud ? "is" : "zijn";
            var heeftHebben = isEnkelvoud ? "heeft" : "hebben";

            // Bezittelijk voornaamwoord op basis van geslacht (bij 1 kind) of "hun" (bij meerdere)
            var hunZijnHaar = isEnkelvoud
                ? GetBezittelijkVoornaamwoord(kinderen[0].Geslacht)
                : "hun";

            // KIES Kindplan enkelvoud/meervoud
            var kindplanTekst = isEnkelvoud
                ? "Het door ons ondertekende KIES Kindplan is"
                : "De door ons ondertekende KIES Kindplannen zijn";

            return kiesplan.ToLowerInvariant() switch
            {
                "kindplan" => $"Bij het maken van de afspraken in dit ouderschapsplan hebben we {kinderenNamen} gevraagd een KIES Kindplan te maken dat door ons is ondertekend, zodat wij rekening kunnen houden met {hunZijnHaar} wensen. Het KIES Kindplan van {kinderenNamen} is opgenomen als bijlage van dit ouderschapsplan.",

                "kies_professional" => $"Bij het maken van de afspraken in dit ouderschapsplan {isZijn} {kinderenNamen} ondersteund door een KIES professional met een KIES kindgesprek om {hunZijnHaar} vragen te kunnen stellen en behoeftes en wensen aan te geven, zodat wij hiermee rekening kunnen houden. {kindplanTekst} daarbij gemaakt en bijlage van dit ouderschapsplan.",

                "kindbehartiger" => $"Bij het maken van de afspraken in dit ouderschapsplan {heeftHebben} {kinderenNamen} hulp gekregen van een Kindbehartiger om {hunZijnHaar} wensen in kaart te brengen zodat wij hiermee rekening kunnen houden.",

                _ => ""
            };
        }

        /// <summary>
        /// Get bezittelijk voornaamwoord based on gender
        /// </summary>
        private string GetBezittelijkVoornaamwoord(string? geslacht)
        {
            return geslacht?.ToLowerInvariant() switch
            {
                "man" or "m" or "jongen" => "zijn",
                "vrouw" or "v" or "meisje" => "haar",
                _ => "zijn/haar"  // fallback als geslacht onbekend is
            };
        }

        /// <summary>
        /// Get the betrokkenheid kind sentence based on the chosen option
        /// </summary>
        private string GetBetrokkenheidKindZin(string? betrokkenheid, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(betrokkenheid))
                return "";

            if (kinderen.Count == 0)
                return "";

            var kinderenNamen = DutchLanguageHelper.FormatList(
                kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());

            var isEnkelvoud = kinderen.Count == 1;

            // Werkwoord vormen
            var isZijn = isEnkelvoud ? "is" : "zijn";

            // Bezittelijk voornaamwoord op basis van geslacht (bij 1 kind) of "hun" (bij meerdere)
            var hunZijnHaar = isEnkelvoud
                ? GetBezittelijkVoornaamwoord(kinderen[0].Geslacht)
                : "hun";

            return betrokkenheid.ToLowerInvariant() switch
            {
                "samen" => $"Wij hebben samen met {kinderenNamen} gesproken zodat wij rekening kunnen houden met {hunZijnHaar} wensen.",

                "los_van_elkaar" => $"Wij hebben los van elkaar met {kinderenNamen} gesproken zodat wij rekening kunnen houden met {hunZijnHaar} wensen.",

                "jonge_leeftijd" => $"{kinderenNamen} {isZijn} gezien de jonge leeftijd niet betrokken bij het opstellen van het ouderschapsplan.",

                "niet_betrokken" => $"{kinderenNamen} {isZijn} niet betrokken bij het opstellen van het ouderschapsplan.",

                _ => ""
            };
        }

        /// <summary>
        /// Get the Villa Pinedo sentence based on the chosen option
        /// </summary>
        private string GetVillaPinedoZin(string? villaPinedo, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(villaPinedo))
                return "";

            if (kinderen.Count == 0)
                return "";

            var kinderenNamen = DutchLanguageHelper.FormatList(
                kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());

            var isEnkelvoud = kinderen.Count == 1;

            // Bezittelijk voornaamwoord op basis van geslacht (bij 1 kind) of "hun" (bij meerdere)
            var hunZijnHaar = isEnkelvoud
                ? GetBezittelijkVoornaamwoord(kinderen[0].Geslacht)
                : "hun";

            // "hij/zij" voor enkelvoud, "zij" voor meervoud
            var zijHijZij = isEnkelvoud
                ? (kinderen[0].Geslacht?.ToLowerInvariant() switch
                {
                    "man" or "m" or "jongen" => "hij",
                    "vrouw" or "v" or "meisje" => "zij",
                    _ => "hij/zij"
                })
                : "zij";

            // "hem/haar" voor enkelvoud, "hen" voor meervoud (lijdend voorwerp)
            var henHemHaar = isEnkelvoud
                ? (kinderen[0].Geslacht?.ToLowerInvariant() switch
                {
                    "man" or "m" or "jongen" => "hem",
                    "vrouw" or "v" or "meisje" => "haar",
                    _ => "hem/haar"
                })
                : "hen";

            return villaPinedo.ToLowerInvariant() switch
            {
                "ja" => $"Wij hebben {kinderenNamen} op de hoogte gebracht van Villa Pinedo, waar {zijHijZij} terecht kan met {hunZijnHaar} vragen, voor het delen van ervaringen, het krijgen van tips en steun om met de scheiding om te gaan.",

                "nee" => $"Wij hebben {kinderenNamen} nog niet op de hoogte gebracht van Villa Pinedo, waar {zijHijZij} terecht kan met {hunZijnHaar} vragen, voor het delen van ervaringen, het krijgen van tips en steun om met de scheiding om te gaan. Als daar aanleiding toe is zullen wij {henHemHaar} daar zeker op attenderen.",

                _ => ""
            };
        }

        /// <summary>
        /// Get the woonplaats (residence) arrangement sentence based on woonplaatsOptie value
        /// </summary>
        private string GetWoonplaatsRegeling(
            int? woonplaatsOptie,
            string? woonplaatsPartij1,
            string? woonplaatsPartij2,
            PersonData? partij1,
            PersonData? partij2,
            string? soortRelatie = null)
        {
            var partij1Naam = partij1?.Roepnaam ?? partij1?.Voornamen ?? "Partij 1";
            var partij2Naam = partij2?.Roepnaam ?? partij2?.Voornamen ?? "Partij 2";
            var huidigeWoonplaatsPartij1 = partij1?.Plaats ?? "onbekend";
            var huidigeWoonplaatsPartij2 = partij2?.Plaats ?? "onbekend";
            
            // Get relationship termination text for option 5 and fallback
            var relatieVerbreking = GetRelatieVerbreking(soortRelatie);

            // Default to option 5 if not set
            if (!woonplaatsOptie.HasValue)
            {
                return $"Het is nog onduidelijk waar de ouders zullen gaan wonen nadat zij {relatieVerbreking}.";
            }

            return woonplaatsOptie.Value switch
            {
                1 => $"De woonplaatsen van partijen blijven hetzelfde. {partij1Naam} blijft wonen in {huidigeWoonplaatsPartij1} en {partij2Naam} blijft wonen in {huidigeWoonplaatsPartij2}.",
                2 => $"{partij1Naam} gaat verhuizen naar {woonplaatsPartij1 ?? "een nieuwe woonplaats"}. {partij2Naam} blijft wonen in {huidigeWoonplaatsPartij2}.",
                3 => $"{partij1Naam} blijft wonen in {huidigeWoonplaatsPartij1}. {partij2Naam} gaat verhuizen naar {woonplaatsPartij2 ?? "een nieuwe woonplaats"}.",
                4 => $"{partij1Naam} gaat verhuizen naar {woonplaatsPartij1 ?? "een nieuwe woonplaats"} en {partij2Naam} gaat verhuizen naar {woonplaatsPartij2 ?? "een nieuwe woonplaats"}.",
                5 => $"Het is nog onduidelijk waar de ouders zullen gaan wonen nadat zij {relatieVerbreking}.",
                _ => $"Het is nog onduidelijk waar de ouders zullen gaan wonen nadat zij {relatieVerbreking}."
            };
        }

        /// <summary>
        /// Get the omgangsregeling description based on the chosen format (Tekst/Schema/Beiden)
        /// </summary>
        private string GetOmgangsregelingBeschrijving(
            string? omgangTekstOfSchema,
            string? omgangBeschrijving,
            int aantalKinderen)
        {
            var kinderenTekst = aantalKinderen == 1 ? "ons kind" : "onze kinderen";
            var keuze = omgangTekstOfSchema?.Trim().ToLowerInvariant() ?? "";

            return keuze switch
            {
                "tekst" => $"Wij verdelen de zorg en opvoeding van {kinderenTekst} op de volgende manier: {omgangBeschrijving}",
                "beiden" => $"Wij verdelen de zorg en opvoeding van {kinderenTekst} op de volgende manier: {omgangBeschrijving} Daarnaast is er ook een vast schema toegevoegd in de bijlage van het ouderschapsplan.",
                _ => $"Wij verdelen de zorg en opvoeding van {kinderenTekst} volgens het vaste schema van bijlage 1."
            };
        }

        /// <summary>
        /// Get the opvang description based on the chosen option (1 or 2)
        /// </summary>
        private string GetOpvangBeschrijving(string? opvang)
        {
            if (string.IsNullOrEmpty(opvang))
                return "";

            var keuze = opvang.Trim();

            return keuze switch
            {
                "1" => "Wij blijven ieder zelf verantwoordelijk voor de opvang van onze kinderen op de dagen dat ze volgens het schema bij ieder van ons verblijven.",
                "2" => "Als opvang of een afwijking van het schema nodig is, vragen wij altijd eerst aan de andere ouder of die beschikbaar is, voordat wij anderen vragen voor de opvang van onze kinderen.",
                _ => ""
            };
        }

        /// <summary>
        /// Get the informatie uitwisseling description based on the chosen method
        /// </summary>
        private string GetInformatieUitwisselingBeschrijving(string? informatieUitwisseling, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(informatieUitwisseling))
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = informatieUitwisseling.Trim().ToLowerInvariant();

            return keuze switch
            {
                "email" => $"Wij delen de informatie over {kinderenTekst} met elkaar via de e-mail.",
                "telefoon" => $"Wij delen de informatie over {kinderenTekst} met elkaar telefonisch.",
                "app" => $"Wij delen de informatie over {kinderenTekst} met elkaar via een app (zoals WhatsApp).",
                "oudersapp" => $"Wij delen de informatie over {kinderenTekst} met elkaar via een speciale ouders-app.",
                "persoonlijk" => $"Wij delen de informatie over {kinderenTekst} met elkaar in een persoonlijk gesprek.",
                "combinatie" => $"Wij delen de informatie over {kinderenTekst} met elkaar via een combinatie van methoden (e-mail, telefonisch, app en mondeling).",
                _ => ""
            };
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
        /// Get the hoofdverblijf (primary residence) text based on the stored person ID
        /// </summary>
        private string GetHoofdverblijfText(string? hoofdverblijf, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen, bool? isAnoniem)
        {
            if (string.IsNullOrEmpty(hoofdverblijf))
                return "";

            // Try to parse the hoofdverblijf as a person ID
            if (int.TryParse(hoofdverblijf, out int personId))
            {
                // Check if it matches partij1
                if (partij1 != null && partij1.Id == personId)
                {
                    var partij1Benaming = GetPartijBenaming(partij1, isAnoniem);
                    var kinderenTekst = GetKinderenTekst(kinderen);
                    return $"{kinderenTekst} {(kinderen.Count == 1 ? "heeft" : "hebben")} {(kinderen.Count == 1 ? "het" : "hun")} hoofdverblijf bij {partij1Benaming}.";
                }
                // Check if it matches partij2
                else if (partij2 != null && partij2.Id == personId)
                {
                    var partij2Benaming = GetPartijBenaming(partij2, isAnoniem);
                    var kinderenTekst = GetKinderenTekst(kinderen);
                    return $"{kinderenTekst} {(kinderen.Count == 1 ? "heeft" : "hebben")} {(kinderen.Count == 1 ? "het" : "hun")} hoofdverblijf bij {partij2Benaming}.";
                }
            }

            // If not a valid person ID or doesn't match, return the raw value
            return hoofdverblijf;
        }

        /// <summary>
        /// Get appropriate text for children based on count and names
        /// </summary>
        private string GetKinderenTekst(List<ChildData> kinderen)
        {
            if (kinderen.Count == 0)
                return "De kinderen";

            if (kinderen.Count == 1)
            {
                return kinderen[0].Roepnaam ?? kinderen[0].Voornamen ?? "Het kind";
            }

            var roepnamen = kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList();
            return DutchLanguageHelper.FormatList(roepnamen);
        }

        /// <summary>
        /// Add alimentatie (alimony) related replacements
        /// </summary>
        private void AddAlimentatieReplacements(
            Dictionary<string, string> replacements,
            AlimentatieData? alimentatie,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen,
            bool? isAnoniem)
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
            replacements["ZorgkortingPercentageAlleKinderen"] = "";
            replacements["IsKinderrekeningBetaalwijze"] = "";
            replacements["IsAlimentatieplichtBetaalwijze"] = "";

            // Initialize new sync settings placeholders
            replacements["AfsprakenAlleKinderenGelijk"] = "";
            replacements["HoofdverblijfAlleKinderen"] = "";
            replacements["InschrijvingAlleKinderen"] = "";
            replacements["KinderbijslagOntvangerAlleKinderen"] = "";
            replacements["KindgebondenBudgetAlleKinderen"] = "";

            // Initialize betaalwijze beschrijving placeholder
            replacements["BetaalwijzeBeschrijving"] = "";

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
            replacements["ZorgkortingPercentageAlleKinderen"] = alimentatie.ZorgkortingPercentageAlleKinderen.HasValue ? $"{alimentatie.ZorgkortingPercentageAlleKinderen:0.##}%" : "";

            // Template detection flags
            replacements["IsKinderrekeningBetaalwijze"] = DataFormatter.ConvertToString(alimentatie.IsKinderrekeningBetaalwijze);
            replacements["IsAlimentatieplichtBetaalwijze"] = DataFormatter.ConvertToString(alimentatie.IsAlimentatieplichtBetaalwijze);

            // Sync settings for all children
            replacements["AfsprakenAlleKinderenGelijk"] = DataFormatter.ConvertToString(alimentatie.AfsprakenAlleKinderenGelijk);
            replacements["HoofdverblijfAlleKinderen"] = GetPartyName(alimentatie.HoofdverblijfAlleKinderen, partij1, partij2);
            replacements["InschrijvingAlleKinderen"] = GetPartyName(alimentatie.InschrijvingAlleKinderen, partij1, partij2);
            replacements["KinderbijslagOntvangerAlleKinderen"] = GetPartyNameOrKinderrekening(alimentatie.KinderbijslagOntvangerAlleKinderen, partij1, partij2);
            replacements["KindgebondenBudgetAlleKinderen"] = GetPartyNameOrKinderrekening(alimentatie.KindgebondenBudgetAlleKinderen, partij1, partij2);

            // Generate betaalwijze beschrijving (kinderrekening or alimentatie)
            replacements["BetaalwijzeBeschrijving"] = GetBetaalwijzeBeschrijving(alimentatie, partij1, partij2, isAnoniem);

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

        /// <summary>
        /// Gets the roepnaam with tussenvoegsel and achternaam for a child
        /// Example: "Jan de Vries" (when roepnaam is "Jan")
        /// </summary>
        private static string GetKindRoepnaamAchternaam(ChildData? child)
        {
            if (child == null) return "";

            var parts = new List<string>();

            // Use roepnaam, or fall back to first name from voornamen, or fall back to achternaam
            var roepnaam = child.Roepnaam ?? child.Voornamen?.Split(' ').FirstOrDefault() ?? "";
            if (!string.IsNullOrWhiteSpace(roepnaam))
                parts.Add(roepnaam.Trim());

            if (!string.IsNullOrWhiteSpace(child.Tussenvoegsel))
                parts.Add(child.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(child.Achternaam))
                parts.Add(child.Achternaam.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Gets voorletters + tussenvoegsel + achternaam
        /// Example: "J.P. de Vries" (for Jan Peter de Vries)
        /// </summary>
        private static string GetVoorlettersAchternaam(PersonData? person)
        {
            if (person == null) return "";

            var parts = new List<string>();

            // Use voorletters if available, otherwise create from voornamen
            if (!string.IsNullOrWhiteSpace(person.Voorletters))
            {
                parts.Add(person.Voorletters.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(person.Voornamen))
            {
                // Create voorletters from voornamen if not available
                var voorletters = string.Join(".", person.Voornamen
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.FirstOrDefault())
                    .Where(c => c != default(char))) + ".";
                parts.Add(voorletters);
            }

            if (!string.IsNullOrWhiteSpace(person.Tussenvoegsel))
                parts.Add(person.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(person.Achternaam))
                parts.Add(person.Achternaam.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Gets the party name (roepnaam) based on the party identifier
        /// </summary>
        private string GetPartyName(string? partyIdentifier, PersonData? partij1, PersonData? partij2)
        {
            if (string.IsNullOrEmpty(partyIdentifier))
                return "";

            return partyIdentifier.ToLower() switch
            {
                "partij1" => partij1?.Roepnaam ?? "",
                "partij2" => partij2?.Roepnaam ?? "",
                _ => ""
            };
        }

        /// <summary>
        /// Gets the party name (roepnaam) or "Kinderrekening" based on the party identifier
        /// </summary>
        private string GetPartyNameOrKinderrekening(string? partyIdentifier, PersonData? partij1, PersonData? partij2)
        {
            if (string.IsNullOrEmpty(partyIdentifier))
                return "";

            return partyIdentifier.ToLower() switch
            {
                "partij1" => partij1?.Roepnaam ?? "",
                "partij2" => partij2?.Roepnaam ?? "",
                "kinderrekening" => "Kinderrekening",
                _ => ""
            };
        }

        /// <summary>
        /// Generate the betaalwijze beschrijving based on whether it's kinderrekening or alimentatie
        /// </summary>
        private string GetBetaalwijzeBeschrijving(AlimentatieData alimentatie, PersonData? partij1, PersonData? partij2, bool? isAnoniem)
        {
            // Use GetPartijBenaming for proper anonymity handling (returns "de vader"/"de moeder" if anonymous)
            var ouder1Naam = GetPartijBenaming(partij1, isAnoniem);
            var ouder2Naam = GetPartijBenaming(partij2, isAnoniem);

            // Fallback if benaming is empty
            if (string.IsNullOrEmpty(ouder1Naam)) ouder1Naam = "Ouder 1";
            if (string.IsNullOrEmpty(ouder2Naam)) ouder2Naam = "Ouder 2";

            if (alimentatie.IsKinderrekeningBetaalwijze)
            {
                return GetKinderrekeningBeschrijving(alimentatie, ouder1Naam, ouder2Naam);
            }
            else if (alimentatie.IsAlimentatieplichtBetaalwijze)
            {
                return GetAlimentatieBeschrijving(alimentatie, ouder1Naam, ouder2Naam);
            }

            return "";
        }

        /// <summary>
        /// Generate beschrijving for kinderrekening betaalwijze
        /// </summary>
        private string GetKinderrekeningBeschrijving(AlimentatieData alimentatie, string ouder1Naam, string ouder2Naam)
        {
            var paragrafen = new List<string>();

            // Paragraaf 1: Intro
            paragrafen.Add("Wij hebben ervoor gekozen om gebruik te maken van een gezamenlijke kinderrekening.");

            // Paragraaf 2: Kinderbijslag en kindgebonden budget
            var toeslagenZinnen = new List<string>();

            var kinderbijslagOntvanger = alimentatie.KinderbijslagOntvangerAlleKinderen?.ToLower();
            if (!string.IsNullOrEmpty(kinderbijslagOntvanger))
            {
                var ontvangerNaam = kinderbijslagOntvanger == "partij1" ? ouder1Naam : (kinderbijslagOntvanger == "partij2" ? ouder2Naam : "");
                if (!string.IsNullOrEmpty(ontvangerNaam))
                {
                    var actie = alimentatie.KinderbijslagStortenOpKinderrekening == true ? "stort deze op de kinderrekening" : "houdt deze";
                    toeslagenZinnen.Add($"{ontvangerNaam} ontvangt de kinderbijslag en {actie}.");
                }
            }

            var kgbOntvanger = alimentatie.KindgebondenBudgetAlleKinderen?.ToLower();
            if (!string.IsNullOrEmpty(kgbOntvanger))
            {
                var ontvangerNaam = kgbOntvanger == "partij1" ? ouder1Naam : (kgbOntvanger == "partij2" ? ouder2Naam : "");
                if (!string.IsNullOrEmpty(ontvangerNaam))
                {
                    var actie = alimentatie.KindgebondenBudgetStortenOpKinderrekening == true ? "stort deze op de kinderrekening" : "houdt deze";
                    toeslagenZinnen.Add($"{ontvangerNaam} ontvangt het kindgebonden budget en {actie}.");
                }
            }

            if (toeslagenZinnen.Any())
            {
                paragrafen.Add(string.Join(" ", toeslagenZinnen));
            }

            // Paragraaf 3: Kosten
            paragrafen.Add("Wij betalen allebei de eigen verblijfskosten.");

            // Paragraaf 4: Verblijfsoverstijgende kosten met bullet list
            if (alimentatie.KinderrekeningKostensoorten != null && alimentatie.KinderrekeningKostensoorten.Any())
            {
                var kostenLijst = FormatKostensoortenList(alimentatie.KinderrekeningKostensoorten);
                paragrafen.Add($"De verblijfsoverstijgende kosten betalen wij van de kinderrekening:\n{kostenLijst}\nVan deze rekening hebben wij allebei een pinpas.");
            }
            else
            {
                paragrafen.Add("De verblijfsoverstijgende kosten betalen wij van de kinderrekening. Van deze rekening hebben wij allebei een pinpas.");
            }

            // Paragraaf 5: Stortingen
            var stortingenZinnen = new List<string>();
            if (alimentatie.StortingOuder1Kinderrekening.HasValue && alimentatie.StortingOuder1Kinderrekening > 0)
            {
                stortingenZinnen.Add($"{ouder1Naam} zal iedere maand een bedrag van {DataFormatter.FormatCurrency(alimentatie.StortingOuder1Kinderrekening)} op deze rekening storten.");
            }
            if (alimentatie.StortingOuder2Kinderrekening.HasValue && alimentatie.StortingOuder2Kinderrekening > 0)
            {
                stortingenZinnen.Add($"{ouder2Naam} zal iedere maand een bedrag van {DataFormatter.FormatCurrency(alimentatie.StortingOuder2Kinderrekening)} op deze rekening storten.");
            }
            if (stortingenZinnen.Any())
            {
                paragrafen.Add(string.Join(" ", stortingenZinnen));
            }

            // Paragraaf 6: Controle en overleg
            paragrafen.Add("Wij zullen regelmatig controleren of onze bijdragen genoeg zijn om alle kosten te betalen. Als er structureel een tekort is, zullen wij in overleg met elkaar een hogere bijdrage op de kinderrekening storten.");

            // Paragraaf 7: Verantwoording
            paragrafen.Add("Wij zullen op verzoek aan elkaar uitleggen waarvoor wij bepaalde opnames van de kinderrekening hebben gedaan.");

            // Paragraaf 8: Maximum opnamebedrag (indien ingesteld)
            if (alimentatie.KinderrekeningMaximumOpname == true && alimentatie.KinderrekeningMaximumOpnameBedrag.HasValue && alimentatie.KinderrekeningMaximumOpnameBedrag > 0)
            {
                paragrafen.Add($"Per transactie kan maximaal {DataFormatter.FormatCurrency(alimentatie.KinderrekeningMaximumOpnameBedrag)} zonder overleg worden opgenomen.");
            }

            // Paragraaf 9: Opheffing
            var opheffingsOptie = alimentatie.KinderrekeningOpheffen?.ToLower();
            if (!string.IsNullOrEmpty(opheffingsOptie))
            {
                var opheffingsTekst = opheffingsOptie switch
                {
                    "helft" => "krijgen we ieder de helft van het saldo op de rekening",
                    "verhouding" => "krijgen we ieder het deel waar we recht op hebben in verhouding tot ieders bijdrage op de rekening",
                    "spaarrekening" => "maken we het saldo over op een spaarrekening van onze kinderen. Ieder kind krijgt dan evenveel",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(opheffingsTekst))
                {
                    paragrafen.Add($"Als de rekening wordt opgeheven, dan {opheffingsTekst}.");
                }
            }

            return string.Join("\n", paragrafen);
        }

        /// <summary>
        /// Generate beschrijving for alimentatie betaalwijze
        /// </summary>
        private string GetAlimentatieBeschrijving(AlimentatieData alimentatie, string ouder1Naam, string ouder2Naam)
        {
            var zinnen = new List<string>();

            // Intro
            zinnen.Add("Wij hebben ervoor gekozen om een maandelijkse kinderalimentatie af te spreken.");

            // Alimentatiegerechtigde ontvangt kinderbijslag en kgb
            var alimentatiegerechtigde = alimentatie.Alimentatiegerechtigde?.ToLower();
            var gerechtigdeNaam = alimentatiegerechtigde == "partij1" ? ouder1Naam : (alimentatiegerechtigde == "partij2" ? ouder2Naam : "");
            var plichtigeNaam = alimentatiegerechtigde == "partij1" ? ouder2Naam : (alimentatiegerechtigde == "partij2" ? ouder1Naam : "");

            if (!string.IsNullOrEmpty(gerechtigdeNaam))
            {
                zinnen.Add($"{gerechtigdeNaam} ontvangt en houdt de kinderbijslag en het kindgebonden budget.");
            }

            // Eigen verblijfskosten
            zinnen.Add("Wij betalen allebei de eigen verblijfskosten.");

            // Zorgkorting
            if (alimentatie.ZorgkortingPercentageAlleKinderen.HasValue)
            {
                zinnen.Add($"Wij houden rekening met een zorgkorting van {alimentatie.ZorgkortingPercentageAlleKinderen:0.##}%.");
            }

            // Verblijfsoverstijgende kosten
            if (!string.IsNullOrEmpty(gerechtigdeNaam))
            {
                zinnen.Add($"{gerechtigdeNaam} betaalt de verblijfsoverstijgende kosten.");
            }

            // Alimentatiebetaling
            if (!string.IsNullOrEmpty(plichtigeNaam) && !string.IsNullOrEmpty(gerechtigdeNaam) && alimentatie.AlimentatiebedragPerKind.HasValue)
            {
                var ingangsdatum = GetIngangsdatumTekst(alimentatie);
                zinnen.Add($"{plichtigeNaam} betaalt vanaf {ingangsdatum} een kinderalimentatie van {DataFormatter.FormatCurrency(alimentatie.AlimentatiebedragPerKind)} per kind per maand aan {gerechtigdeNaam}.");
            }

            // Indexering
            zinnen.Add("Het alimentatiebedrag wordt ieder jaar verhoogd op basis van de wettelijke indexering.");

            // Eerste indexering jaar
            if (alimentatie.EersteIndexeringJaar.HasValue)
            {
                zinnen.Add($"De eerste jaarlijkse verhoging is per 1 januari {alimentatie.EersteIndexeringJaar}.");
            }

            return string.Join("\n", zinnen);
        }

        /// <summary>
        /// Get the ingangsdatum text based on the option chosen
        /// </summary>
        private string GetIngangsdatumTekst(AlimentatieData alimentatie)
        {
            var optie = alimentatie.IngangsdatumOptie?.ToLower();

            return optie switch
            {
                "ondertekening" => "datum ondertekening",
                "anders" when !string.IsNullOrEmpty(alimentatie.IngangsdatumAnders) => alimentatie.IngangsdatumAnders,
                _ when alimentatie.Ingangsdatum.HasValue => alimentatie.Ingangsdatum.Value.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("nl-NL")),
                _ => "de ingangsdatum"
            };
        }

        /// <summary>
        /// Add communicatie afspraken (communication agreements) related replacements
        /// </summary>
        private void AddCommunicatieAfsprakenReplacements(
            Dictionary<string, string> replacements,
            CommunicatieAfsprakenData? communicatieAfspraken,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen)
        {
            // Initialize all placeholders with empty values first
            replacements["VillaPinedoKinderen"] = "";
            replacements["KinderenBetrokkenheid"] = "";
            replacements["KiesMethode"] = "";
            replacements["OmgangTekstOfSchema"] = "";
            replacements["OmgangsregelingBeschrijving"] = "";
            replacements["Opvang"] = "";
            replacements["OpvangBeschrijving"] = "";
            replacements["InformatieUitwisseling"] = "";
            replacements["InformatieUitwisselingBeschrijving"] = "";
            replacements["BijlageBeslissingen"] = "";
            replacements["SocialMedia"] = "";
            replacements["SocialMediaKeuze"] = "";
            replacements["SocialMediaLeeftijd"] = "";
            replacements["SocialMediaBeschrijving"] = "";
            replacements["MobielTablet"] = "";
            replacements["DeviceSmartphone"] = "";
            replacements["DeviceTablet"] = "";
            replacements["DeviceSmartwatch"] = "";
            replacements["DeviceLaptop"] = "";
            replacements["DevicesBeschrijving"] = "";
            replacements["ToezichtApps"] = "";
            replacements["ToezichtAppsBeschrijving"] = "";
            replacements["LocatieDelen"] = "";
            replacements["LocatieDelenBeschrijving"] = "";
            replacements["IdBewijzen"] = "";
            replacements["IdBewijzenBeschrijving"] = "";
            replacements["Aansprakelijkheidsverzekering"] = "";
            replacements["AansprakelijkheidsverzekeringBeschrijving"] = "";
            replacements["Ziektekostenverzekering"] = "";
            replacements["ZiektekostenverzekeringBeschrijving"] = "";
            replacements["ToestemmingReizen"] = "";
            replacements["ToestemmingReizenBeschrijving"] = "";
            replacements["Jongmeerderjarige"] = "";
            replacements["JongmeerderjarigeBeschrijving"] = "";
            replacements["Studiekosten"] = "";
            replacements["StudiekostenBeschrijving"] = "";
            replacements["BankrekeningKinderen"] = "";
            replacements["BankrekeningenCount"] = "0";
            replacements["Evaluatie"] = "";
            replacements["ParentingCoordinator"] = "";
            replacements["MediationClausule"] = "";

            // If no communicatie afspraken data, return early
            if (communicatieAfspraken == null)
            {
                _logger.LogDebug("No communicatie afspraken data available, placeholders set to empty strings");
                return;
            }

            // Basic communicatie afspraken data
            replacements["VillaPinedoKinderen"] = communicatieAfspraken.VillaPinedoKinderen ?? "";
            replacements["VillaPinedoZin"] = GetVillaPinedoZin(communicatieAfspraken.VillaPinedoKinderen, kinderen ?? new List<ChildData>());
            replacements["KinderenBetrokkenheid"] = communicatieAfspraken.KinderenBetrokkenheid ?? "";
            replacements["BetrokkenheidKindZin"] = GetBetrokkenheidKindZin(communicatieAfspraken.KinderenBetrokkenheid, kinderen ?? new List<ChildData>());
            replacements["KiesMethode"] = communicatieAfspraken.KiesMethode ?? "";
            replacements["OmgangTekstOfSchema"] = communicatieAfspraken.OmgangTekstOfSchema ?? "";
            replacements["OmgangsregelingBeschrijving"] = GetOmgangsregelingBeschrijving(
                communicatieAfspraken.OmgangTekstOfSchema,
                communicatieAfspraken.OmgangBeschrijving,
                kinderen?.Count ?? 0
            );
            replacements["Opvang"] = communicatieAfspraken.Opvang ?? "";
            replacements["OpvangBeschrijving"] = GetOpvangBeschrijving(communicatieAfspraken.Opvang);
            replacements["InformatieUitwisseling"] = communicatieAfspraken.InformatieUitwisseling ?? "";
            replacements["InformatieUitwisselingBeschrijving"] = GetInformatieUitwisselingBeschrijving(communicatieAfspraken.InformatieUitwisseling, kinderen ?? new List<ChildData>());
            replacements["BijlageBeslissingen"] = communicatieAfspraken.BijlageBeslissingen ?? "";
            replacements["IdBewijzen"] = communicatieAfspraken.IdBewijzen ?? "";
            replacements["IdBewijzenBeschrijving"] = GetIdBewijzenBeschrijving(communicatieAfspraken.IdBewijzen, partij1, partij2, kinderen);
            replacements["Aansprakelijkheidsverzekering"] = communicatieAfspraken.Aansprakelijkheidsverzekering ?? "";
            replacements["AansprakelijkheidsverzekeringBeschrijving"] = GetAansprakelijkheidsverzekeringBeschrijving(communicatieAfspraken.Aansprakelijkheidsverzekering, partij1, partij2, kinderen);
            replacements["Ziektekostenverzekering"] = communicatieAfspraken.Ziektekostenverzekering ?? "";
            replacements["ZiektekostenverzekeringBeschrijving"] = GetZiektekostenverzekeringBeschrijving(communicatieAfspraken.Ziektekostenverzekering, partij1, partij2, kinderen);
            replacements["ToestemmingReizen"] = communicatieAfspraken.ToestemmingReizen ?? "";
            replacements["ToestemmingReizenBeschrijving"] = GetToestemmingReizenBeschrijving(communicatieAfspraken.ToestemmingReizen, kinderen);
            replacements["Jongmeerderjarige"] = communicatieAfspraken.Jongmeerderjarige ?? "";
            replacements["JongmeerderjarigeBeschrijving"] = GetJongmeerderjarigeBeschrijving(communicatieAfspraken.Jongmeerderjarige, partij1, partij2);
            replacements["Studiekosten"] = communicatieAfspraken.Studiekosten ?? "";
            replacements["StudiekostenBeschrijving"] = GetStudiekostenBeschrijving(communicatieAfspraken.Studiekosten, partij1, partij2);
            replacements["Evaluatie"] = communicatieAfspraken.Evaluatie ?? "";
            replacements["ParentingCoordinator"] = communicatieAfspraken.ParentingCoordinator ?? "";
            replacements["MediationClausule"] = communicatieAfspraken.MediationClausule ?? "";

            // Parse social media field (can contain age: "wel_13")
            if (!string.IsNullOrEmpty(communicatieAfspraken.SocialMedia))
            {
                var (keuze, leeftijd) = ParseSocialMediaValue(communicatieAfspraken.SocialMedia);
                replacements["SocialMedia"] = communicatieAfspraken.SocialMedia;
                replacements["SocialMediaKeuze"] = keuze;
                replacements["SocialMediaLeeftijd"] = leeftijd;
            }
            replacements["SocialMediaBeschrijving"] = GetSocialMediaBeschrijving(communicatieAfspraken.SocialMedia, kinderen ?? new List<ChildData>());

            // Parse device afspraken (JSON object with device:age pairs)
            if (!string.IsNullOrEmpty(communicatieAfspraken.MobielTablet))
            {
                var deviceAfspraken = ParseDeviceAfspraken(communicatieAfspraken.MobielTablet);
                replacements["MobielTablet"] = FormatDeviceAfspraken(deviceAfspraken);
                replacements["DeviceSmartphone"] = deviceAfspraken.Smartphone?.ToString() ?? "";
                replacements["DeviceTablet"] = deviceAfspraken.Tablet?.ToString() ?? "";
                replacements["DeviceSmartwatch"] = deviceAfspraken.Smartwatch?.ToString() ?? "";
                replacements["DeviceLaptop"] = deviceAfspraken.Laptop?.ToString() ?? "";
                replacements["DevicesBeschrijving"] = GetDevicesBeschrijving(deviceAfspraken, kinderen ?? new List<ChildData>());
            }

            // Toezicht apps (parental supervision apps)
            replacements["ToezichtApps"] = communicatieAfspraken.ToezichtApps ?? "";
            replacements["ToezichtAppsBeschrijving"] = GetToezichtAppsBeschrijving(communicatieAfspraken.ToezichtApps);

            // Locatie delen (location sharing)
            replacements["LocatieDelen"] = communicatieAfspraken.LocatieDelen ?? "";
            replacements["LocatieDelenBeschrijving"] = GetLocatieDelenBeschrijving(communicatieAfspraken.LocatieDelen);

            // Parse bank accounts (JSON array)
            if (!string.IsNullOrEmpty(communicatieAfspraken.BankrekeningKinderen))
            {
                var bankrekeningen = ParseBankrekeningen(communicatieAfspraken.BankrekeningKinderen);
                replacements["BankrekeningKinderen"] = FormatBankrekeningen(bankrekeningen, partij1, partij2, kinderen ?? new List<ChildData>());
                replacements["BankrekeningenCount"] = bankrekeningen.Count.ToString();

                // Add individual bank account placeholders
                for (int i = 0; i < bankrekeningen.Count; i++)
                {
                    var rek = bankrekeningen[i];
                    replacements[$"Bankrekening{i + 1}IBAN"] = FormatIBAN(rek.Iban);
                    replacements[$"Bankrekening{i + 1}Tenaamstelling"] = TranslateTenaamstelling(rek.Tenaamstelling, partij1, partij2, kinderen ?? new List<ChildData>());
                    replacements[$"Bankrekening{i + 1}BankNaam"] = rek.BankNaam;
                }
            }

            _logger.LogDebug("Added communicatie afspraken data: VillaPinedo={VillaPinedo}, BankrekeningenCount={BankCount}",
                replacements["VillaPinedoKinderen"],
                replacements["BankrekeningenCount"]);
        }

        /// <summary>
        /// Parses social media value to extract choice and age
        /// Format: "wel_13" or simple "geen", "bepaalde_leeftijd", etc.
        /// </summary>
        private (string keuze, string leeftijd) ParseSocialMediaValue(string socialMedia)
        {
            if (string.IsNullOrEmpty(socialMedia))
                return ("", "");

            if (socialMedia.StartsWith("wel_"))
            {
                var parts = socialMedia.Split('_');
                if (parts.Length == 2)
                {
                    return ("wel", parts[1]);
                }
            }

            return (socialMedia, "");
        }

        /// <summary>
        /// Get the social media description based on the chosen option
        /// </summary>
        private string GetSocialMediaBeschrijving(string? socialMedia, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(socialMedia) || kinderen == null || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var zijnHun = kinderen.Count == 1
                ? (kinderen[0].Geslacht?.ToLowerInvariant() == "m" ? "zijn" : "haar")
                : "hun";

            var waarde = socialMedia.Trim().ToLowerInvariant();

            // Check for age pattern (wel_13)
            if (waarde.StartsWith("wel_"))
            {
                var leeftijd = waarde.Substring(4);
                return $"Wij spreken als ouders af dat {kinderenTekst} social media mogen gebruiken vanaf {zijnHun} {leeftijd}e jaar, op voorwaarde dat het op een veilige manier gebeurt.";
            }

            return waarde switch
            {
                "geen" => $"Wij spreken als ouders af dat {kinderenTekst} geen social media mogen gebruiken.",
                "wel" => $"Wij spreken als ouders af dat {kinderenTekst} social media mogen gebruiken, op voorwaarde dat het op een veilige manier gebeurt.",
                "later" => $"Wij maken als ouders later afspraken over het gebruik van social media door {kinderenTekst}.",
                _ => ""
            };
        }

        /// <summary>
        /// Parses device afspraken JSON object
        /// Format: {"smartphone":12,"tablet":14,"smartwatch":13,"laptop":16}
        /// </summary>
        private DeviceAfspraken ParseDeviceAfspraken(string jsonString)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<DeviceAfspraken>(jsonString) ?? new DeviceAfspraken();
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse device afspraken JSON: {Json}", jsonString);
                return new DeviceAfspraken();
            }
        }

        /// <summary>
        /// Formats device afspraken for display
        /// </summary>
        private string FormatDeviceAfspraken(DeviceAfspraken afspraken)
        {
            var lines = new List<string>();

            if (afspraken.Smartphone.HasValue)
                lines.Add($"- Smartphone: {afspraken.Smartphone} jaar");
            if (afspraken.Tablet.HasValue)
                lines.Add($"- Tablet: {afspraken.Tablet} jaar");
            if (afspraken.Smartwatch.HasValue)
                lines.Add($"- Smartwatch: {afspraken.Smartwatch} jaar");
            if (afspraken.Laptop.HasValue)
                lines.Add($"- Laptop: {afspraken.Laptop} jaar");

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Formats device afspraken as full sentences with children's names
        /// </summary>
        private string GetDevicesBeschrijving(DeviceAfspraken afspraken, List<ChildData> kinderen)
        {
            if (kinderen == null || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var krijgtKrijgen = kinderen.Count == 1 ? "krijgt" : "krijgen";
            var zijnHun = kinderen.Count == 1
                ? (kinderen[0].Geslacht?.ToLowerInvariant() == "m" ? "zijn" : "haar")
                : "hun";

            var zinnen = new List<string>();

            if (afspraken.Smartphone.HasValue)
                zinnen.Add($"{kinderenTekst} {krijgtKrijgen} een smartphone vanaf {zijnHun} {afspraken.Smartphone}e jaar.");
            if (afspraken.Tablet.HasValue)
                zinnen.Add($"{kinderenTekst} {krijgtKrijgen} een tablet vanaf {zijnHun} {afspraken.Tablet}e jaar.");
            if (afspraken.Smartwatch.HasValue)
                zinnen.Add($"{kinderenTekst} {krijgtKrijgen} een smartwatch vanaf {zijnHun} {afspraken.Smartwatch}e jaar.");
            if (afspraken.Laptop.HasValue)
                zinnen.Add($"{kinderenTekst} {krijgtKrijgen} een laptop vanaf {zijnHun} {afspraken.Laptop}e jaar.");

            return string.Join("\n", zinnen);
        }

        /// <summary>
        /// Get the toezicht apps description based on the chosen option (wel/geen)
        /// </summary>
        private string GetToezichtAppsBeschrijving(string? toezichtApps)
        {
            if (string.IsNullOrEmpty(toezichtApps))
                return "";

            var keuze = toezichtApps.Trim().ToLowerInvariant();

            return keuze switch
            {
                "wel" => "Wij spreken als ouders af wel ouderlijk toezichtapps te gebruiken.",
                "geen" => "Wij spreken als ouders af geen ouderlijk toezichtapps te gebruiken.",
                _ => ""
            };
        }

        /// <summary>
        /// Get the locatie delen description based on the chosen option (wel/geen)
        /// </summary>
        private string GetLocatieDelenBeschrijving(string? locatieDelen)
        {
            if (string.IsNullOrEmpty(locatieDelen))
                return "";

            var keuze = locatieDelen.Trim().ToLowerInvariant();

            return keuze switch
            {
                "wel" => "Wij spreken als ouders af om de locatie van onze kinderen wel te delen via digitale apparaten.",
                "geen" => "Wij spreken als ouders af om de locatie van onze kinderen niet te delen via digitale apparaten.",
                _ => ""
            };
        }

        /// <summary>
        /// Get the ID bewijzen (identity documents) description based on the chosen option
        /// Options: ouder_1, ouder_2, beide_ouders, kinderen_zelf, nvt
        /// </summary>
        private string GetIdBewijzenBeschrijving(string? idBewijzen, PersonData? partij1, PersonData? partij2, List<ChildData>? kinderen)
        {
            if (string.IsNullOrEmpty(idBewijzen) || kinderen == null || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = idBewijzen.Trim().ToLowerInvariant();

            var partij1Naam = partij1?.Roepnaam ?? partij1?.Voornamen ?? "Ouder 1";
            var partij2Naam = partij2?.Roepnaam ?? partij2?.Voornamen ?? "Ouder 2";

            return keuze switch
            {
                "ouder_1" or "partij1" => $"De identiteitsbewijzen van {kinderenTekst} worden bewaard door {partij1Naam}.",
                "ouder_2" or "partij2" => $"De identiteitsbewijzen van {kinderenTekst} worden bewaard door {partij2Naam}.",
                "beide_ouders" or "beiden" => $"De identiteitsbewijzen van {kinderenTekst} worden bewaard door beide ouders.",
                "kinderen_zelf" or "kinderen" => $"{kinderenTekst} {(kinderen.Count == 1 ? "bewaart" : "bewaren")} {(kinderen.Count == 1 ? "zijn/haar" : "hun")} eigen identiteitsbewijs.",
                "nvt" or "niet_van_toepassing" => "Niet van toepassing.",
                _ => ""
            };
        }

        /// <summary>
        /// Get the aansprakelijkheidsverzekering (liability insurance) description based on the chosen option
        /// Options: ouder_1, ouder_2, beiden, nvt
        /// </summary>
        private string GetAansprakelijkheidsverzekeringBeschrijving(string? aansprakelijkheidsverzekering, PersonData? partij1, PersonData? partij2, List<ChildData>? kinderen)
        {
            if (string.IsNullOrEmpty(aansprakelijkheidsverzekering) || kinderen == null || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = aansprakelijkheidsverzekering.Trim().ToLowerInvariant();

            var partij1Naam = partij1?.Roepnaam ?? partij1?.Voornamen ?? "Ouder 1";
            var partij2Naam = partij2?.Roepnaam ?? partij2?.Voornamen ?? "Ouder 2";

            return keuze switch
            {
                "beiden" or "beide_ouders" => $"Wij zorgen ervoor dat {kinderenTekst} bij ons beiden tegen wettelijke aansprakelijkheid {(kinderen.Count == 1 ? "is" : "zijn")} verzekerd.",
                "ouder_1" or "partij1" => $"{partij1Naam} zorgt ervoor dat {kinderenTekst} tegen wettelijke aansprakelijkheid {(kinderen.Count == 1 ? "is" : "zijn")} verzekerd.",
                "ouder_2" or "partij2" => $"{partij2Naam} zorgt ervoor dat {kinderenTekst} tegen wettelijke aansprakelijkheid {(kinderen.Count == 1 ? "is" : "zijn")} verzekerd.",
                "nvt" or "niet_van_toepassing" => "Niet van toepassing.",
                _ => ""
            };
        }

        /// <summary>
        /// Get the ziektekostenverzekering (health insurance) description based on the chosen option
        /// Options: ouder_1, ouder_2, hoofdverblijf, nvt
        /// </summary>
        private string GetZiektekostenverzekeringBeschrijving(string? ziektekostenverzekering, PersonData? partij1, PersonData? partij2, List<ChildData>? kinderen)
        {
            if (string.IsNullOrEmpty(ziektekostenverzekering) || kinderen == null || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = ziektekostenverzekering.Trim().ToLowerInvariant();

            var partij1Naam = partij1?.Roepnaam ?? partij1?.Voornamen ?? "Ouder 1";
            var partij2Naam = partij2?.Roepnaam ?? partij2?.Voornamen ?? "Ouder 2";
            var isZijn = kinderen.Count == 1 ? "is" : "zijn";
            var zijHun = kinderen.Count == 1
                ? (kinderen[0].Geslacht?.ToLowerInvariant() == "m" ? "hij zijn" : "zij haar")
                : "zij hun";

            return keuze switch
            {
                "ouder_1" or "partij1" => $"{kinderenTekst} {isZijn} verzekerd op de ziektekostenverzekering van {partij1Naam}.",
                "ouder_2" or "partij2" => $"{kinderenTekst} {isZijn} verzekerd op de ziektekostenverzekering van {partij2Naam}.",
                "hoofdverblijf" => $"{kinderenTekst} {isZijn} verzekerd op de ziektekostenverzekering van de ouder waar {zijHun} hoofdverblijf {(kinderen.Count == 1 ? "heeft" : "hebben")}.",
                "nvt" or "niet_van_toepassing" => "Niet van toepassing.",
                _ => ""
            };
        }

        /// <summary>
        /// Get the toestemming reizen (travel permission) description based on the chosen option
        /// Options: altijd_overleggen, eu_vrij, vrij, schriftelijk
        /// </summary>
        private string GetToestemmingReizenBeschrijving(string? toestemmingReizen, List<ChildData>? kinderen)
        {
            if (string.IsNullOrEmpty(toestemmingReizen) || kinderen == null || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = toestemmingReizen.Trim().ToLowerInvariant();

            return keuze switch
            {
                "altijd_overleggen" or "altijd" => $"Voor reizen met {kinderenTekst} is altijd vooraf overleg tussen de ouders vereist.",
                "eu_vrij" => $"Met {kinderenTekst} mag binnen de EU vrij worden gereisd. Voor reizen buiten de EU is vooraf overleg tussen de ouders vereist.",
                "vrij" => $"Met {kinderenTekst} mag vrij worden gereisd zonder vooraf overleg.",
                "schriftelijk" => $"Voor reizen met {kinderenTekst} is schriftelijke toestemming van de andere ouder vereist.",
                _ => ""
            };
        }

        /// <summary>
        /// Get the jongmeerderjarige (young adult 18-21) description based on the chosen option
        /// Options: bijdrage_rechtstreeks_kind, bijdrage_rechtstreeks_uitwonend, bijdrage_beiden, ouder1, ouder2, geen_bijdrage, nvt
        /// </summary>
        private string GetJongmeerderjarigeBeschrijving(string? jongmeerderjarige, PersonData? partij1, PersonData? partij2)
        {
            if (string.IsNullOrEmpty(jongmeerderjarige))
                return "";

            var ouder1Naam = partij1?.Roepnaam ?? partij1?.Voornamen ?? "de vader";
            var ouder2Naam = partij2?.Roepnaam ?? partij2?.Voornamen ?? "de moeder";
            var keuze = jongmeerderjarige.Trim().ToLowerInvariant();

            return keuze switch
            {
                "bijdrage_rechtstreeks_kind" => "De ouders betalen een bijdrage rechtstreeks aan het kind.",
                "bijdrage_rechtstreeks_uitwonend" => "De ouders betalen een bijdrage rechtstreeks aan het kind als het kind niet meer thuiswoont.",
                "bijdrage_beiden" => "Beide ouders blijven bijdragen aan de kosten voor het jongmeerderjarige kind.",
                "ouder1" => $"{ouder1Naam} blijft bijdragen aan de kosten voor het jongmeerderjarige kind.",
                "ouder2" => $"{ouder2Naam} blijft bijdragen aan de kosten voor het jongmeerderjarige kind.",
                "geen_bijdrage" => "Er is geen bijdrage meer verschuldigd als het kind voldoende eigen inkomen heeft.",
                "nvt" => "",
                _ => ""
            };
        }

        /// <summary>
        /// Get the studiekosten (study costs) description based on the chosen option
        /// Options: draagkracht_rato, netto_inkomen_rato, beide_helft, evenredig, ouder1, ouder2, kind_zelf, nvt
        /// </summary>
        private string GetStudiekostenBeschrijving(string? studiekosten, PersonData? partij1, PersonData? partij2)
        {
            if (string.IsNullOrEmpty(studiekosten))
                return "";

            var ouder1Naam = partij1?.Roepnaam ?? partij1?.Voornamen ?? "de vader";
            var ouder2Naam = partij2?.Roepnaam ?? partij2?.Voornamen ?? "de moeder";
            var keuze = studiekosten.Trim().ToLowerInvariant();

            return keuze switch
            {
                "draagkracht_rato" => "Beide ouders dragen naar rato van hun draagkracht bij aan de studiekosten.",
                "netto_inkomen_rato" => "De ouders dragen naar rato van hun netto inkomen bij aan de studiekosten.",
                "beide_helft" => "Beide ouders dragen voor de helft bij aan de studiekosten.",
                "evenredig" => "De ouders dragen evenredig naar inkomen bij aan de studiekosten.",
                "ouder1" => $"{ouder1Naam} betaalt de studiekosten.",
                "ouder2" => $"{ouder2Naam} betaalt de studiekosten.",
                "kind_zelf" => "Het kind betaalt de studiekosten zelf (via lening en/of werk).",
                "nvt" => "",
                _ => ""
            };
        }

        /// <summary>
        /// Parses bankrekeningen JSON array
        /// Format: [{"iban":"NL91ABNA0417164300","tenaamstelling":"ouder_1","bankNaam":"ABN AMRO"}]
        /// </summary>
        private List<Kinderrekening> ParseBankrekeningen(string jsonString)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<Kinderrekening>>(jsonString) ?? new List<Kinderrekening>();
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse bankrekeningen JSON: {Json}", jsonString);
                return new List<Kinderrekening>();
            }
        }

        /// <summary>
        /// Formats bank accounts for display
        /// </summary>
        private string FormatBankrekeningen(List<Kinderrekening> rekeningen, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen)
        {
            if (!rekeningen.Any())
                return "";

            var lines = new List<string>();

            for (int i = 0; i < rekeningen.Count; i++)
            {
                var rek = rekeningen[i];
                lines.Add($"Rekening {i + 1}:");
                lines.Add($"  IBAN: {FormatIBAN(rek.Iban)}");
                lines.Add($"  Bank: {rek.BankNaam}");
                lines.Add($"  Ten name van: {TranslateTenaamstelling(rek.Tenaamstelling, partij1, partij2, kinderen)}");
                if (i < rekeningen.Count - 1)
                    lines.Add(""); // Empty line between accounts
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Formats IBAN with spaces for readability
        /// Example: NL91ABNA0417164300 -> NL91 ABNA 0417 1643 00
        /// </summary>
        private string FormatIBAN(string iban)
        {
            if (string.IsNullOrEmpty(iban))
                return "";

            // Remove any existing spaces
            iban = iban.Replace(" ", "");

            // Add space every 4 characters
            var formatted = "";
            for (int i = 0; i < iban.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    formatted += " ";
                formatted += iban[i];
            }

            return formatted;
        }

        /// <summary>
        /// Translates tenaamstelling codes to readable text
        /// Codes: "ouder_1", "ouder_2", "ouders_gezamenlijk", "kind_123", "kinderen_alle"
        /// </summary>
        private string TranslateTenaamstelling(string code, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            // Handle parent codes
            if (code == "ouder_1" && partij1 != null)
                return $"Op naam van {partij1.Roepnaam ?? partij1.Voornamen ?? "Partij 1"}";

            if (code == "ouder_2" && partij2 != null)
                return $"Op naam van {partij2.Roepnaam ?? partij2.Voornamen ?? "Partij 2"}";

            if (code == "ouders_gezamenlijk" && partij1 != null && partij2 != null)
            {
                var naam1 = partij1.Roepnaam ?? partij1.Voornamen ?? "Partij 1";
                var naam2 = partij2.Roepnaam ?? partij2.Voornamen ?? "Partij 2";
                return $"Op gezamenlijke naam van {naam1} en {naam2}";
            }

            // Handle all children code
            if (code == "kinderen_alle")
            {
                var minderjarigen = kinderen.Where(k => CalculateAge(k.GeboorteDatum) < 18).ToList();
                if (minderjarigen.Any())
                {
                    var namen = minderjarigen.Select(k => k.Roepnaam ?? k.Voornamen ?? k.Achternaam).ToList();
                    return $"Op naam van {DutchLanguageHelper.FormatList(namen)}";
                }
                return "Op naam van alle minderjarige kinderen";
            }

            // Handle individual child codes (format: "kind_123" where 123 is the child ID)
            if (code.StartsWith("kind_"))
            {
                var kindIdStr = code.Substring(5);
                if (int.TryParse(kindIdStr, out int kindId))
                {
                    var kind = kinderen.FirstOrDefault(k => k.Id == kindId);
                    if (kind != null)
                    {
                        var kindNaam = kind.Roepnaam ?? kind.Voornamen ?? kind.Achternaam;
                        return $"Op naam van {kindNaam}";
                    }
                }
            }

            // If no match found, return the code as-is
            return code;
        }

        /// <summary>
        /// Calculates age from birth date
        /// </summary>
        private int CalculateAge(DateTime? geboorteDatum)
        {
            if (!geboorteDatum.HasValue)
                return 0;

            var today = DateTime.Today;
            var age = today.Year - geboorteDatum.Value.Year;

            if (geboorteDatum.Value.Date > today.AddYears(-age))
                age--;

            return age;
        }

        /// <summary>
        /// Gets the hoofdverblijf distribution text based on where children have their primary residence
        /// Handles co-parenting, single parent residence, and mixed scenarios
        /// </summary>
        private string GetHoofdverblijfVerdeling(
            AlimentatieData? alimentatie,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen,
            bool? isAnoniem)
        {
            // If no alimentatie data or no children, return empty
            if (alimentatie == null || kinderen == null || !kinderen.Any())
                return "";

            // If no financial agreements per child, return empty
            if (!alimentatie.FinancieleAfsprakenKinderen.Any())
                return "";

            // Group children by hoofdverblijf
            var kinderenBijPartij1 = new List<ChildData>();
            var kinderenBijPartij2 = new List<ChildData>();
            var kinderenCoOuderschap = new List<ChildData>();

            foreach (var kind in kinderen)
            {
                var afspraak = alimentatie.FinancieleAfsprakenKinderen
                    .FirstOrDefault(f => f.KindId == kind.Id);

                if (afspraak != null && !string.IsNullOrEmpty(afspraak.Hoofdverblijf))
                {
                    var hoofdverblijf = afspraak.Hoofdverblijf.ToLower().Trim();

                    if (hoofdverblijf == "partij1")
                        kinderenBijPartij1.Add(kind);
                    else if (hoofdverblijf == "partij2")
                        kinderenBijPartij2.Add(kind);
                    else if (hoofdverblijf.Contains("co-ouderschap") || hoofdverblijf.Contains("coouderschap"))
                        kinderenCoOuderschap.Add(kind);
                }
            }

            // Build result sentences
            var zinnen = new List<string>();

            // Handle children at partij1
            if (kinderenBijPartij1.Any())
            {
                var namen = kinderenBijPartij1.Select(k => k.Naam).ToList();
                var namenTekst = DutchLanguageHelper.FormatList(namen);
                var heeftHebbben = kinderenBijPartij1.Count == 1 ? "heeft" : "hebben";
                var zijnHaarHun = kinderenBijPartij1.Count == 1
                    ? (kinderenBijPartij1[0].Geslacht?.ToLower() == "m" ? "zijn" : "haar")
                    : "hun";
                var partij1Benaming = GetPartijBenaming(partij1, isAnoniem);

                zinnen.Add($"{namenTekst} {heeftHebbben} {zijnHaarHun} hoofdverblijf bij {partij1Benaming}.");
            }

            // Handle children at partij2
            if (kinderenBijPartij2.Any())
            {
                var namen = kinderenBijPartij2.Select(k => k.Naam).ToList();
                var namenTekst = DutchLanguageHelper.FormatList(namen);
                var heeftHebbben = kinderenBijPartij2.Count == 1 ? "heeft" : "hebben";
                var zijnHaarHun = kinderenBijPartij2.Count == 1
                    ? (kinderenBijPartij2[0].Geslacht?.ToLower() == "m" ? "zijn" : "haar")
                    : "hun";
                var partij2Benaming = GetPartijBenaming(partij2, isAnoniem);

                zinnen.Add($"{namenTekst} {heeftHebbben} {zijnHaarHun} hoofdverblijf bij {partij2Benaming}.");
            }

            // Handle co-parenting children
            if (kinderenCoOuderschap.Any())
            {
                var namen = kinderenCoOuderschap.Select(k => k.Naam).ToList();
                var enkelvoud = kinderenCoOuderschap.Count == 1;

                if (enkelvoud)
                {
                    var kindNaam = namen[0];
                    var verblijft = "verblijft";
                    var zijnHaar = kinderenCoOuderschap[0].Geslacht?.ToLower() == "m" ? "hij" : "zij";
                    var heeftZin = $"{zijnHaar} heeft";

                    zinnen.Add($"Voor {kindNaam} hebben wij een zorgregeling afgesproken waarbij {zijnHaar} ongeveer evenveel tijd bij ieder van ons {verblijft}. {char.ToUpper(zijnHaar[0]) + zijnHaar.Substring(1)} {heeftZin} dus geen hoofdverblijf.");
                }
                else
                {
                    var namenTekst = DutchLanguageHelper.FormatList(namen);
                    zinnen.Add($"Voor {namenTekst} hebben wij een zorgregeling afgesproken waarbij zij ongeveer evenveel tijd bij ieder van ons verblijven. Zij hebben dus geen hoofdverblijf.");
                }
            }

            // Special case: all children have co-parenting and it's the only arrangement
            if (kinderenCoOuderschap.Count == kinderen.Count && !kinderenBijPartij1.Any() && !kinderenBijPartij2.Any())
            {
                var enkelvoud = kinderen.Count == 1;
                var onzeTekst = enkelvoud ? "ons kind" : "onze kinderen";
                var verblijftVerblijven = enkelvoud ? "verblijft" : "verblijven";
                var heeftHebben = enkelvoud ? "heeft" : "hebben";
                var hetKindZij = enkelvoud
                    ? (kinderen[0].Geslacht?.ToLower() == "m" ? "Het kind" : "Het kind")
                    : "Zij";

                return $"Wij hebben een zorgregeling afgesproken waarbij {onzeTekst} ongeveer evenveel tijd bij ieder van ons {verblijftVerblijven}. {hetKindZij} {heeftHebben} dus geen hoofdverblijf.";
            }

            return string.Join(" ", zinnen);
        }

        /// <summary>
        /// Gets the BRP registration (inschrijving) distribution text based on where children are registered
        /// </summary>
        private string GetInschrijvingVerdeling(
            AlimentatieData? alimentatie,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen,
            bool? isAnoniem)
        {
            // If no alimentatie data or no children, return empty
            if (alimentatie == null || kinderen == null || !kinderen.Any())
                return "";

            // If no financial agreements per child, return empty
            if (!alimentatie.FinancieleAfsprakenKinderen.Any())
                return "";

            // Group children by inschrijving (BRP registration)
            var kinderenBijPartij1 = new List<ChildData>();
            var kinderenBijPartij2 = new List<ChildData>();

            foreach (var kind in kinderen)
            {
                var afspraak = alimentatie.FinancieleAfsprakenKinderen
                    .FirstOrDefault(f => f.KindId == kind.Id);

                if (afspraak != null && !string.IsNullOrEmpty(afspraak.Inschrijving))
                {
                    var inschrijving = afspraak.Inschrijving.ToLower().Trim();

                    if (inschrijving == "partij1")
                        kinderenBijPartij1.Add(kind);
                    else if (inschrijving == "partij2")
                        kinderenBijPartij2.Add(kind);
                }
            }

            // Build result sentences
            var zinnen = new List<string>();

            // Handle children registered at partij1
            if (kinderenBijPartij1.Any())
            {
                var namen = kinderenBijPartij1.Select(k => k.Naam).ToList();
                var namenTekst = DutchLanguageHelper.FormatList(namen);
                var zalZullen = kinderenBijPartij1.Count == 1 ? "zal" : "zullen";
                var partij1Benaming = GetPartijBenaming(partij1, isAnoniem);
                var plaats1 = partij1?.Plaats ?? "onbekend";

                zinnen.Add($"{namenTekst} {zalZullen} ingeschreven staan in de Basisregistratie Personen aan het adres van {partij1Benaming} in {plaats1}.");
            }

            // Handle children registered at partij2
            if (kinderenBijPartij2.Any())
            {
                var namen = kinderenBijPartij2.Select(k => k.Naam).ToList();
                var namenTekst = DutchLanguageHelper.FormatList(namen);
                var zalZullen = kinderenBijPartij2.Count == 1 ? "zal" : "zullen";
                var partij2Benaming = GetPartijBenaming(partij2, isAnoniem);
                var plaats2 = partij2?.Plaats ?? "onbekend";

                zinnen.Add($"{namenTekst} {zalZullen} ingeschreven staan in de Basisregistratie Personen aan het adres van {partij2Benaming} in {plaats2}.");
            }

            return string.Join(" ", zinnen);
        }

        #endregion
    }
}