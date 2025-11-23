using System.IO;
using System.Threading.Tasks;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration
{
    /// <summary>
    /// Main service interface for document generation orchestration
    /// </summary>
    public interface IDocumentGenerationService
    {
        /// <summary>
        /// Generates a complete document based on dossier data
        /// </summary>
        /// <param name="dossierId">The dossier ID to generate document for</param>
        /// <param name="templateType">Template type ('default' or 'v2'). Defaults to 'default' if null/empty.</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Stream containing the generated document</returns>
        Task<Stream> GenerateDocumentAsync(int dossierId, string? templateType, string correlationId);
    }
}