using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services;

namespace Scheidingsdesk
{
    public class OuderschapsplanFunction
    {
        private readonly ILogger<OuderschapsplanFunction> _logger;
        private readonly DatabaseService _databaseService;

        public OuderschapsplanFunction(ILogger<OuderschapsplanFunction> logger, DatabaseService databaseService)
        {
            _logger = logger;
            _databaseService = databaseService;
        }

        [Function("Ouderschapsplan")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ouderschapsplan")] HttpRequest req)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString();
            
            _logger.LogInformation($"[{correlationId}] Ouderschapsplan generation request started");

            try
            {
                // Read and validate request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "Request body is required. Please provide a JSON object with DossierId.",
                        correlationId = correlationId
                    });
                }

                OuderschapsplanRequest? requestData;
                try
                {
                    requestData = JsonConvert.DeserializeObject<OuderschapsplanRequest>(requestBody);
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"[{correlationId}] Invalid JSON in request body: {ex.Message}");
                    return new BadRequestObjectResult(new
                    {
                        error = "Invalid JSON format in request body.",
                        details = ex.Message,
                        correlationId = correlationId
                    });
                }

                if (requestData?.DossierId == null || requestData.DossierId <= 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        error = "Valid DossierId is required and must be greater than 0.",
                        correlationId = correlationId
                    });
                }

                _logger.LogInformation($"[{correlationId}] Processing ouderschapsplan for DossierId: {requestData.DossierId}");

                // Load template file - try multiple locations
                string templateFileName = "Ouderschapsplan NIEUW.docx";
                string[] possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Ouderschapsplan", templateFileName),
                    Path.Combine(Directory.GetCurrentDirectory(), templateFileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ouderschapsplan", templateFileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templateFileName),
                    Path.Combine("/home/site/wwwroot", "Ouderschapsplan", templateFileName),
                    Path.Combine("/home/site/wwwroot", templateFileName)
                };

                string? templatePath = null;
                foreach (var path in possiblePaths)
                {
                    _logger.LogInformation($"[{correlationId}] Checking for template at: {path}");
                    if (File.Exists(path))
                    {
                        templatePath = path;
                        break;
                    }
                }
                
                if (templatePath == null)
                {
                    _logger.LogError($"[{correlationId}] Template file not found in any of the expected locations. Current directory: {Directory.GetCurrentDirectory()}, Base directory: {AppDomain.CurrentDomain.BaseDirectory}");
                    return new ObjectResult(new
                    {
                        error = "Template file not found. Please ensure the template exists.",
                        correlationId = correlationId,
                        currentDirectory = Directory.GetCurrentDirectory(),
                        baseDirectory = AppDomain.CurrentDomain.BaseDirectory
                    })
                    {
                        StatusCode = 500
                    };
                }

                _logger.LogInformation($"[{correlationId}] Template found at: {templatePath}");

                // Get data from database
                var dossierData = await GetDossierDataAsync(requestData.DossierId, correlationId);
                
                if (dossierData == null)
                {
                    return new BadRequestObjectResult(new
                    {
                        error = $"No data found for DossierId: {requestData.DossierId}",
                        correlationId = correlationId
                    });
                }

                // Generate document
                var documentStream = await GenerateDocumentAsync(templatePath, dossierData, correlationId);
                
                if (documentStream == null)
                {
                    return new ObjectResult(new
                    {
                        error = "Failed to generate document.",
                        correlationId = correlationId
                    })
                    {
                        StatusCode = 500
                    };
                }

                stopwatch.Stop();
                _logger.LogInformation($"[{correlationId}] Ouderschapsplan generated successfully for DossierId: {requestData.DossierId} in {stopwatch.ElapsedMilliseconds}ms");

                // Return the generated document
                documentStream.Position = 0;
                return new FileStreamResult(documentStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    FileDownloadName = $"Ouderschapsplan_Dossier_{requestData.DossierId}_{DateTime.Now:yyyyMMdd}.docx"
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"[{correlationId}] Business logic error: {ex.Message}");
                return new BadRequestObjectResult(new
                {
                    error = ex.Message,
                    correlationId = correlationId
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"[{correlationId}] Invalid argument error: {ex.Message}");
                return new BadRequestObjectResult(new
                {
                    error = ex.Message,
                    correlationId = correlationId
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError($"[{correlationId}] File not found error: {ex.Message}");
                return new ObjectResult(new
                {
                    error = "Required file not found.",
                    details = ex.Message,
                    correlationId = correlationId
                })
                {
                    StatusCode = 500
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"[{correlationId}] Access denied error: {ex.Message}");
                return new ObjectResult(new
                {
                    error = "Access denied to required resources.",
                    details = ex.Message,
                    correlationId = correlationId
                })
                {
                    StatusCode = 500
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Unexpected error generating ouderschapsplan");
                return new ObjectResult(new
                {
                    error = "An unexpected error occurred while generating the ouderschapsplan.",
                    details = ex.Message,
                    correlationId = correlationId
                })
                {
                    StatusCode = 500
                };
            }
        }

        /// <summary>
        /// Retrieves all data needed for the ouderschapsplan from the database
        /// </summary>
        /// <param name="dossierId">The dossier ID to retrieve data for</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Complete dossier data or null if not found</returns>
        private async Task<DossierData?> GetDossierDataAsync(int dossierId, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Retrieving dossier data for ID: {dossierId}");
            
            try
            {
                // Placeholder: This will be implemented to call DatabaseService
                var dossierData = await _databaseService.GetDossierDataAsync(dossierId);
                
                if (dossierData != null)
                {
                    _logger.LogInformation($"[{correlationId}] Successfully retrieved data for dossier {dossierId}");
                }
                else
                {
                    _logger.LogWarning($"[{correlationId}] No data found for dossier {dossierId}");
                }
                
                return dossierData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Error retrieving dossier data for ID: {dossierId}");
                throw;
            }
        }

        /// <summary>
        /// Generates the ouderschapsplan document by filling the template with data
        /// </summary>
        /// <param name="templatePath">Path to the template file</param>
        /// <param name="data">Data to fill in the template</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Generated document as MemoryStream</returns>
        private async Task<MemoryStream?> GenerateDocumentAsync(string templatePath, DossierData data, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Starting document generation from template: {templatePath}");
            
            try
            {
                // Read template file
                byte[] templateBytes = await File.ReadAllBytesAsync(templatePath);
                var templateStream = new MemoryStream(templateBytes);
                
                // Create a copy for processing
                var documentStream = new MemoryStream();
                
                // Create grammar rules based on number of children
                var grammarRules = CreateGrammarRules(data.Kinderen.Count, data.Kinderen, correlationId);
                
                // Process the document
                using (WordprocessingDocument sourceDoc = WordprocessingDocument.Open(templateStream, false))
                {
                    _logger.LogInformation($"[{correlationId}] Source document opened successfully.");
                    
                    // Create a NEW document in the output stream
                    using (WordprocessingDocument outputDoc = WordprocessingDocument.Create(documentStream, sourceDoc.DocumentType))
                    {
                        _logger.LogInformation($"[{correlationId}] Creating new document for output.");
                        
                        // Copy all parts from source to output
                        foreach (var part in sourceDoc.Parts)
                        {
                            outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                        }
                        
                        var mainPart = outputDoc.MainDocumentPart;
                        if (mainPart != null)
                        {
                            // Process content controls with data replacement
                            ReplaceContentControlsWithData(mainPart.Document, data, grammarRules, correlationId);
                            
                            // Remove content controls after populating them
                            RemoveContentControls(mainPart.Document, correlationId);
                            
                            mainPart.Document.Save();
                            _logger.LogInformation($"[{correlationId}] Document generation completed successfully.");
                        }
                        else
                        {
                            _logger.LogError($"[{correlationId}] MainDocumentPart is null!");
                            return null;
                        }
                    }
                }
                
                documentStream.Position = 0;
                return documentStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Error during document generation");
                throw;
            }
        }

        /// <summary>
        /// Creates grammar rules based on the number of children and their genders
        /// </summary>
        /// <param name="childCount">Number of children</param>
        /// <param name="children">List of child data for gender-specific rules</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Dictionary of grammar rules</returns>
        private Dictionary<string, string> CreateGrammarRules(int childCount, List<ChildData> children, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Creating grammar rules for {childCount} children");
            
            var rules = new Dictionary<string, string>();
            bool isPlural = childCount > 1;
            
            // Basic singular/plural rules
            rules["meervoud onze kinderen"] = isPlural ? "onze kinderen" : "ons kind";
            rules["meervoud heeft/hebben"] = isPlural ? "hebben" : "heeft";
            rules["meervoud is/zijn"] = isPlural ? "zijn" : "is";
            rules["meervoud verblijft/verblijven"] = isPlural ? "verblijven" : "verblijft";
            rules["meervoud kan/kunnen"] = isPlural ? "kunnen" : "kan";
            rules["meervoud zal/zullen"] = isPlural ? "zullen" : "zal";
            rules["meervoud moet/moeten"] = isPlural ? "moeten" : "moet";
            rules["meervoud wordt/worden"] = isPlural ? "worden" : "wordt";
            rules["meervoud blijft/blijven"] = isPlural ? "blijven" : "blijft";
            rules["meervoud gaat/gaan"] = isPlural ? "gaan" : "gaat";
            rules["meervoud komt/komen"] = isPlural ? "komen" : "komt";
            
            // Gender and count specific pronouns
            if (isPlural)
            {
                rules["meervoud hem/haar/hen"] = "hen";
                rules["meervoud hij/zij/ze"] = "ze";
            }
            else if (children.Count == 1)
            {
                var child = children.First();
                bool isMale = string.Equals(child.Geslacht, "M", StringComparison.OrdinalIgnoreCase) || 
                             string.Equals(child.Geslacht, "Man", StringComparison.OrdinalIgnoreCase);
                bool isFemale = string.Equals(child.Geslacht, "V", StringComparison.OrdinalIgnoreCase) || 
                               string.Equals(child.Geslacht, "Vrouw", StringComparison.OrdinalIgnoreCase);
                
                if (isMale)
                {
                    rules["meervoud hem/haar/hen"] = "hem";
                    rules["meervoud hij/zij/ze"] = "hij";
                }
                else if (isFemale)
                {
                    rules["meervoud hem/haar/hen"] = "haar";
                    rules["meervoud hij/zij/ze"] = "zij";
                }
                else
                {
                    // Default to neutral when gender is unknown
                    rules["meervoud hem/haar/hen"] = "hem/haar";
                    rules["meervoud hij/zij/ze"] = "hij/zij";
                }
            }
            else
            {
                // No children case
                rules["meervoud hem/haar/hen"] = "hem/haar";
                rules["meervoud hij/zij/ze"] = "hij/zij";
            }
            
            _logger.LogInformation($"[{correlationId}] Created {rules.Count} grammar rules");
            return rules;
        }

        /// <summary>
        /// Replaces content controls with data from the database
        /// </summary>
        /// <param name="document">The document to process</param>
        /// <param name="data">The dossier data</param>
        /// <param name="grammarRules">Grammar rules for text replacement</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        private void ReplaceContentControlsWithData(Document document, DossierData data, Dictionary<string, string> grammarRules, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Starting content control replacement");
            
            var sdtElements = document.Descendants<SdtElement>().ToList();
            _logger.LogInformation($"[{correlationId}] Found {sdtElements.Count} content controls to process");
            
            foreach (var sdt in sdtElements)
            {
                try
                {
                    var contentText = GetSdtContentText(sdt);
                    
                    if (string.IsNullOrWhiteSpace(contentText))
                        continue;
                    
                    _logger.LogDebug($"[{correlationId}] Processing content control: '{contentText}'");
                    
                    // Apply grammar rules first
                    var processedText = ApplyGrammarRules(contentText, grammarRules);
                    
                    // Apply data replacements
                    processedText = ApplyDataReplacements(processedText, data, correlationId);
                    
                    // Handle checkbox symbols
                    processedText = ReplaceCheckboxSymbols(processedText, data, correlationId);
                    
                    // Update the content control if text was changed
                    if (processedText != contentText)
                    {
                        SetSdtContentText(sdt, processedText);
                        _logger.LogDebug($"[{correlationId}] Updated content control: '{contentText}' -> '{processedText}'");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[{correlationId}] Error processing content control: {ex.Message}");
                }
            }
            
            _logger.LogInformation($"[{correlationId}] Content control replacement completed");
        }

        /// <summary>
        /// Applies grammar rules to the text
        /// </summary>
        /// <param name="text">Original text</param>
        /// <param name="grammarRules">Grammar rules to apply</param>
        /// <returns>Text with grammar rules applied</returns>
        private string ApplyGrammarRules(string text, Dictionary<string, string> grammarRules)
        {
            foreach (var rule in grammarRules)
            {
                text = text.Replace(rule.Key, rule.Value);
            }
            return text;
        }

        /// <summary>
        /// Applies data replacements to the text
        /// </summary>
        /// <param name="text">Text to process</param>
        /// <param name="data">Dossier data</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Text with data replacements applied</returns>
        private string ApplyDataReplacements(string text, DossierData data, string correlationId)
        {
            // Replace party information
            if (data.Partij1 != null)
            {
                text = text.Replace("{{Partij1.VolledigeNaam}}", data.Partij1.VolledigeNaam ?? string.Empty);
                text = text.Replace("{{Partij1.Naam}}", data.Partij1.Naam ?? string.Empty);
                text = text.Replace("{{Partij1.Adres}}", data.Partij1.Adres ?? string.Empty);
                text = text.Replace("{{Partij1.Postcode}}", data.Partij1.Postcode ?? string.Empty);
                text = text.Replace("{{Partij1.Plaats}}", data.Partij1.Plaats ?? string.Empty);
                text = text.Replace("{{Partij1.GeboorteDatum}}", data.Partij1.GeboorteDatum?.ToString("dd-MM-yyyy") ?? string.Empty);
                text = text.Replace("{{Partij1.Telefoon}}", data.Partij1.Telefoon ?? string.Empty);
                text = text.Replace("{{Partij1.Email}}", data.Partij1.Email ?? string.Empty);
            }
            
            if (data.Partij2 != null)
            {
                text = text.Replace("{{Partij2.VolledigeNaam}}", data.Partij2.VolledigeNaam ?? string.Empty);
                text = text.Replace("{{Partij2.Naam}}", data.Partij2.Naam ?? string.Empty);
                text = text.Replace("{{Partij2.Adres}}", data.Partij2.Adres ?? string.Empty);
                text = text.Replace("{{Partij2.Postcode}}", data.Partij2.Postcode ?? string.Empty);
                text = text.Replace("{{Partij2.Plaats}}", data.Partij2.Plaats ?? string.Empty);
                text = text.Replace("{{Partij2.GeboorteDatum}}", data.Partij2.GeboorteDatum?.ToString("dd-MM-yyyy") ?? string.Empty);
                text = text.Replace("{{Partij2.Telefoon}}", data.Partij2.Telefoon ?? string.Empty);
                text = text.Replace("{{Partij2.Email}}", data.Partij2.Email ?? string.Empty);
            }
            
            // Replace dossier information
            text = text.Replace("{{Dossier.DossierNummer}}", data.DossierNummer ?? string.Empty);
            text = text.Replace("{{Dossier.AangemaaktOp}}", data.AangemaaktOp.ToString("dd-MM-yyyy"));
            
            // Replace child information
            for (int i = 0; i < data.Kinderen.Count; i++)
            {
                var child = data.Kinderen[i];
                text = text.Replace($"{{{{Kind{i + 1}.VolledigeNaam}}}}", child.VolledigeNaam ?? string.Empty);
                text = text.Replace($"{{{{Kind{i + 1}.Naam}}}}", child.Naam ?? string.Empty);
                text = text.Replace($"{{{{Kind{i + 1}.GeboorteDatum}}}}", child.GeboorteDatum?.ToString("dd-MM-yyyy") ?? string.Empty);
                text = text.Replace($"{{{{Kind{i + 1}.Leeftijd}}}}", child.Leeftijd?.ToString() ?? string.Empty);
                text = text.Replace($"{{{{Kind{i + 1}.Geslacht}}}}", child.Geslacht ?? string.Empty);
            }
            
            return text;
        }

        /// <summary>
        /// Replaces checkbox symbols based on data conditions
        /// </summary>
        /// <param name="text">Text to process</param>
        /// <param name="data">Dossier data</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Text with checkbox symbols replaced</returns>
        private string ReplaceCheckboxSymbols(string text, DossierData data, string correlationId)
        {
            // This is a placeholder implementation - specific checkbox logic would be implemented based on
            // the actual template requirements and business rules
            
            // Example: Replace unchecked boxes with checked boxes based on data conditions
            // text = text.Replace("☐", "☑"); // This would be conditional based on actual data
            
            return text;
        }

        /// <summary>
        /// Gets the text content from a content control
        /// </summary>
        /// <param name="sdt">The content control element</param>
        /// <returns>The text content</returns>
        private string GetSdtContentText(SdtElement sdt)
        {
            var contentElements = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
            if (contentElements == null) return string.Empty;
            
            return contentElements.Descendants<Text>().Aggregate(string.Empty, (current, text) => current + (text.Text ?? string.Empty));
        }

        /// <summary>
        /// Sets the text content of a content control
        /// </summary>
        /// <param name="sdt">The content control element</param>
        /// <param name="newText">The new text content</param>
        private void SetSdtContentText(SdtElement sdt, string newText)
        {
            var contentElement = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
            if (contentElement == null) return;
            
            // Clear existing content
            contentElement.RemoveAllChildren();
            
            // Ensure newText is never null
            newText = newText ?? string.Empty;
            
            // Add new paragraph with the text
            var paragraph = new Paragraph();
            var run = new Run();
            var text = new Text(newText);
            run.Append(text);
            paragraph.Append(run);
            contentElement.Append(paragraph);
        }

        /// <summary>
        /// Removes content controls after populating them, following the pattern from DocumentProcessor
        /// </summary>
        /// <param name="document">The document to process</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        private void RemoveContentControls(Document document, string correlationId)
        {
            var sdtElements = document.Descendants<SdtElement>().ToList();
            _logger.LogInformation($"[{correlationId}] Removing {sdtElements.Count} content controls.");
            
            // Process from bottom to top to avoid issues with nested content controls
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                var parent = sdt.Parent;
                if (parent == null) continue;
                
                var contentElements = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                if (contentElements == null) continue;
                
                var contentToPreserve = contentElements.ChildElements.ToList();
                
                if (contentToPreserve.Count > 0)
                {
                    foreach (var child in contentToPreserve)
                    {
                        var clonedChild = child.CloneNode(true);
                        
                        // Fix text formatting on all Run elements inside this content
                        foreach (var run in clonedChild.Descendants<Run>())
                        {
                            var runProps = run.RunProperties ?? run.AppendChild(new RunProperties());
                            
                            // Remove any existing color and explicitly set to black
                            var colorElements = runProps.Elements<Color>().ToList();
                            foreach (var color in colorElements)
                            {
                                runProps.RemoveChild(color);
                            }
                            
                            // Explicitly set text color to black
                            runProps.AppendChild(new Color() { Val = "000000" });
                            
                            // Remove any shading that might affect text appearance
                            var shadingElements = runProps.Elements<Shading>().ToList();
                            foreach (var shading in shadingElements)
                            {
                                runProps.RemoveChild(shading);
                            }
                        }
                        
                        parent.InsertBefore(clonedChild, sdt);
                    }
                }
                
                // Remove the content control
                parent.RemoveChild(sdt);
            }
            
            _logger.LogInformation($"[{correlationId}] Content controls removal completed.");
        }
    }

    /// <summary>
    /// Request model for ouderschapsplan generation
    /// </summary>
    public class OuderschapsplanRequest
    {
        public int DossierId { get; set; }
    }

}