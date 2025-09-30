using DocumentFormat.OpenXml.Wordprocessing;
using scheidingsdesk_document_generator.Models;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Service interface for processing placeholders in documents
    /// </summary>
    public interface IPlaceholderProcessor
    {
        /// <summary>
        /// Builds all placeholder replacements from dossier data
        /// </summary>
        /// <param name="data">Dossier data</param>
        /// <param name="grammarRules">Grammar rules to include</param>
        /// <returns>Dictionary of placeholder keys and their replacement values</returns>
        Dictionary<string, string> BuildReplacements(DossierData data, Dictionary<string, string> grammarRules);

        /// <summary>
        /// Processes a document body and replaces all placeholders
        /// </summary>
        /// <param name="body">Document body to process</param>
        /// <param name="replacements">Dictionary of replacements</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        void ProcessDocument(Body body, Dictionary<string, string> replacements, string correlationId);

        /// <summary>
        /// Processes headers and footers in the document
        /// </summary>
        /// <param name="mainPart">Main document part containing headers/footers</param>
        /// <param name="replacements">Dictionary of replacements</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        void ProcessHeadersAndFooters(DocumentFormat.OpenXml.Packaging.MainDocumentPart mainPart, Dictionary<string, string> replacements, string correlationId);
    }
}