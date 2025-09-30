using System;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper class for formatting data consistently throughout the application
    /// Handles dates, names, addresses, and type conversions
    /// </summary>
    public static class DataFormatter
    {
        /// <summary>
        /// Formats a date with the specified format string
        /// </summary>
        /// <param name="date">Date to format</param>
        /// <param name="format">Format string (default: dd-MM-yyyy)</param>
        /// <returns>Formatted date string or empty string if null</returns>
        public static string FormatDate(DateTime? date, string format = "dd-MM-yyyy")
        {
            return date?.ToString(format) ?? string.Empty;
        }

        /// <summary>
        /// Formats a date in Dutch long format (dd MMMM yyyy)
        /// Example: 15 januari 2024
        /// </summary>
        /// <param name="date">Date to format</param>
        /// <returns>Formatted date string in Dutch</returns>
        public static string FormatDateDutchLong(DateTime? date)
        {
            if (!date.HasValue)
                return string.Empty;

            return date.Value.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("nl-NL"));
        }

        /// <summary>
        /// Formats a full name from components
        /// Example: "Jan", "de", "Vries" -> "Jan de Vries"
        /// </summary>
        /// <param name="voornamen">First name(s)</param>
        /// <param name="tussenvoegsel">Infix (e.g., "de", "van der")</param>
        /// <param name="achternaam">Last name</param>
        /// <returns>Full formatted name</returns>
        public static string FormatFullName(string? voornamen, string? tussenvoegsel, string achternaam)
        {
            var parts = new[] { voornamen, tussenvoegsel, achternaam }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Formats a complete address
        /// Example: "Kerkstraat 1", "1234 AB", "Amsterdam" -> "Kerkstraat 1, 1234 AB Amsterdam"
        /// </summary>
        /// <param name="adres">Street and house number</param>
        /// <param name="postcode">Postal code</param>
        /// <param name="plaats">City</param>
        /// <returns>Formatted address string</returns>
        public static string FormatAddress(string? adres, string? postcode, string? plaats)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(adres))
                parts.Add(adres.Trim());

            var postcodeAndCity = new[] { postcode, plaats }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim());

            var postcodeCity = string.Join(" ", postcodeAndCity);
            if (!string.IsNullOrWhiteSpace(postcodeCity))
                parts.Add(postcodeCity);

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Safely converts any value to a string representation
        /// Handles special types like DateTime, bool, and nullable types
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>String representation of the value</returns>
        public static string ConvertToString(object? value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            // Handle DateTime
            if (value is DateTime dateTime)
                return dateTime.ToString("dd-MM-yyyy");

            // Handle bool
            if (value is bool boolValue)
                return boolValue ? "Ja" : "Nee";

            // Handle nullable DateTime
            var type = value.GetType();
            if (type == typeof(DateTime?))
            {
                var nullableDateTime = (DateTime?)value;
                return nullableDateTime.HasValue ? nullableDateTime.Value.ToString("dd-MM-yyyy") : string.Empty;
            }

            // Handle nullable bool
            if (type == typeof(bool?))
            {
                var nullableBool = (bool?)value;
                return nullableBool.HasValue ? (nullableBool.Value ? "Ja" : "Nee") : string.Empty;
            }

            // Handle nullable int
            if (type == typeof(int?))
            {
                var nullableInt = (int?)value;
                return nullableInt.HasValue ? nullableInt.Value.ToString() : string.Empty;
            }

            // Handle nullable decimal
            if (type == typeof(decimal?))
            {
                var nullableDecimal = (decimal?)value;
                return nullableDecimal.HasValue ? nullableDecimal.Value.ToString("F2") : string.Empty;
            }

            // Default conversion for all other types
            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Formats a decimal as currency (Euro)
        /// </summary>
        /// <param name="amount">Amount to format</param>
        /// <returns>Formatted currency string (e.g., "€ 1.234,56")</returns>
        public static string FormatCurrency(decimal? amount)
        {
            if (!amount.HasValue)
                return string.Empty;

            return amount.Value.ToString("C2", new System.Globalization.CultureInfo("nl-NL"));
        }

        /// <summary>
        /// Formats a phone number (basic formatting)
        /// </summary>
        /// <param name="phoneNumber">Phone number to format</param>
        /// <returns>Formatted phone number</returns>
        public static string FormatPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Remove all non-digit characters
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Basic Dutch phone number formatting
            if (digits.Length == 10 && digits.StartsWith("0"))
            {
                // Format as 06-12345678 or 020-1234567
                if (digits.StartsWith("06"))
                    return $"{digits.Substring(0, 2)}-{digits.Substring(2)}";
                else
                    return $"{digits.Substring(0, 3)}-{digits.Substring(3)}";
            }

            // Return original if format not recognized
            return phoneNumber;
        }

        /// <summary>
        /// Formats initials from first names
        /// Example: "Jan Peter" -> "J.P."
        /// </summary>
        /// <param name="voornamen">First names</param>
        /// <returns>Formatted initials</returns>
        public static string FormatInitials(string? voornamen)
        {
            if (string.IsNullOrWhiteSpace(voornamen))
                return string.Empty;

            var names = voornamen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = names.Select(n => n[0].ToString().ToUpper() + ".");

            return string.Join("", initials);
        }
    }
}