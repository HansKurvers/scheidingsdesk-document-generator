using scheidingsdesk_document_generator.Models;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.Artikel
{
    /// <summary>
    /// Service interface voor het verwerken van artikelen uit de bibliotheek
    /// </summary>
    public interface IArtikelService
    {
        /// <summary>
        /// Filtert conditionele artikelen op basis van beschikbare data
        /// </summary>
        /// <param name="artikelen">Lijst van artikelen</param>
        /// <param name="replacements">Beschikbare placeholder waarden</param>
        /// <returns>Gefilterde lijst met alleen toepasselijke artikelen</returns>
        List<ArtikelData> FilterConditioneleArtikelen(
            List<ArtikelData> artikelen,
            Dictionary<string, string> replacements);

        /// <summary>
        /// Vervangt placeholders in artikel tekst
        /// </summary>
        /// <param name="tekst">De artikel tekst met placeholders</param>
        /// <param name="replacements">Placeholder waarden</param>
        /// <returns>Tekst met vervangen placeholders</returns>
        string VervangPlaceholders(string tekst, Dictionary<string, string> replacements);

        /// <summary>
        /// Verwerkt conditionele blokken binnen artikel tekst
        /// [[IF:VeldNaam]]tekst[[ENDIF:VeldNaam]]
        /// </summary>
        /// <param name="tekst">De artikel tekst met conditionele blokken</param>
        /// <param name="replacements">Placeholder waarden</param>
        /// <returns>Tekst met verwerkte conditionele blokken</returns>
        string VerwerkConditioneleBlokken(string tekst, Dictionary<string, string> replacements);

        /// <summary>
        /// Past alle transformaties toe op een artikel tekst:
        /// 1. Verwerkt conditionele blokken
        /// 2. Vervangt placeholders
        /// </summary>
        /// <param name="artikel">Het artikel om te verwerken</param>
        /// <param name="replacements">Placeholder waarden</param>
        /// <returns>De volledig verwerkte tekst</returns>
        string VerwerkArtikelTekst(ArtikelData artikel, Dictionary<string, string> replacements);
    }
}
