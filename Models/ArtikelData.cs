using System;

namespace scheidingsdesk_document_generator.Models
{
    /// <summary>
    /// Data model for artikel templates with user/dossier customizations
    /// </summary>
    public class ArtikelData
    {
        // Template fields
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string ArtikelCode { get; set; } = string.Empty;
        public string ArtikelTitel { get; set; } = string.Empty;
        public string ArtikelTekst { get; set; } = string.Empty;
        public int Volgorde { get; set; }
        public bool IsVerplicht { get; set; }
        public bool IsConditioneel { get; set; }
        public string? ConditieVeld { get; set; }
        public string? Categorie { get; set; }
        public string? HelpTekst { get; set; }
        public int Versie { get; set; }
        public bool IsActief { get; set; }

        // Gebruiker aanpassingen (nullable)
        public string? GebruikerTitel { get; set; }
        public string? GebruikerTekst { get; set; }
        public bool? GebruikerActief { get; set; }

        // Dossier overrides (nullable)
        public string? DossierTekst { get; set; }
        public bool IsUitgesloten { get; set; }

        /// <summary>
        /// Gets the effective title with priority: gebruiker > systeem
        /// (dossier heeft geen titel override)
        /// </summary>
        public string EffectieveTitel =>
            !string.IsNullOrEmpty(GebruikerTitel) ? GebruikerTitel : ArtikelTitel;

        /// <summary>
        /// Gets the effective text with priority: dossier > gebruiker > systeem
        /// </summary>
        public string EffectieveTekst
        {
            get
            {
                if (!string.IsNullOrEmpty(DossierTekst))
                    return DossierTekst;
                if (!string.IsNullOrEmpty(GebruikerTekst))
                    return GebruikerTekst;
                return ArtikelTekst;
            }
        }

        /// <summary>
        /// Gets the source of the effective text
        /// </summary>
        public string Bron
        {
            get
            {
                if (!string.IsNullOrEmpty(DossierTekst))
                    return "dossier";
                if (!string.IsNullOrEmpty(GebruikerTekst))
                    return "gebruiker";
                return "systeem";
            }
        }

        /// <summary>
        /// Whether this artikel has any customizations
        /// </summary>
        public bool IsAangepast =>
            !string.IsNullOrEmpty(GebruikerTitel) ||
            !string.IsNullOrEmpty(GebruikerTekst) ||
            !string.IsNullOrEmpty(DossierTekst);
    }
}
