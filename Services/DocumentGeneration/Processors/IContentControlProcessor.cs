using DocumentFormat.OpenXml.Wordprocessing;
using scheidingsdesk_document_generator.Models;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Service interface for processing content controls
    /// </summary>
    public interface IContentControlProcessor
    {
        /// <summary>
        /// Removes all content controls from document while preserving content
        /// </summary>
        /// <param name="document">Document to process</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        void RemoveContentControls(Document document, string correlationId);

        /// <summary>
        /// Processes table and list placeholders in document body
        /// </summary>
        /// <param name="body">Document body to process</param>
        /// <param name="data">Dossier data for table generation</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        void ProcessTablePlaceholders(Body body, DossierData data, string correlationId);
    }
}