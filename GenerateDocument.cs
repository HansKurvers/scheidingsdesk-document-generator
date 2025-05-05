using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
