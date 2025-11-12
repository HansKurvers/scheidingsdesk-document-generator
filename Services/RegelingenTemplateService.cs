using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace scheidingsdesk_document_generator.Services
{
    /// <summary>
    /// Service for retrieving regelingen templates from the API
    /// </summary>
    public interface IRegelingenTemplateService
    {
        Task<List<RegelingTemplate>> GetTemplatesByTypeAsync(string templateType, bool meervoudKinderen);
        Task<RegelingTemplate?> GetTemplateBySubtypeAsync(string templateType, string templateSubtype, bool meervoudKinderen);
    }

    public class RegelingenTemplateService : IRegelingenTemplateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RegelingenTemplateService> _logger;
        private readonly string _apiBaseUrl;

        public RegelingenTemplateService(
            IHttpClientFactory httpClientFactory, 
            ILogger<RegelingenTemplateService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            
            // Get API base URL from environment variable or use default
            _apiBaseUrl = Environment.GetEnvironmentVariable("OUDERSCHAPS_API_URL") ?? "https://api.ouderschapsplan.nl";
        }

        /// <summary>
        /// Get all templates for a specific type and plurality
        /// </summary>
        public async Task<List<RegelingTemplate>> GetTemplatesByTypeAsync(string templateType, bool meervoudKinderen)
        {
            try
            {
                var meervoudParam = meervoudKinderen ? "true" : "false";
                var url = $"{_apiBaseUrl}/api/lookups/regelingen-templates?type={Uri.EscapeDataString(templateType)}&meervoudKinderen={meervoudParam}";
                
                _logger.LogInformation($"Fetching templates from: {url}");

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to fetch templates. Status: {response.StatusCode}");
                    return new List<RegelingTemplate>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var templates = JsonConvert.DeserializeObject<List<RegelingTemplate>>(json) ?? new List<RegelingTemplate>();
                
                _logger.LogInformation($"Retrieved {templates.Count} templates for type '{templateType}' (meervoud: {meervoudKinderen})");
                
                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching templates for type '{templateType}'");
                return new List<RegelingTemplate>();
            }
        }

        /// <summary>
        /// Get a specific template by type and subtype
        /// </summary>
        public async Task<RegelingTemplate?> GetTemplateBySubtypeAsync(string templateType, string templateSubtype, bool meervoudKinderen)
        {
            var templates = await GetTemplatesByTypeAsync(templateType, meervoudKinderen);
            
            // Find template matching the subtype
            var template = templates.Find(t => 
                string.Equals(t.TemplateSubtype, templateSubtype, StringComparison.OrdinalIgnoreCase));
            
            if (template == null)
            {
                _logger.LogWarning($"No template found for type '{templateType}', subtype '{templateSubtype}' (meervoud: {meervoudKinderen})");
            }
            
            return template;
        }
    }

    /// <summary>
    /// Represents a regelingen template from the API
    /// </summary>
    public class RegelingTemplate
    {
        public int Id { get; set; }
        public string TemplateTekst { get; set; } = "";
        public string TemplateNaam { get; set; } = "";
        public string? TemplateSubtype { get; set; }
        public string Type { get; set; } = "";
        public bool MeervoudKinderen { get; set; }
    }
}