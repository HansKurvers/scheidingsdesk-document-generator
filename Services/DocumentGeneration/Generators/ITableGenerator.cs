using DocumentFormat.OpenXml;
using scheidingsdesk_document_generator.Models;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Interface for table and list generators using Strategy Pattern
    /// Allows for easy addition of new table types without modifying existing code
    /// </summary>
    public interface ITableGenerator
    {
        /// <summary>
        /// The placeholder tag this generator handles (e.g., "[[TABEL_OMGANG]]")
        /// </summary>
        string PlaceholderTag { get; }

        /// <summary>
        /// Generates table or list elements based on dossier data
        /// </summary>
        /// <param name="data">Dossier data containing information for table generation</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>List of OpenXML elements (can include tables, headings, paragraphs)</returns>
        List<OpenXmlElement> Generate(DossierData data, string correlationId);
    }
}