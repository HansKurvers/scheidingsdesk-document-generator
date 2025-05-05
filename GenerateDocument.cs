using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Net.Http.Headers;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Scheidingsdesk
{
    public static class GenerateDocument
    {
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Document generation process started");

            // Parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<DocumentRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.RecordId) || string.IsNullOrEmpty(data.EntityName))
            {
                return new BadRequestObjectResult("Please provide RecordId and EntityName in the request body");
            }

            try
            {
                // Get data from Dataverse
                var recordData = await GetDataFromDataverse(data.EntityName, data.RecordId, log);
                if (recordData == null)
                {
                    return new BadRequestObjectResult($"Record with ID {data.RecordId} not found in {data.EntityName}");
                }

                // Get the template from blob storage or SharePoint
                byte[] templateBytes = await GetTemplateDocument(data.TemplateUrl);
                
                // Process the template
                byte[] resultDocumentBytes = ProcessTemplate(templateBytes, recordData, log);
                
                // Return the document
                return new FileContentResult(resultDocumentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    FileDownloadName = $"{data.DocumentName ?? "Generated_Document"}.docx"
                };
            }
            catch (Exception ex)
            {
                log.LogError($"Error generating document: {ex.Message}");
                log.LogError(ex.StackTrace);
                return new ObjectResult($"Error generating document: {ex.Message}") { StatusCode = 500 };
            }
        }

        private static async Task<Entity> GetDataFromDataverse(string entityName, string recordId, ILogger log)
        {
            log.LogInformation($"Fetching data from Dataverse for {entityName} with ID {recordId}");
            
            // Get connection string from environment variables
            string connectionString = Environment.GetEnvironmentVariable("DataverseConnectionString");
            
            using (var serviceClient = new ServiceClient(connectionString))
            {
                if (!serviceClient.IsReady)
                {
                    throw new Exception("Failed to connect to Dataverse");
                }
                
                try
                {
                    // Create the request to retrieve all attributes for the entity
                    var query = new QueryExpression(entityName)
                    {
                        ColumnSet = new ColumnSet(true), // Get all attributes
                        Criteria = new FilterExpression(LogicalOperator.And)
                    };
                    
                    // Convert string ID to Guid
                    Guid recordGuid = Guid.Parse(recordId);
                    query.Criteria.AddCondition(entityName + "id", ConditionOperator.Equal, recordGuid);
                    
                    // Execute the query
                    EntityCollection result = serviceClient.RetrieveMultiple(query);
                    
                    if (result.Entities.Count > 0)
                    {
                        return result.Entities[0];
                    }
                    
                    return null;
                }
                catch (Exception ex)
                {
                    log.LogError($"Error querying Dataverse: {ex.Message}");
                    throw;
                }
            }
        }

        private static async Task<byte[]> GetTemplateDocument(string templateUrl)
        {
            // If templateUrl is a SharePoint URL, use Microsoft Graph to download
            if (templateUrl.Contains("sharepoint.com"))
            {
                return await DownloadFromSharePoint(templateUrl);
            }
            
            // If it's a direct URL, download it
            using (var httpClient = new HttpClient())
            {
                return await httpClient.GetByteArrayAsync(templateUrl);
            }
        }
        
        private static async Task<byte[]> DownloadFromSharePoint(string sharePointUrl)
        {
            // Get the Microsoft Graph access token
            string accessToken = await GetMicrosoftGraphAccessToken();
            
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                // Convert SharePoint URL to Microsoft Graph API URL format
                // This is a simplified example - actual conversion depends on your SharePoint structure
                string graphUrl = ConvertToGraphUrl(sharePointUrl);
                
                // Download the file
                return await httpClient.GetByteArrayAsync(graphUrl);
            }
        }
        
        private static string ConvertToGraphUrl(string sharePointUrl)
        {
            // This is a simplified example - actual conversion depends on your SharePoint structure
            // Example conversion: 
            // From: https://contoso.sharepoint.com/sites/site/Shared%20Documents/template.docx
            // To: https://graph.microsoft.com/v1.0/sites/contoso.sharepoint.com:/sites/site:/drive/root:/Shared%20Documents/template.docx:/content
            
            // Extract the site and file path
            Uri uri = new Uri(sharePointUrl);
            string host = uri.Host;
            string sitePath = uri.AbsolutePath.Substring(0, uri.AbsolutePath.IndexOf("/", 1));
            string filePath = uri.AbsolutePath.Substring(sitePath.Length);
            
            return $"https://graph.microsoft.com/v1.0/sites/{host}:{sitePath}:/drive/root:{filePath}:/content";
        }
        
        private static async Task<string> GetMicrosoftGraphAccessToken()
        {
            // Get credentials from environment variables
            string clientId = Environment.GetEnvironmentVariable("MicrosoftGraphClientId");
            string clientSecret = Environment.GetEnvironmentVariable("MicrosoftGraphClientSecret");
            string tenantId = Environment.GetEnvironmentVariable("MicrosoftGraphTenantId");
            
            // Get token using client credentials flow
            using (var httpClient = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
                });
                
                var response = await httpClient.PostAsync($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", content);
                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);
                
                return tokenResponse.AccessToken;
            }
        }

        private static byte[] ProcessTemplate(byte[] templateBytes, Entity recordData, ILogger log)
        {
            using (MemoryStream templateStream = new MemoryStream())
            {
                templateStream.Write(templateBytes, 0, templateBytes.Length);
                
                // Create a copy for the result
                using (MemoryStream resultStream = new MemoryStream())
                {
                    templateStream.Position = 0;
                    templateStream.CopyTo(resultStream);
                    resultStream.Position = 0;
                    
                    using (WordprocessingDocument document = WordprocessingDocument.Open(resultStream, true))
                    {
                        // Process all content controls in the main document part
                        ProcessContentControls(document.MainDocumentPart, recordData, log);
                        
                        // Process all content controls in the header parts
                        if (document.MainDocumentPart.HeaderParts != null)
                        {
                            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
                            {
                                ProcessContentControls(headerPart, recordData, log);
                            }
                        }
                        
                        // Process all content controls in the footer parts
                        if (document.MainDocumentPart.FooterParts != null)
                        {
                            foreach (var footerPart in document.MainDocumentPart.FooterParts)
                            {
                                ProcessContentControls(footerPart, recordData, log);
                            }
                        }
                        
                        // Save the document
                        document.Save();
                    }
                    
                    // Return the modified document
                    return resultStream.ToArray();
                }
            }
        }

        private static void ProcessContentControls(OpenXmlPart part, Entity recordData, ILogger log)
        {
            // Find all structured document tags (SDTs, aka content controls)
            var sdtElements = part.RootElement.Descendants<SdtElement>().ToList();
            
            log.LogInformation($"Found {sdtElements.Count} content controls in document part");
            
            foreach (var sdt in sdtElements)
            {
                // Get the tag of the content control (this is how we identify which field to use)
                var tag = sdt.SdtProperties?.GetFirstChild<Tag>();
                
                if (tag != null && !string.IsNullOrEmpty(tag.Val))
                {
                    string tagValue = tag.Val.Value;
                    log.LogInformation($"Processing content control with tag: {tagValue}");
                    
                    // Check if the record has this attribute
                    if (recordData.Contains(tagValue))
                    {
                        // Get the value from the record
                        object fieldValue = recordData[tagValue];
                        string textValue = ConvertToString(fieldValue);
                        
                        // Find the text inside the content control
                        var textElements = sdt.Descendants<Text>();
                        
                        // Update all text elements within the content control
                        foreach (var textElement in textElements)
                        {
                            textElement.Text = textValue;
                        }
                    }
                }
                
                // Find the content part of the SDT
                var sdtContentPart = sdt.Descendants<SdtContentBlock>().FirstOrDefault() ?? (OpenXmlElement)sdt.Descendants<SdtContentRun>().FirstOrDefault();

                if (sdtContentPart != null)
                {
                    var parent = sdt.Parent;
                    // Get the children of the content part
                    var contentChildren = sdtContentPart.Elements().ToList();

                    // Insert the children before the SDT
                    foreach (var child in contentChildren)
                    {
                        parent.InsertBefore(child.CloneNode(true), sdt);
                    }

                    // Remove the SDT
                    sdt.Remove();
                }
            }
        }

        private static string ConvertToString(object value)
        {
            if (value == null)
                return string.Empty;
                
            // Handle different data types
            if (value is DateTime dateTime)
                return dateTime.ToString("dd-MM-yyyy");
                
            if (value is Money money)
                return money.Value.ToString("C");
                
            if (value is OptionSetValue optionSet)
                return optionSet.Value.ToString();
                
            if (value is EntityReference entityRef)
                return entityRef.Name ?? entityRef.Id.ToString();
                
            // Default conversion
            return value.ToString();
        }
    }

    public class DocumentRequest
    {
        public string RecordId { get; set; }
        public string EntityName { get; set; }
        public string TemplateUrl { get; set; }
        public string DocumentName { get; set; }
    }

    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
