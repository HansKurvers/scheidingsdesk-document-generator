using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Processors;
using System;
using System.IO;
using System.Threading.Tasks;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration
{
    /// <summary>
    /// Main orchestrator service for document generation
    /// Coordinates all steps: template download, data retrieval, placeholder processing, and table generation
    /// </summary>
    public class DocumentGenerationService : IDocumentGenerationService
    {
        private readonly ILogger<DocumentGenerationService> _logger;
        private readonly DatabaseService _databaseService;
        private readonly ITemplateProvider _templateProvider;
        private readonly GrammarRulesBuilder _grammarRulesBuilder;
        private readonly IPlaceholderProcessor _placeholderProcessor;
        private readonly IContentControlProcessor _contentControlProcessor;
        private readonly IConditionalSectionProcessor _conditionalSectionProcessor;

        public DocumentGenerationService(
            ILogger<DocumentGenerationService> logger,
            DatabaseService databaseService,
            ITemplateProvider templateProvider,
            GrammarRulesBuilder grammarRulesBuilder,
            IPlaceholderProcessor placeholderProcessor,
            IContentControlProcessor contentControlProcessor,
            IConditionalSectionProcessor conditionalSectionProcessor)
        {
            _logger = logger;
            _databaseService = databaseService;
            _templateProvider = templateProvider;
            _grammarRulesBuilder = grammarRulesBuilder;
            _placeholderProcessor = placeholderProcessor;
            _contentControlProcessor = contentControlProcessor;
            _conditionalSectionProcessor = conditionalSectionProcessor;
        }

        /// <summary>
        /// Generates a complete document based on dossier data
        /// Simple orchestration: download template, get data, process, return
        /// </summary>
        public async Task<Stream> GenerateDocumentAsync(int dossierId, string? templateType, string correlationId)
        {
            // Default to 'default' template type if not specified
            templateType = string.IsNullOrWhiteSpace(templateType) ? "default" : templateType;

            _logger.LogInformation($"[{correlationId}] Starting document generation for dossier {dossierId} with template type '{templateType}'");

            // Step 1: Download template
            _logger.LogInformation($"[{correlationId}] Step 1: Downloading template (type: {templateType})");
            var templateBytes = await _templateProvider.GetTemplateByTypeAsync(templateType);

            // Step 2: Get dossier data from database
            _logger.LogInformation($"[{correlationId}] Step 2: Retrieving dossier data");
            var dossierData = await _databaseService.GetDossierDataAsync(dossierId);

            if (dossierData == null)
            {
                throw new InvalidOperationException($"No data found for DossierId: {dossierId}");
            }

            // Step 2b: Get artikelen from database (for templates with [[ARTIKELEN]] placeholder)
            _logger.LogInformation($"[{correlationId}] Step 2b: Retrieving artikelen for document type '{templateType}'");
            var documentType = templateType == "convenant" ? "convenant" : "ouderschapsplan";
            var artikelen = await _databaseService.GetArtikelenVoorDossierAsync(
                dossierId,
                dossierData.GebruikerId,
                documentType);
            dossierData.Artikelen = artikelen;
            _logger.LogInformation($"[{correlationId}] Retrieved {artikelen.Count} artikelen");

            // Step 3: Build grammar rules
            _logger.LogInformation($"[{correlationId}] Step 3: Building grammar rules");
            var grammarRules = _grammarRulesBuilder.BuildRules(dossierData.Kinderen, correlationId);

            // Step 4: Build all placeholder replacements
            _logger.LogInformation($"[{correlationId}] Step 4: Building placeholder replacements");
            var replacements = _placeholderProcessor.BuildReplacements(dossierData, grammarRules);
            _logger.LogInformation($"[{correlationId}] Built {replacements.Count} placeholder replacements");

            // Log alimentatie-related placeholders for debugging
            if (replacements.ContainsKey("NettoBesteedbaarGezinsinkomen"))
                _logger.LogInformation($"[{correlationId}] NettoBesteedbaarGezinsinkomen = '{replacements["NettoBesteedbaarGezinsinkomen"]}'");
            if (replacements.ContainsKey("Partij1EigenAandeel"))
                _logger.LogInformation($"[{correlationId}] Partij1EigenAandeel = '{replacements["Partij1EigenAandeel"]}'");
            if (replacements.ContainsKey("KinderenAlimentatie"))
                _logger.LogInformation($"[{correlationId}] KinderenAlimentatie = '{replacements["KinderenAlimentatie"].Substring(0, Math.Min(100, replacements["KinderenAlimentatie"].Length))}...'");

            // Step 5: Process the document
            _logger.LogInformation($"[{correlationId}] Step 5: Processing document");
            var documentStream = await ProcessDocumentAsync(templateBytes, dossierData, replacements, correlationId);

            _logger.LogInformation($"[{correlationId}] Document generation completed successfully");
            return documentStream;
        }

        /// <summary>
        /// Processes the document: opens it, replaces placeholders, generates tables, removes content controls
        /// </summary>
        private async Task<MemoryStream> ProcessDocumentAsync(
            byte[] templateBytes,
            Models.DossierData dossierData,
            System.Collections.Generic.Dictionary<string, string> replacements,
            string correlationId)
        {
            return await Task.Run(() =>
            {
                // Create output stream from template
                var documentStream = new MemoryStream();
                documentStream.Write(templateBytes, 0, templateBytes.Length);
                documentStream.Position = 0;

                // Open and process the document
                using (WordprocessingDocument doc = WordprocessingDocument.Open(documentStream, true))
                {
                    _logger.LogInformation($"[{correlationId}] Document opened successfully");

                    var mainPart = doc.MainDocumentPart;
                    if (mainPart == null)
                    {
                        throw new InvalidOperationException("MainDocumentPart is null");
                    }

                    var body = mainPart.Document.Body;
                    if (body == null)
                    {
                        throw new InvalidOperationException("Document body is null");
                    }

                    // Step 1: Process placeholders in body
                    _logger.LogInformation($"[{correlationId}] Step 1: Processing placeholders");
                    _placeholderProcessor.ProcessDocument(body, replacements, correlationId);

                    // Process headers and footers
                    _placeholderProcessor.ProcessHeadersAndFooters(mainPart, replacements, correlationId);

                    // Step 2: Process conditional sections
                    _logger.LogInformation($"[{correlationId}] Step 2: Processing conditional sections");
                    _conditionalSectionProcessor.ProcessConditionalSections(doc, replacements, correlationId);

                    // Step 3: Process article numbering
                    _logger.LogInformation($"[{correlationId}] Step 3: Processing article numbering");
                    ArticleNumberingHelper.ProcessArticlePlaceholders(doc, _logger, correlationId);

                    // Step 4: Process table placeholders (generates dynamic tables and artikelen)
                    _logger.LogInformation($"[{correlationId}] Step 4: Processing table placeholders");
                    _contentControlProcessor.ProcessTablePlaceholders(body, dossierData, replacements, correlationId);

                    // Step 5: Remove content controls
                    _logger.LogInformation($"[{correlationId}] Step 5: Removing content controls");
                    _contentControlProcessor.RemoveContentControls(mainPart.Document, correlationId);

                    // Save changes
                    mainPart.Document.Save();
                    _logger.LogInformation($"[{correlationId}] Document saved successfully");
                }

                documentStream.Position = 0;
                return documentStream;
            });
        }
    }
}