using System;

namespace scheidingsdesk_document_generator.Models
{
    public class ChildData
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

        /// <summary>
        /// Parent-child relationships for this child
        /// </summary>
        public List<ParentChildRelation> ParentRelations { get; set; } = new List<ParentChildRelation>();

        /// <summary>
        /// Gets the full name of the child, combining all name parts
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

        /// <summary>
        /// Calculates the age based on birth date
        /// </summary>
        public int? Leeftijd
        {
            get
            {
                if (!GeboorteDatum.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - GeboorteDatum.Value.Year;
                if (GeboorteDatum.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
    }

    public class ParentChildRelation
    {
        public int OuderId { get; set; }
        public string? OuderNaam { get; set; }
        public int RelatieTypeId { get; set; }
        public string? RelatieType { get; set; }
    }
}