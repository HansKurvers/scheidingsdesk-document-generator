using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration
{
    /// <summary>
    /// Simple and robust template provider for downloading Word templates from Azure Blob Storage
    /// </summary>
    public class TemplateProvider : ITemplateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TemplateProvider> _logger;

        public TemplateProvider(IHttpClientFactory httpClientFactory, ILogger<TemplateProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a document template by template type
        /// Maps template types to environment variable URLs
        /// </summary>
        public async Task<byte[]> GetTemplateByTypeAsync(string templateType)
        {
            var templateUrl = GetTemplateUrlForType(templateType);
            return await GetTemplateAsync(templateUrl);
        }

        /// <summary>
        /// Maps template type to the corresponding Azure Blob Storage URL from environment variables
        /// </summary>
        private string GetTemplateUrlForType(string templateType)
        {
            // Default to 'default' if not specified
            templateType = string.IsNullOrWhiteSpace(templateType) ? "default" : templateType.ToLower();

            switch (templateType)
            {
                case "default":
                    var defaultUrl = Environment.GetEnvironmentVariable("TemplateStorageUrl");
                    if (string.IsNullOrWhiteSpace(defaultUrl))
                    {
                        throw new InvalidOperationException("TemplateStorageUrl environment variable is not set.");
                    }
                    _logger.LogInformation("Using default template (ouderschapsplan-template.docx)");
                    return defaultUrl;

                case "v2":
                    var v2Url = Environment.GetEnvironmentVariable("TemplateStorageUrlV2");
                    if (string.IsNullOrWhiteSpace(v2Url))
                    {
                        throw new InvalidOperationException("TemplateStorageUrlV2 environment variable is not set.");
                    }
                    _logger.LogInformation("Using v2 template (Placeholders.docx)");
                    return v2Url;

                default:
                    _logger.LogWarning($"Unknown template type '{templateType}', falling back to default");
                    return GetTemplateUrlForType("default");
            }
        }

        /// <summary>
        /// Downloads a template from the specified URL
        /// </summary>
        public async Task<byte[]> GetTemplateAsync(string templateUrl)
        {
            if (string.IsNullOrWhiteSpace(templateUrl))
            {
                throw new ArgumentException("Template URL cannot be empty", nameof(templateUrl));
            }

            // Check for placeholder in URL
            if (templateUrl.Contains("[SAS_TOKEN_HERE]") || templateUrl.Contains("JOUW-STORAGE-ACCOUNT") || templateUrl.Contains("JOUW-SAS-TOKEN"))
            {
                _logger.LogWarning("Template URL contains placeholder. Using local template for development.");
                
                // Use local template for development
                var localTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ouderschapsplan", "Ouderschapsplan NIEUW.docx");
                
                if (File.Exists(localTemplatePath))
                {
                    _logger.LogInformation($"Using local template from: {localTemplatePath}");
                    return await File.ReadAllBytesAsync(localTemplatePath);
                }
                else
                {
                    _logger.LogError($"Local template not found at: {localTemplatePath}");
                    throw new InvalidOperationException("Template storage not properly configured and local template not found.");
                }
            }

            // URL encode spaces
            templateUrl = templateUrl.Replace(" ", "%20");

            _logger.LogInformation($"Downloading template from Azure Storage");

            try
            {
                var response = await _httpClient.GetAsync(templateUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to download template. Status: {response.StatusCode}");
                    throw new InvalidOperationException($"Failed to download template from Azure Storage. Status: {response.StatusCode}");
                }

                var templateBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation($"Template downloaded successfully. Size: {templateBytes.Length} bytes");

                return templateBytes;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error downloading template");
                throw new InvalidOperationException("Error downloading template from Azure Storage", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error downloading template");
                throw;
            }
        }
    }
}