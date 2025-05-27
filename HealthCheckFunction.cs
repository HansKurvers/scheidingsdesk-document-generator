using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Scheidingsdesk
{
    public class HealthCheckFunction
    {
        private readonly ILogger<HealthCheckFunction> _logger;

        public HealthCheckFunction(ILogger<HealthCheckFunction> logger)
        {
            _logger = logger;
        }

        [Function("HealthCheck")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            _logger.LogInformation("Health check requested");

            var response = req.CreateResponse(HttpStatusCode.OK);
            
            await response.WriteAsJsonAsync(new
            {
                status = "Healthy",
                service = "Scheidingsdesk Document Generator",
                version = "1.0.0",
                timestamp = DateTime.UtcNow,
                endpoints = new[]
                {
                    new { name = "ProcessDocument", path = "/api/process", method = "POST" },
                    new { name = "RemoveContentControls", path = "/api/RemoveContentControls", method = "POST" }
                }
            });

            return response;
        }
    }
}