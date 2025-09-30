using System;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper class for Dutch language specific operations
    /// Handles grammar rules, list formatting, and language conventions
    /// </summary>
    public static class DutchLanguageHelper
    {
        /// <summary>
        /// Formats a list of items with proper Dutch grammar
        /// Examples:
        /// - 1 item: "Emma"
        /// - 2 items: "Kees en Emma"
        /// - 3+ items: "Bart, Kees en Emma"
        /// </summary>
        /// <param name="items">List of items to format</param>
        /// <returns>Formatted Dutch list string</returns>
        public static string FormatList(List<string> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            if (items.Count == 1)
                return items[0];

            if (items.Count == 2)
                return $"{items[0]} en {items[1]}";

            // For 3 or more items: "item1, item2, ... en lastItem"
            var allButLast = string.Join(", ", items.Take(items.Count - 1));
            return $"{allButLast} en {items.Last()}";
        }

        /// <summary>
        /// Gets the appropriate object pronoun based on gender and plurality
        /// </summary>
        /// <param name="geslacht">Gender (M/V/Man/Vrouw)</param>
        /// <param name="isPlural">Whether this is for multiple people</param>
        /// <returns>Object pronoun (hem/haar/hen)</returns>
        public static string GetObjectPronoun(string? geslacht, bool isPlural)
        {
            if (isPlural)
                return "hen";

            if (string.IsNullOrEmpty(geslacht))
                return "hem/haar";

            bool isMale = string.Equals(geslacht, "M", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(geslacht, "Man", StringComparison.OrdinalIgnoreCase);
            bool isFemale = string.Equals(geslacht, "V", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(geslacht, "Vrouw", StringComparison.OrdinalIgnoreCase);

            if (isMale)
                return "hem";
            if (isFemale)
                return "haar";

            return "hem/haar";
        }

        /// <summary>
        /// Gets the appropriate subject pronoun based on gender and plurality
        /// </summary>
        /// <param name="geslacht">Gender (M/V/Man/Vrouw)</param>
        /// <param name="isPlural">Whether this is for multiple people</param>
        /// <returns>Subject pronoun (hij/zij/ze)</returns>
        public static string GetSubjectPronoun(string? geslacht, bool isPlural)
        {
            if (isPlural)
                return "ze";

            if (string.IsNullOrEmpty(geslacht))
                return "hij/zij";

            bool isMale = string.Equals(geslacht, "M", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(geslacht, "Man", StringComparison.OrdinalIgnoreCase);
            bool isFemale = string.Equals(geslacht, "V", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(geslacht, "Vrouw", StringComparison.OrdinalIgnoreCase);

            if (isMale)
                return "hij";
            if (isFemale)
                return "zij";

            return "hij/zij";
        }

        /// <summary>
        /// Gets the plural or singular form of common verb conjugations
        /// </summary>
        /// <param name="singularForm">Singular verb form</param>
        /// <param name="pluralForm">Plural verb form</param>
        /// <param name="isPlural">Whether to use plural</param>
        /// <returns>Appropriate verb form</returns>
        public static string GetVerbForm(string singularForm, string pluralForm, bool isPlural)
        {
            return isPlural ? pluralForm : singularForm;
        }

        /// <summary>
        /// Gets the plural or singular form of "kind/kinderen"
        /// </summary>
        /// <param name="isPlural">Whether to use plural</param>
        /// <returns>"ons kind" or "onze kinderen"</returns>
        public static string GetChildTerm(bool isPlural)
        {
            return isPlural ? "onze kinderen" : "ons kind";
        }

        /// <summary>
        /// Common Dutch verb conjugations (singular/plural)
        /// </summary>
        public static class VerbForms
        {
            public static string Heeft_Hebben(bool isPlural) => isPlural ? "hebben" : "heeft";
            public static string Is_Zijn(bool isPlural) => isPlural ? "zijn" : "is";
            public static string Verblijft_Verblijven(bool isPlural) => isPlural ? "verblijven" : "verblijft";
            public static string Kan_Kunnen(bool isPlural) => isPlural ? "kunnen" : "kan";
            public static string Zal_Zullen(bool isPlural) => isPlural ? "zullen" : "zal";
            public static string Moet_Moeten(bool isPlural) => isPlural ? "moeten" : "moet";
            public static string Wordt_Worden(bool isPlural) => isPlural ? "worden" : "wordt";
            public static string Blijft_Blijven(bool isPlural) => isPlural ? "blijven" : "blijft";
            public static string Gaat_Gaan(bool isPlural) => isPlural ? "gaan" : "gaat";
            public static string Komt_Komen(bool isPlural) => isPlural ? "komen" : "komt";
        }
    }
}