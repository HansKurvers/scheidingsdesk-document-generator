using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Scheidingsdesk
{
    public class GenerateDocument
    {
        private readonly ILogger<GenerateDocument> _logger;

        public GenerateDocument(ILogger<GenerateDocument> logger)
        {
            _logger = logger;
        }

        [Function("GenerateDocument")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Document generation request received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic? data = JsonConvert.DeserializeObject(requestBody);
            
            // Extract data from request
            var mediationData = data?.MediationData;
            var templateId = data?.TemplateId?.ToString() ?? "default_template.docx";
            
            if (mediationData == null)
            {
                return new BadRequestObjectResult("Please provide mediation data in the request body.");
            }
            
            try 
            {
                // Get template from storage
                byte[] templateBytes = await GetTemplateFromStorage(templateId);
                
                // Generate document
                byte[] documentBytes = Generate(templateBytes, mediationData);
                
                // Return the document
                return new FileContentResult(documentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    FileDownloadName = $"Mediation_Report_{DateTime.Now:yyyyMMdd}.docx"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating document: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<byte[]> GetTemplateFromStorage(string templateId)
        {
            // TODO: Implement template retrieval from Azure Blob Storage
            // For now, using a placeholder implementation
            return File.ReadAllBytes("path/to/template.docx");
        }

        private static byte[] Generate(byte[] templateBytes, dynamic mediationData)
        {
            // Create a memory stream with the template
            using MemoryStream memoryStream = new();
            memoryStream.Write(templateBytes, 0, templateBytes.Length);

            // Open the document from memory stream
            using (WordprocessingDocument document = WordprocessingDocument.Open(memoryStream, true))
            {
                // Fill the document with data
                FillContentControls(document, mediationData);

                // Remove content controls but keep their contents
                // RemoveContentControls(document);

                // Save changes to the document
                document.Save();
            }

            return memoryStream.ToArray();
        }
        
        private static void FillContentControls(WordprocessingDocument document, dynamic mediationData)
        {
            // Get the main document part
            MainDocumentPart mainPart = document.MainDocumentPart;
            
            // Find all content controls
            var contentControls = mainPart.Document.Descendants<SdtElement>().ToList();
            
            foreach (var contentControl in contentControls)
            {
                // Get the tag of the content control
                string tag = GetContentControlTag(contentControl);
                
                if (!string.IsNullOrEmpty(tag))
                {
                    // Get the value from the mediation data based on the tag
                    string value = GetValueFromData(mediationData, tag);
                    
                    if (value != null)
                    {
                        // Set the text in the content control
                        SetContentControlText(contentControl, value);
                    }
                }
            }
        }
        private static string GetContentControlTag(SdtElement contentControl)
        {
            var sdtProperties = contentControl.Elements<SdtProperties>().FirstOrDefault();
            if (sdtProperties != null)
            {
                var tagProperty = sdtProperties.Elements<Tag>().FirstOrDefault();
                if (tagProperty != null)
                {
                    return tagProperty.Val?.Value;
                }
            }
            return null;
        }
        
        private static string GetValueFromData(dynamic data, string propertyPath)
        {
            // Handle nested properties using dot notation
            string[] properties = propertyPath.Split('.');
            dynamic currentValue = data;
            
            foreach (string property in properties)
            {
                if (currentValue == null)
                    return null;
                    
                try
                {
                    currentValue = currentValue[property];
                }
                catch
                {
                    return null;
                }
            }
            
            return currentValue?.ToString();
        }
        
        private static void SetContentControlText(SdtElement contentControl, string text)
        {
            // Find the text element inside the content control
            var runElements = contentControl.Descendants<Run>().ToList();
            
            if (runElements.Count > 0)
            {
                // Clear existing text
                foreach (var run in runElements)
                {
                    var textElements = run.Elements<Text>().ToList();
                    foreach (var textElement in textElements)
                    {
                        textElement.Text = "";
                    }
                }
                
                // Set new text in the first run
                var firstRun = runElements.First();
                var firstText = firstRun.GetFirstChild<Text>();
                if (firstText != null)
                {
                    firstText.Text = text;
                }
                else
                {
                    firstRun.AppendChild(new Text(text));
                }
            }
        }
        
        // TODO Remove content controls
    }
}


