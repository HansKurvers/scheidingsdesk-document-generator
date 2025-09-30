using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scheidingsdesk;
using scheidingsdesk_document_generator.Services;
using scheidingsdesk_document_generator.Services.DocumentGeneration;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Processors;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Generators;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Configure Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configure logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Register HTTP client
        services.AddHttpClient();

        // Register database service
        services.AddScoped<DatabaseService>();

        // Register document generation services
        services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
        services.AddScoped<ITemplateProvider, TemplateProvider>();
        services.AddScoped<IPlaceholderProcessor, PlaceholderProcessor>();
        services.AddScoped<IContentControlProcessor, ContentControlProcessor>();
        services.AddScoped<GrammarRulesBuilder>();

        // Register all table generators (Strategy Pattern)
        services.AddScoped<ITableGenerator, OmgangTableGenerator>();
        services.AddScoped<ITableGenerator, ZorgTableGenerator>(); // Handles ALL zorg categories including vakanties & feestdagen
        services.AddScoped<ITableGenerator, ChildrenListGenerator>();
        services.AddScoped<ITableGenerator, AlimentatieTableGenerator>();
    })

    .Build();

host.Run();