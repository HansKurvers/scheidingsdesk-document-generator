using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private DossierData? _currentDossierData;

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
                var documentStream = new MemoryStream();
                await documentStream.WriteAsync(templateBytes, 0, templateBytes.Length);
                documentStream.Position = 0;
                
                // Create grammar rules based on number of children
                var grammarRules = CreateGrammarRules(data.Kinderen.Count, data.Kinderen, correlationId);
                
                // Build all replacements
                var replacements = BuildAllReplacements(data, grammarRules, correlationId);
                
                // Store data for table generation
                _currentDossierData = data;
                
                // Process the document with simple find and replace
                using (WordprocessingDocument doc = WordprocessingDocument.Open(documentStream, true))
                {
                    _logger.LogInformation($"[{correlationId}] Document opened successfully for processing.");
                    
                    var mainPart = doc.MainDocumentPart;
                    if (mainPart == null)
                    {
                        _logger.LogError($"[{correlationId}] MainDocumentPart is null!");
                        return null;
                    }
                    
                    // Get the document body as text
                    var body = mainPart.Document.Body;
                    
                    // Simple approach: Process all text elements directly
                    ProcessDocumentWithSimpleReplace(body, replacements, correlationId);
                    
                    // Also process headers and footers
                    ProcessHeadersAndFooters(mainPart, replacements, correlationId);
                    
                    // Save changes
                    mainPart.Document.Save();
                    _logger.LogInformation($"[{correlationId}] Document generation completed successfully.");
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
        /// Build all replacements into a single dictionary
        /// </summary>
        private Dictionary<string, string> BuildAllReplacements(DossierData data, Dictionary<string, string> grammarRules, string correlationId)
        {
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            // Add grammar rules
            foreach (var rule in grammarRules)
            {
                replacements[rule.Key] = rule.Value;
            }
            
            // Add party data
            if (data.Partij1 != null)
            {
                AddPersonReplacements(replacements, "Partij1", data.Partij1);
            }
            
            if (data.Partij2 != null)
            {
                AddPersonReplacements(replacements, "Partij2", data.Partij2);
            }
            
            // Add dossier data
            replacements["DossierNummer"] = data.DossierNummer ?? "";
            replacements["DossierDatum"] = data.AangemaaktOp.ToString("dd-MM-yyyy");
            replacements["HuidigeDatum"] = DateTime.Now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("nl-NL"));
            
            // Add children data
            if (data.Kinderen.Any())
            {
                replacements["AantalKinderen"] = data.Kinderen.Count.ToString();
                
                for (int i = 0; i < data.Kinderen.Count; i++)
                {
                    var child = data.Kinderen[i];
                    var prefix = $"Kind{i + 1}";
                    
                    replacements[$"{prefix}Naam"] = child.VolledigeNaam ?? "";
                    replacements[$"{prefix}Voornaam"] = child.Voornamen ?? "";
                    replacements[$"{prefix}Achternaam"] = child.Achternaam ?? "";
                    replacements[$"{prefix}Geboortedatum"] = child.GeboorteDatum?.ToString("dd-MM-yyyy") ?? "";
                    replacements[$"{prefix}Leeftijd"] = child.Leeftijd?.ToString() ?? "";
                    replacements[$"{prefix}Geslacht"] = child.Geslacht ?? "";
                }
                
                // Create a formatted list of all children names
                var kinderenNamen = string.Join(", ", data.Kinderen.Select(k => k.Voornamen ?? k.VolledigeNaam));
                replacements["KinderenNamen"] = kinderenNamen;
                replacements["KinderenVolledigeNamen"] = string.Join(", ", data.Kinderen.Select(k => k.VolledigeNaam));
            }
            
            _logger.LogInformation($"[{correlationId}] Built {replacements.Count} replacements");
            return replacements;
        }
        
        /// <summary>
        /// Add person-related replacements
        /// </summary>
        private void AddPersonReplacements(Dictionary<string, string> replacements, string prefix, PersonData person)
        {
            replacements[$"{prefix}Naam"] = person.VolledigeNaam ?? "";
            replacements[$"{prefix}Voornaam"] = person.Voornamen ?? "";
            replacements[$"{prefix}Achternaam"] = person.Achternaam ?? "";
            replacements[$"{prefix}Tussenvoegsel"] = person.Tussenvoegsel ?? "";
            replacements[$"{prefix}Adres"] = person.Adres ?? "";
            replacements[$"{prefix}Postcode"] = person.Postcode ?? "";
            replacements[$"{prefix}Plaats"] = person.Plaats ?? "";
            replacements[$"{prefix}Telefoon"] = person.Telefoon ?? "";
            replacements[$"{prefix}Email"] = person.Email ?? "";
            replacements[$"{prefix}Geboortedatum"] = person.GeboorteDatum?.ToString("dd-MM-yyyy") ?? "";
            
            // Combined address
            var volledigAdres = $"{person.Adres}, {person.Postcode} {person.Plaats}".Trim(' ', ',');
            replacements[$"{prefix}VolledigAdres"] = volledigAdres;
        }
        
        /// <summary>
        /// Process document with simple find and replace
        /// </summary>
        private void ProcessDocumentWithSimpleReplace(Body body, Dictionary<string, string> replacements, string correlationId)
        {
            // Get all paragraphs and process them
            var paragraphs = body.Descendants<Paragraph>().ToList();
            _logger.LogInformation($"[{correlationId}] Processing {paragraphs.Count} paragraphs");
            
            // Process paragraphs and check for table placeholders
            for (int i = paragraphs.Count - 1; i >= 0; i--)
            {
                var paragraph = paragraphs[i];
                
                // Check if this paragraph contains a table placeholder
                var text = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
                
                if (text.Contains("[[TABEL_"))
                {
                    // Handle table placeholders
                    ProcessTablePlaceholder(paragraph, text, correlationId);
                }
                else
                {
                    // Regular text replacement
                    ProcessParagraph(paragraph, replacements, correlationId);
                }
            }
            
            // Also process any existing tables
            var tables = body.Descendants<Table>().ToList();
            _logger.LogInformation($"[{correlationId}] Processing {tables.Count} tables");
            
            foreach (var table in tables)
            {
                foreach (var row in table.Descendants<TableRow>())
                {
                    foreach (var cell in row.Descendants<TableCell>())
                    {
                        foreach (var para in cell.Descendants<Paragraph>())
                        {
                            ProcessParagraph(para, replacements, correlationId);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Process a single paragraph
        /// </summary>
        private void ProcessParagraph(Paragraph paragraph, Dictionary<string, string> replacements, string correlationId)
        {
            // Get all text in the paragraph
            var texts = paragraph.Descendants<Text>().ToList();
            if (!texts.Any()) return;
            
            // Combine all text to handle placeholders that might be split
            var fullText = string.Join("", texts.Select(t => t.Text));
            
            // Check if this paragraph contains any placeholders
            bool hasPlaceholders = replacements.Keys.Any(key => 
                fullText.Contains($"[[{key}]]") || 
                fullText.Contains($"{{{key}}}") ||
                fullText.Contains($"<<{key}>>") ||
                fullText.Contains($"[{key}]"));
            
            if (!hasPlaceholders) return;
            
            // Apply replacements
            var newText = fullText;
            foreach (var replacement in replacements)
            {
                // Try different placeholder formats
                newText = newText.Replace($"[[{replacement.Key}]]", replacement.Value);
                newText = newText.Replace($"{{{replacement.Key}}}", replacement.Value);
                newText = newText.Replace($"<<{replacement.Key}>>", replacement.Value);
                newText = newText.Replace($"[{replacement.Key}]", replacement.Value);
            }
            
            if (newText != fullText)
            {
                // Clear existing text elements
                texts.Skip(1).ToList().ForEach(t => t.Remove());
                
                // Update the first text element with the new text
                if (texts.Any())
                {
                    texts[0].Text = newText;
                }
                
                _logger.LogDebug($"[{correlationId}] Replaced text in paragraph");
            }
        }
        
        /// <summary>
        /// Process headers and footers
        /// </summary>
        private void ProcessHeadersAndFooters(MainDocumentPart mainPart, Dictionary<string, string> replacements, string correlationId)
        {
            // Process headers
            foreach (var headerPart in mainPart.HeaderParts)
            {
                foreach (var paragraph in headerPart.Header.Descendants<Paragraph>())
                {
                    ProcessParagraph(paragraph, replacements, correlationId);
                }
            }
            
            // Process footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                foreach (var paragraph in footerPart.Footer.Descendants<Paragraph>())
                {
                    ProcessParagraph(paragraph, replacements, correlationId);
                }
            }
        }
        
        /// <summary>
        /// Process table placeholders and replace with generated tables
        /// </summary>
        private void ProcessTablePlaceholder(Paragraph paragraph, string text, string correlationId)
        {
            if (_currentDossierData == null)
            {
                _logger.LogWarning($"[{correlationId}] No dossier data available for table generation");
                return;
            }
            
            Table? newTable = null;
            
            // Determine which table to generate based on placeholder
            if (text.Contains("[[TABEL_OMGANG]]"))
            {
                newTable = GenerateOmgangTable(_currentDossierData.Omgang, correlationId);
            }
            else if (text.Contains("[[TABEL_ZORG]]"))
            {
                newTable = GenerateZorgTable(_currentDossierData.Zorg, correlationId);
            }
            else if (text.Contains("[[TABEL_VAKANTIES]]"))
            {
                newTable = GenerateVakantiesTable(correlationId);
            }
            else if (text.Contains("[[TABEL_FEESTDAGEN]]"))
            {
                newTable = GenerateFeestdagenTable(correlationId);
            }
            
            if (newTable != null)
            {
                // Insert the table after the paragraph
                paragraph.Parent?.InsertAfter(newTable, paragraph);
                // Remove the placeholder paragraph
                paragraph.Remove();
                _logger.LogInformation($"[{correlationId}] Replaced table placeholder with generated table");
            }
        }
        
        /// <summary>
        /// Generate table for omgang (visitation) arrangements
        /// </summary>
        private Table GenerateOmgangTable(List<OmgangData> omgangData, string correlationId)
        {
            var table = new Table();
            
            // Add table properties for borders
            var tblProp = new TableProperties();
            var tblBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            );
            tblProp.Append(tblBorders);
            table.Append(tblProp);
            
            // Add header row
            var headerRow = new TableRow();
            AddTableCell(headerRow, "Dag", true);
            AddTableCell(headerRow, "Dagdeel", true);
            AddTableCell(headerRow, "Bij ouder", true);
            AddTableCell(headerRow, "Wisseltijd", true);
            AddTableCell(headerRow, "Regeling", true);
            table.Append(headerRow);
            
            // Add data rows
            foreach (var omgang in omgangData.OrderBy(o => o.DagId).ThenBy(o => o.DagdeelId))
            {
                var row = new TableRow();
                AddTableCell(row, omgang.DagNaam ?? "");
                AddTableCell(row, omgang.DagdeelNaam ?? "");
                AddTableCell(row, omgang.VerzorgerNaam ?? "");
                AddTableCell(row, omgang.WisselTijd ?? "");
                AddTableCell(row, omgang.EffectieveRegeling);
                table.Append(row);
            }
            
            _logger.LogInformation($"[{correlationId}] Generated omgang table with {omgangData.Count} rows");
            return table;
        }
        
        /// <summary>
        /// Generate table for zorg (care) arrangements
        /// </summary>
        private Table GenerateZorgTable(List<ZorgData> zorgData, string correlationId)
        {
            var table = new Table();
            
            // Add table properties for borders
            var tblProp = new TableProperties();
            var tblBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            );
            tblProp.Append(tblBorders);
            table.Append(tblProp);
            
            // Add header row
            var headerRow = new TableRow();
            AddTableCell(headerRow, "Categorie", true);
            AddTableCell(headerRow, "Situatie", true);
            AddTableCell(headerRow, "Afspraak", true);
            table.Append(headerRow);
            
            // Add data rows
            foreach (var zorg in zorgData.OrderBy(z => z.ZorgCategorieNaam))
            {
                var row = new TableRow();
                AddTableCell(row, zorg.ZorgCategorieNaam ?? "");
                AddTableCell(row, zorg.EffectieveSituatie);
                AddTableCell(row, zorg.Overeenkomst ?? "");
                table.Append(row);
            }
            
            _logger.LogInformation($"[{correlationId}] Generated zorg table with {zorgData.Count} rows");
            return table;
        }
        
        /// <summary>
        /// Generate table for vakanties (holidays)
        /// </summary>
        private Table GenerateVakantiesTable(string correlationId)
        {
            var table = new Table();
            
            // Add table properties
            var tblProp = new TableProperties();
            var tblBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            );
            tblProp.Append(tblBorders);
            table.Append(tblProp);
            
            // Add header row
            var headerRow = new TableRow();
            AddTableCell(headerRow, "Vakantie", true);
            AddTableCell(headerRow, "Even jaren", true);
            AddTableCell(headerRow, "Oneven jaren", true);
            table.Append(headerRow);
            
            // Add standard Dutch school holidays
            var vakanties = new[]
            {
                new { Naam = "Voorjaarsvakantie", Even = "Partij 1", Oneven = "Partij 2" },
                new { Naam = "Meivakantie", Even = "Partij 2", Oneven = "Partij 1" },
                new { Naam = "Zomervakantie (1e helft)", Even = "Partij 1", Oneven = "Partij 2" },
                new { Naam = "Zomervakantie (2e helft)", Even = "Partij 2", Oneven = "Partij 1" },
                new { Naam = "Herfstvakantie", Even = "Partij 1", Oneven = "Partij 2" },
                new { Naam = "Kerstvakantie", Even = "Partij 2", Oneven = "Partij 1" }
            };
            
            foreach (var vakantie in vakanties)
            {
                var row = new TableRow();
                AddTableCell(row, vakantie.Naam);
                AddTableCell(row, vakantie.Even);
                AddTableCell(row, vakantie.Oneven);
                table.Append(row);
            }
            
            _logger.LogInformation($"[{correlationId}] Generated vakanties table");
            return table;
        }
        
        /// <summary>
        /// Generate table for feestdagen (holidays)
        /// </summary>
        private Table GenerateFeestdagenTable(string correlationId)
        {
            var table = new Table();
            
            // Add table properties
            var tblProp = new TableProperties();
            var tblBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            );
            tblProp.Append(tblBorders);
            table.Append(tblProp);
            
            // Add header row
            var headerRow = new TableRow();
            AddTableCell(headerRow, "Feestdag", true);
            AddTableCell(headerRow, "Even jaren", true);
            AddTableCell(headerRow, "Oneven jaren", true);
            table.Append(headerRow);
            
            // Add standard holidays
            var feestdagen = new[]
            {
                new { Naam = "Goede Vrijdag", Even = "Partij 1", Oneven = "Partij 2" },
                new { Naam = "1e Paasdag", Even = "Partij 2", Oneven = "Partij 1" },
                new { Naam = "2e Paasdag", Even = "Partij 1", Oneven = "Partij 2" },
                new { Naam = "Koningsdag", Even = "Partij 2", Oneven = "Partij 1" },
                new { Naam = "Hemelvaartsdag", Even = "Partij 1", Oneven = "Partij 2" },
                new { Naam = "1e Pinksterdag", Even = "Partij 2", Oneven = "Partij 1" },
                new { Naam = "2e Pinksterdag", Even = "Partij 1", Oneven = "Partij 2" },
                new { Naam = "1e Kerstdag", Even = "Partij 2", Oneven = "Partij 1" },
                new { Naam = "2e Kerstdag", Even = "Partij 1", Oneven = "Partij 2" },
                new { Naam = "Oudjaarsdag", Even = "Partij 2", Oneven = "Partij 1" },
                new { Naam = "Nieuwjaarsdag", Even = "Partij 1", Oneven = "Partij 2" }
            };
            
            foreach (var feestdag in feestdagen)
            {
                var row = new TableRow();
                AddTableCell(row, feestdag.Naam);
                AddTableCell(row, feestdag.Even);
                AddTableCell(row, feestdag.Oneven);
                table.Append(row);
            }
            
            _logger.LogInformation($"[{correlationId}] Generated feestdagen table");
            return table;
        }
        
        /// <summary>
        /// Helper method to add a cell to a table row
        /// </summary>
        private void AddTableCell(TableRow row, string text, bool isHeader = false)
        {
            var cell = new TableCell();
            var paragraph = new Paragraph();
            var run = new Run();
            
            if (isHeader)
            {
                var runProps = new RunProperties();
                runProps.Append(new Bold());
                run.Append(runProps);
            }
            
            run.Append(new Text(text));
            paragraph.Append(run);
            cell.Append(paragraph);
            row.Append(cell);
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
            
            // Also process all text in the document, not just content controls
            ProcessAllTextInDocument(document, data, grammarRules, correlationId);
            
            var sdtElements = document.Descendants<SdtElement>().ToList();
            _logger.LogInformation($"[{correlationId}] Found {sdtElements.Count} content controls to process");
            
            foreach (var sdt in sdtElements)
            {
                try
                {
                    // Try to get the tag/alias of the content control
                    var sdtProperties = sdt.Descendants<SdtProperties>().FirstOrDefault();
                    var tag = sdtProperties?.Descendants<Tag>()?.FirstOrDefault()?.Val?.Value;
                    var alias = sdtProperties?.Descendants<SdtAlias>()?.FirstOrDefault()?.Val?.Value;
                    
                    _logger.LogInformation($"[{correlationId}] Content control - Tag: '{tag}', Alias: '{alias}'");
                    
                    var contentText = GetSdtContentText(sdt);
                    
                    if (string.IsNullOrWhiteSpace(contentText))
                    {
                        _logger.LogInformation($"[{correlationId}] Empty content control with tag: '{tag}'");
                        continue;
                    }
                    
                    _logger.LogInformation($"[{correlationId}] Processing content control text: '{contentText.Substring(0, Math.Min(contentText.Length, 100))}'");
                    
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
                        _logger.LogInformation($"[{correlationId}] Updated content control: '{contentText.Substring(0, Math.Min(contentText.Length, 50))}' -> '{processedText.Substring(0, Math.Min(processedText.Length, 50))}'");
                    }
                    else
                    {
                        _logger.LogInformation($"[{correlationId}] No changes made to content control with text: '{contentText.Substring(0, Math.Min(contentText.Length, 50))}'");
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
        /// Process all text elements in the document, not just content controls
        /// </summary>
        private void ProcessAllTextInDocument(Document document, DossierData data, Dictionary<string, string> grammarRules, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Processing all text elements in document");
            
            // Get all text elements in the document
            var textElements = document.Descendants<Text>().ToList();
            _logger.LogInformation($"[{correlationId}] Found {textElements.Count} text elements");
            
            foreach (var textElement in textElements)
            {
                try
                {
                    var originalText = textElement.Text;
                    if (string.IsNullOrWhiteSpace(originalText))
                        continue;
                    
                    // Apply grammar rules
                    var processedText = ApplyGrammarRules(originalText, grammarRules);
                    
                    // Apply data replacements
                    processedText = ApplyDataReplacements(processedText, data, correlationId);
                    
                    // Update the text if it changed
                    if (processedText != originalText)
                    {
                        textElement.Text = processedText;
                        _logger.LogDebug($"[{correlationId}] Updated text: '{originalText.Substring(0, Math.Min(originalText.Length, 50))}' -> '{processedText.Substring(0, Math.Min(processedText.Length, 50))}'");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[{correlationId}] Error processing text element: {ex.Message}");
                }
            }
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
            var originalText = text;
            
            // Log sample of text to understand format
            if (text.Length > 0)
            {
                _logger.LogDebug($"[{correlationId}] Sample text for replacement: '{text.Substring(0, Math.Min(text.Length, 200))}'");
            }
            
            // Use reflection to replace all placeholders with proper string conversions
            var replacements = new Dictionary<string, string>();
            
            // Replace party information
            if (data.Partij1 != null)
            {
                AddObjectReplacements(replacements, "Partij1", data.Partij1);
                // Also add simplified versions
                replacements["Partij1Naam"] = ConvertToString(data.Partij1.VolledigeNaam);
                replacements["Partij1Adres"] = ConvertToString(data.Partij1.Adres);
                replacements["Partij1Postcode"] = ConvertToString(data.Partij1.Postcode);
                replacements["Partij1Plaats"] = ConvertToString(data.Partij1.Plaats);
            }
            
            if (data.Partij2 != null)
            {
                AddObjectReplacements(replacements, "Partij2", data.Partij2);
                // Also add simplified versions
                replacements["Partij2Naam"] = ConvertToString(data.Partij2.VolledigeNaam);
                replacements["Partij2Adres"] = ConvertToString(data.Partij2.Adres);
                replacements["Partij2Postcode"] = ConvertToString(data.Partij2.Postcode);
                replacements["Partij2Plaats"] = ConvertToString(data.Partij2.Plaats);
            }
            
            // Replace dossier information with multiple formats
            AddDossierReplacements(replacements, data);
            
            // Replace child information
            for (int i = 0; i < data.Kinderen.Count; i++)
            {
                var child = data.Kinderen[i];
                AddObjectReplacements(replacements, $"Kind{i + 1}", child);
                // Also add simplified versions
                replacements[$"Kind{i + 1}Naam"] = ConvertToString(child.VolledigeNaam);
                replacements[$"Kind{i + 1}Geboortedatum"] = ConvertToString(child.GeboorteDatum);
            }
            
            // Apply all replacements with different bracket formats
            foreach (var replacement in replacements)
            {
                // Try multiple placeholder formats
                var formats = new[] {
                    $"{{{{{replacement.Key}}}}}",     // {{Key}}
                    $"{{{replacement.Key}}}",         // {Key}
                    $"[{replacement.Key}]",           // [Key]
                    $"<<{replacement.Key}>>",         // <<Key>>
                    replacement.Key                    // Just the key without brackets
                };
                
                foreach (var format in formats)
                {
                    if (text.Contains(format))
                    {
                        text = text.Replace(format, replacement.Value);
                        _logger.LogDebug($"[{correlationId}] Replaced '{format}' with '{replacement.Value}'");
                    }
                }
            }
            
            if (text != originalText)
            {
                _logger.LogInformation($"[{correlationId}] Text was modified during replacement");
            }
            
            return text;
        }
        
        /// <summary>
        /// Add dossier replacements with multiple key formats
        /// </summary>
        private void AddDossierReplacements(Dictionary<string, string> replacements, DossierData data)
        {
            // Add with dot notation
            replacements["Dossier.DossierNummer"] = ConvertToString(data.DossierNummer);
            replacements["Dossier.AangemaaktOp"] = ConvertToString(data.AangemaaktOp);
            replacements["Dossier.GewijzigdOp"] = ConvertToString(data.GewijzigdOp);
            replacements["Dossier.Status"] = ConvertToString(data.Status);
            replacements["Dossier.GebruikerId"] = ConvertToString(data.GebruikerId);
            replacements["Dossier.Id"] = ConvertToString(data.Id);
            
            // Add without dot notation
            replacements["DossierNummer"] = ConvertToString(data.DossierNummer);
            replacements["DossierAangemaaktOp"] = ConvertToString(data.AangemaaktOp);
            replacements["DossierGewijzigdOp"] = ConvertToString(data.GewijzigdOp);
            replacements["DossierStatus"] = ConvertToString(data.Status);
            replacements["DossierId"] = ConvertToString(data.Id);
        }
        
        /// <summary>
        /// Adds replacements for all properties of an object
        /// </summary>
        private void AddObjectReplacements(Dictionary<string, string> replacements, string prefix, object obj)
        {
            if (obj == null) return;
            
            var properties = obj.GetType().GetProperties();
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    // Add with dot notation
                    var keyWithDot = $"{prefix}.{prop.Name}";
                    replacements[keyWithDot] = ConvertToString(value);
                    
                    // Also add without dot notation for flexibility
                    var keyWithoutDot = $"{prefix}{prop.Name}";
                    replacements[keyWithoutDot] = ConvertToString(value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to get property {prop.Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Safely converts any value to a string representation
        /// </summary>
        private string ConvertToString(object? value)
        {
            if (value == null)
                return string.Empty;
                
            // Handle specific types with custom formatting
            if (value is DateTime dateTime)
                return dateTime.ToString("dd-MM-yyyy");
                
            if (value is bool boolValue)
                return boolValue ? "Ja" : "Nee";
                
            // Handle nullable types
            var type = value.GetType();
            if (type == typeof(DateTime?))
            {
                var nullableDateTime = (DateTime?)value;
                return nullableDateTime.HasValue ? nullableDateTime.Value.ToString("dd-MM-yyyy") : string.Empty;
            }
            
            if (type == typeof(bool?))
            {
                var nullableBool = (bool?)value;
                return nullableBool.HasValue ? (nullableBool.Value ? "Ja" : "Nee") : string.Empty;
            }
                
            // Default conversion for all other types
            return value.ToString() ?? string.Empty;
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