using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Builds Dutch grammar rules based on child data
    /// Handles singular/plural forms and gender-specific pronouns
    /// </summary>
    public class GrammarRulesBuilder
    {
        private readonly ILogger<GrammarRulesBuilder> _logger;

        public GrammarRulesBuilder(ILogger<GrammarRulesBuilder> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates grammar rules based on the number of minor children and their genders
        /// </summary>
        /// <param name="children">List of all children</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Dictionary of grammar rule replacements</returns>
        public Dictionary<string, string> BuildRules(List<ChildData> children, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Building grammar rules for {children.Count} children");

            var rules = new Dictionary<string, string>();

            // Count minor children (under 18)
            var minorChildren = children.Where(c => c.Leeftijd.HasValue && c.Leeftijd.Value < 18).ToList();
            int minorCount = minorChildren.Count;
            bool isPlural = minorCount > 1;

            _logger.LogInformation($"[{correlationId}] Found {minorCount} minor children (isPlural: {isPlural})");

            // Basic singular/plural rules
            rules["ons kind/onze kinderen"] = DutchLanguageHelper.GetChildTerm(isPlural);
            rules["heeft/hebben"] = DutchLanguageHelper.VerbForms.Heeft_Hebben(isPlural);
            rules["is/zijn"] = DutchLanguageHelper.VerbForms.Is_Zijn(isPlural);
            rules["verblijft/verblijven"] = DutchLanguageHelper.VerbForms.Verblijft_Verblijven(isPlural);
            rules["kan/kunnen"] = DutchLanguageHelper.VerbForms.Kan_Kunnen(isPlural);
            rules["zal/zullen"] = DutchLanguageHelper.VerbForms.Zal_Zullen(isPlural);
            rules["moet/moeten"] = DutchLanguageHelper.VerbForms.Moet_Moeten(isPlural);
            rules["wordt/worden"] = DutchLanguageHelper.VerbForms.Wordt_Worden(isPlural);
            rules["blijft/blijven"] = DutchLanguageHelper.VerbForms.Blijft_Blijven(isPlural);
            rules["gaat/gaan"] = DutchLanguageHelper.VerbForms.Gaat_Gaan(isPlural);
            rules["komt/komen"] = DutchLanguageHelper.VerbForms.Komt_Komen(isPlural);

            // Gender and count specific pronouns
            if (isPlural)
            {
                // Multiple children - use plural pronouns
                rules["hem/haar/hen"] = DutchLanguageHelper.GetObjectPronoun(null, isPlural: true);
                rules["hij/zij/ze"] = DutchLanguageHelper.GetSubjectPronoun(null, isPlural: true);
            }
            else if (minorCount == 1)
            {
                // Single child - use gender-specific pronouns
                var child = minorChildren.First();
                rules["hem/haar/hen"] = DutchLanguageHelper.GetObjectPronoun(child.Geslacht, isPlural: false);
                rules["hij/zij/ze"] = DutchLanguageHelper.GetSubjectPronoun(child.Geslacht, isPlural: false);

                _logger.LogDebug($"[{correlationId}] Single child gender: {child.Geslacht}, pronouns: {rules["hij/zij/ze"]}/{rules["hem/haar/hen"]}");
            }
            else
            {
                // No minor children - use neutral forms
                rules["hem/haar/hen"] = "hem/haar";
                rules["hij/zij/ze"] = "hij/zij";
            }

            _logger.LogInformation($"[{correlationId}] Created {rules.Count} grammar rules");

            return rules;
        }

        /// <summary>
        /// Creates grammar rules based on child count only (simpler version)
        /// </summary>
        /// <param name="childCount">Number of children</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Dictionary of grammar rule replacements</returns>
        public Dictionary<string, string> BuildSimpleRules(int childCount, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Building simple grammar rules for {childCount} children");

            bool isPlural = childCount > 1;

            var rules = new Dictionary<string, string>
            {
                ["ons kind/onze kinderen"] = DutchLanguageHelper.GetChildTerm(isPlural),
                ["heeft/hebben"] = DutchLanguageHelper.VerbForms.Heeft_Hebben(isPlural),
                ["is/zijn"] = DutchLanguageHelper.VerbForms.Is_Zijn(isPlural),
                ["verblijft/verblijven"] = DutchLanguageHelper.VerbForms.Verblijft_Verblijven(isPlural),
                ["kan/kunnen"] = DutchLanguageHelper.VerbForms.Kan_Kunnen(isPlural),
                ["zal/zullen"] = DutchLanguageHelper.VerbForms.Zal_Zullen(isPlural),
                ["moet/moeten"] = DutchLanguageHelper.VerbForms.Moet_Moeten(isPlural),
                ["wordt/worden"] = DutchLanguageHelper.VerbForms.Wordt_Worden(isPlural),
                ["blijft/blijven"] = DutchLanguageHelper.VerbForms.Blijft_Blijven(isPlural),
                ["gaat/gaan"] = DutchLanguageHelper.VerbForms.Gaat_Gaan(isPlural),
                ["komt/komen"] = DutchLanguageHelper.VerbForms.Komt_Komen(isPlural),
                ["hem/haar/hen"] = isPlural ? "hen" : "hem/haar",
                ["hij/zij/ze"] = isPlural ? "ze" : "hij/zij"
            };

            _logger.LogInformation($"[{correlationId}] Created {rules.Count} simple grammar rules");

            return rules;
        }
    }
}