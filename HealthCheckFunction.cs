using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
        {
            _logger.LogInformation("Health check requested");

            return new OkObjectResult(new
            {
                status = "Healthy",
                service = "Scheidingsdesk Document Generator",
                version = "2.0.0",
                timestamp = DateTime.UtcNow,
                endpoints = new[]
                {
                    new { name = "ProcessDocument", path = "/api/process", method = "POST" },
                    new { name = "RemoveContentControls", path = "/api/RemoveContentControls", method = "POST" }
                }
            });
        }
    }
}