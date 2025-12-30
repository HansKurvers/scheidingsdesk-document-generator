using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.Artikel
{
    /// <summary>
    /// Service voor het verwerken van artikelen uit de bibliotheek
    /// Bevat logica voor conditionele filtering en placeholder vervanging
    /// </summary>
    public class ArtikelService : IArtikelService
    {
        private readonly ILogger<ArtikelService> _logger;

        // Regex patronen voor conditionele blokken en placeholders
        private static readonly Regex IfEndIfPattern = new Regex(
            @"\[\[IF:(\w+)\]\](.*?)\[\[ENDIF:\1\]\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex PlaceholderPattern = new Regex(
            @"\[\[(\w+)\]\]",
            RegexOptions.IgnoreCase);

        public ArtikelService(ILogger<ArtikelService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Filtert conditionele artikelen op basis van beschikbare data
        /// </summary>
        public List<ArtikelData> FilterConditioneleArtikelen(
            List<ArtikelData> artikelen,
            Dictionary<string, string> replacements)
        {
            if (artikelen == null || artikelen.Count == 0)
                return new List<ArtikelData>();

            var result = new List<ArtikelData>();

            foreach (var artikel in artikelen)
            {
                // Niet-conditionele artikelen altijd toevoegen
                if (!artikel.IsConditioneel || string.IsNullOrEmpty(artikel.ConditieVeld))
                {
                    result.Add(artikel);
                    continue;
                }

                // Evalueer conditie
                if (EvalueerConditie(artikel.ConditieVeld, replacements))
                {
                    result.Add(artikel);
                    _logger.LogDebug($"Artikel '{artikel.ArtikelCode}' toegevoegd (conditie '{artikel.ConditieVeld}' is waar)");
                }
                else
                {
                    _logger.LogDebug($"Artikel '{artikel.ArtikelCode}' gefilterd (conditie '{artikel.ConditieVeld}' is onwaar)");
                }
            }

            return result;
        }

        /// <summary>
        /// Vervangt [[Placeholder]] syntax met waarden uit replacements
        /// </summary>
        public string VervangPlaceholders(string tekst, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(tekst))
                return tekst;

            return PlaceholderPattern.Replace(tekst, match =>
            {
                var placeholder = match.Groups[1].Value;

                // Probeer exacte match
                if (replacements.TryGetValue(placeholder, out var value))
                    return value;

                // Probeer case-insensitive match
                var key = replacements.Keys.FirstOrDefault(k =>
                    k.Equals(placeholder, StringComparison.OrdinalIgnoreCase));

                if (key != null)
                    return replacements[key];

                // Placeholder niet gevonden, laat staan voor debugging
                _logger.LogWarning($"Placeholder niet gevonden: [[{placeholder}]]");
                return match.Value;
            });
        }

        /// <summary>
        /// Verwerkt [[IF:Veld]]...[[ENDIF:Veld]] blokken binnen tekst
        /// </summary>
        public string VerwerkConditioneleBlokken(string tekst, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(tekst))
                return tekst;

            // Blijf verwerken totdat er geen matches meer zijn (voor geneste blokken)
            var previousTekst = "";
            var currentTekst = tekst;
            int maxIterations = 10; // Voorkom oneindige loops
            int iteration = 0;

            while (currentTekst != previousTekst && iteration < maxIterations)
            {
                previousTekst = currentTekst;
                currentTekst = IfEndIfPattern.Replace(currentTekst, match =>
                {
                    var veldNaam = match.Groups[1].Value;
                    var inhoud = match.Groups[2].Value;

                    if (EvalueerConditie(veldNaam, replacements))
                    {
                        // Conditie waar: behoud inhoud (zonder tags)
                        return inhoud.Trim();
                    }
                    else
                    {
                        // Conditie onwaar: verwijder hele blok
                        return "";
                    }
                });
                iteration++;
            }

            // Verwijder lege regels die kunnen ontstaan na verwijdering van conditionele blokken
            currentTekst = Regex.Replace(currentTekst, @"(\r?\n){3,}", "\n\n");

            return currentTekst.Trim();
        }

        /// <summary>
        /// Past alle transformaties toe op een artikel tekst
        /// </summary>
        public string VerwerkArtikelTekst(ArtikelData artikel, Dictionary<string, string> replacements)
        {
            var tekst = artikel.EffectieveTekst;

            // 1. Verwerk eerst conditionele blokken
            tekst = VerwerkConditioneleBlokken(tekst, replacements);

            // 2. Vervang daarna placeholders
            tekst = VervangPlaceholders(tekst, replacements);

            return tekst;
        }

        /// <summary>
        /// Evalueert of een conditie waar is op basis van de replacements
        /// </summary>
        private bool EvalueerConditie(string conditie, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(conditie))
                return true;

            // Ondersteun NOT operator (bijv. "!HeeftKinderrekening")
            bool isNegated = conditie.StartsWith("!");
            var veldNaam = isNegated ? conditie.Substring(1) : conditie;

            // Zoek waarde in replacements
            var heeftWaarde = HeeftWaarde(veldNaam, replacements);

            return isNegated ? !heeftWaarde : heeftWaarde;
        }

        /// <summary>
        /// Controleert of een veld een niet-lege waarde heeft
        /// </summary>
        private bool HeeftWaarde(string veldNaam, Dictionary<string, string> replacements)
        {
            // Exacte match
            if (replacements.TryGetValue(veldNaam, out var value))
            {
                return !string.IsNullOrWhiteSpace(value) && value != "0" && value.ToLower() != "false";
            }

            // Case-insensitive match
            var key = replacements.Keys.FirstOrDefault(k =>
                k.Equals(veldNaam, StringComparison.OrdinalIgnoreCase));

            if (key != null)
            {
                var val = replacements[key];
                return !string.IsNullOrWhiteSpace(val) && val != "0" && val.ToLower() != "false";
            }

            return false;
        }
    }
}
