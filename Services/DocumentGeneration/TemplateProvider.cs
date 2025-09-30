using Microsoft.Extensions.Logging;
using System;
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
        /// Downloads a template from the specified URL
        /// </summary>
        public async Task<byte[]> GetTemplateAsync(string templateUrl)
        {
            if (string.IsNullOrWhiteSpace(templateUrl))
            {
                throw new ArgumentException("Template URL cannot be empty", nameof(templateUrl));
            }

            // Check for placeholder in URL
            if (templateUrl.Contains("[SAS_TOKEN_HERE]"))
            {
                _logger.LogError("Template URL contains placeholder. Please configure a valid SAS token.");
                throw new InvalidOperationException("Template storage not properly configured. Please add SAS token to TemplateStorageUrl setting.");
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