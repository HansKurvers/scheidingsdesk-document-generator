using System;
using System.Linq;

namespace scheidingsdesk_document_generator.Constants
{
    /// <summary>
    /// Centralized template type constants used across the application
    /// </summary>
    public static class TemplateTypes
    {
        public const string Feestdag = "Feestdag";
        public const string Vakantie = "Vakantie";
        public const string Algemeen = "Algemeen";
        public const string BijzondereDag = "Bijzondere dag";

        /// <summary>
        /// Default template types when database is empty or unavailable
        /// </summary>
        public static readonly string[] DefaultTypes = new[]
        {
            Feestdag,
            Vakantie,
            Algemeen,
            BijzondereDag
        };

        /// <summary>
        /// Validates if a given type is a valid template type
        /// </summary>
        public static bool IsValidType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return false;

            return DefaultTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
        }
    }
}