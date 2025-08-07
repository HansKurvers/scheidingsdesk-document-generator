using System;

namespace scheidingsdesk_document_generator.Models
{
    public class PersonData
    {
        public int Id { get; set; }
        public string? Voorletters { get; set; }
        public string? Voornamen { get; set; }
        public string? Roepnaam { get; set; }
        public string? Geslacht { get; set; }
        public string? Tussenvoegsel { get; set; }
        public string Achternaam { get; set; } = string.Empty;
        public string? Adres { get; set; }
        public string? Postcode { get; set; }
        public string? Plaats { get; set; }
        public string? GeboortePlaats { get; set; }
        public DateTime? GeboorteDatum { get; set; }
        public string? Nationaliteit1 { get; set; }
        public string? Nationaliteit2 { get; set; }
        public string? Telefoon { get; set; }
        public string? Email { get; set; }
        public string? Beroep { get; set; }
        public int? RolId { get; set; }
        public string? RolNaam { get; set; }

        /// <summary>
        /// Gets the full name of the person, combining all name parts
        /// </summary>
        public string VolledigeNaam
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(Voornamen)) parts.Add(Voornamen);
                if (!string.IsNullOrEmpty(Tussenvoegsel)) parts.Add(Tussenvoegsel);
                if (!string.IsNullOrEmpty(Achternaam)) parts.Add(Achternaam);
                return string.Join(" ", parts);
            }
        }

        /// <summary>
        /// Gets the calling name or falls back to first name
        /// </summary>
        public string Naam => !string.IsNullOrEmpty(Roepnaam) ? Roepnaam : Voornamen?.Split(' ').FirstOrDefault() ?? Achternaam;
    }
}