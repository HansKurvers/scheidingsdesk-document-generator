using System.Threading.Tasks;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration
{
    /// <summary>
    /// Service interface for retrieving document templates
    /// </summary>
    public interface ITemplateProvider
    {
        /// <summary>
        /// Retrieves a document template from storage
        /// </summary>
        /// <param name="templateUrl">URL to the template (with SAS token if needed)</param>
        /// <returns>Template file as byte array</returns>
        Task<byte[]> GetTemplateAsync(string templateUrl);
    }
}