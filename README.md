# Scheidingsdesk Document Generator

An Azure Functions application for processing legal documents (Dutch divorce agreements) that integrates with Power Automate workflows. This service processes Word documents populated with content controls, removes placeholder sections, renumbers articles, and produces clean final documents.

## Overview

This document processor is built with Azure Functions v4 (.NET 9, isolated worker model) and designed to work seamlessly with Power Automate workflows to automate the generation of legal documents. It handles:
- Processing Word documents with content controls
- Removing sections marked with special placeholders
- Intelligent article and sub-article renumbering
- Producing clean, professional documents ready for client delivery

## Features

- **Placeholder Processing**: Handles special markers in content controls:
  - `"#"` - Removes the specific paragraph containing this marker
  - `"^"` - Removes the entire article including all sub-articles
- **Smart Renumbering**: Automatically renumbers articles and sub-articles after removals
- **Content Control Removal**: Strips all content controls to create final documents
- **Power Automate Integration**: Designed for seamless integration with Power Automate flows
- **Error Handling**: Comprehensive error handling with correlation IDs for debugging
- **Performance Monitoring**: Application Insights integration for monitoring and debugging

## API Endpoints

### ProcessDocument
- **Route**: `POST /api/process`
- **Authorization**: Function key required
- **Content-Type**: `multipart/form-data` or `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- **Max File Size**: 50MB
- **Timeout**: 5 minutes

#### Request
Upload a Word document either as:
- Form data with field name `document` or `file`
- Direct binary upload in request body

#### Response Headers
- `X-Correlation-Id`: Unique ID for request tracking
- `X-Processing-Time-Ms`: Processing duration in milliseconds
- `X-Document-Size`: Output document size in bytes

#### Response Body
Returns the processed Word document with:
- Content-Type: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- Filename: `Processed_[original_filename].docx`

### RemoveContentControls (Legacy)
- **Route**: `POST /api/RemoveContentControls`
- **Purpose**: Legacy endpoint that only removes content controls without placeholder processing
- **Note**: Use `/api/process` for full document processing capabilities

## Document Processing Logic

### 1. Placeholder Detection
The processor scans all content controls in the document for special markers:
- Paragraphs containing `"#"` are marked for removal
- Articles containing `"^"` are marked for complete removal (including sub-articles)

### 2. Content Removal
- Individual paragraphs marked with `"#"` are removed
- Complete articles marked with `"^"` are removed along with all their sub-articles

### 3. Article Renumbering
After content removal, the processor:
- Identifies remaining articles (pattern: `"1. ARTICLE TITLE"`)
- Identifies sub-articles (pattern: `"   1.1 Sub article content"`)
- Renumbers articles sequentially (if article 2 is removed, article 3 becomes 2)
- Updates sub-article numbers to match their parent article

### 4. Content Control Removal
Finally, all content controls are removed while preserving their content:
- Text formatting is maintained
- Gray coloring from content controls is removed
- Text is set to black for professional appearance

## Development Setup

### Prerequisites
- .NET 9.0 SDK
- Azure Functions Core Tools v4
- Visual Studio 2022 or VS Code with C# extension
- Azure Storage Emulator or Azurite for local development

### Local Development
1. Clone the repository
2. Update `local.settings.json` with your configuration:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "APPLICATIONINSIGHTS_CONNECTION_STRING": "your-connection-string"
     }
   }
   ```
3. Run the function app:
   ```bash
   func start
   ```

### Testing with cURL
```bash
# Process a document
curl -X POST http://localhost:7071/api/process \
  -H "x-functions-key: your-function-key" \
  -F "document=@path/to/your/document.docx" \
  -o processed_document.docx
```

### Testing with PowerShell
```powershell
$headers = @{
    "x-functions-key" = "your-function-key"
}
$form = @{
    document = Get-Item -Path "path\to\your\document.docx"
}
Invoke-RestMethod -Uri "http://localhost:7071/api/process" `
    -Method Post -Headers $headers -Form $form `
    -OutFile "processed_document.docx"
```

## Power Automate Integration

### Sample Flow Configuration
1. **Trigger**: When a record is created/updated in Dataverse
2. **Action**: Populate a Word template
3. **Action**: HTTP POST to ProcessDocument function
   - Method: `POST`
   - URI: `https://your-function-app.azurewebsites.net/api/process`
   - Headers:
     - `x-functions-key`: `your-function-key`
   - Body: Output from "Populate a Word template" action
4. **Action**: Create file in SharePoint/OneDrive with function response

### Error Handling in Power Automate
The function returns structured error responses that can be parsed in Power Automate:
```json
{
  "error": "Error message",
  "correlationId": "unique-id-for-debugging",
  "details": "Additional error details (if available)"
}
```

## Deployment

### Deploy to Azure
1. Create an Azure Function App (Windows, .NET 9, Consumption plan)
2. Configure Application Insights
3. Deploy using your preferred method:
   ```bash
   # Using Azure Functions Core Tools
   func azure functionapp publish your-function-app-name
   
   # Using Visual Studio
   # Right-click project → Publish → Azure
   ```

### Configuration Settings
Required Azure Function App settings:
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Your App Insights connection string
- `AzureWebJobsStorage`: Storage account connection string

## Monitoring and Debugging

### Application Insights
The function automatically logs to Application Insights:
- Request/response tracking with correlation IDs
- Performance metrics
- Error details and stack traces
- Custom events for document processing steps

### Key Metrics to Monitor
- Request duration
- Success/failure rates
- Document sizes processed
- Error types and frequencies

### Debugging Tips
1. Use correlation IDs to track requests through logs
2. Check Application Insights for detailed error traces
3. Enable verbose logging in development
4. Test with sample documents before production deployment

### Common Issues

#### "Unexpected error processing document" on local development
If you encounter this error when testing locally:
1. Ensure you're using the latest Azure Functions Core Tools (v4)
2. Try testing with the health check endpoint first: `GET http://localhost:7071/api/health`
3. Use Postman or cURL instead of the Functions test interface
4. Check that your `local.settings.json` has all required settings
5. Ensure the request includes proper Content-Type headers

#### Testing with cURL (recommended for local testing)
```bash
# Test with form data
curl -X POST http://localhost:7071/api/process \
  -H "x-functions-key: test" \
  -F "document=@test.docx" \
  -o output.docx -v

# Test with binary upload
curl -X POST http://localhost:7071/api/process \
  -H "x-functions-key: test" \
  -H "Content-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document" \
  --data-binary @test.docx \
  -o output.docx -v
```

## Architecture

### Project Structure
```
/scheidingsdesk-document-generator/
├── ProcessDocumentFunction.cs      # Main processing endpoint
├── DocumentProcessor.cs            # Core document processing logic
├── RemoveContentControls.cs        # Legacy content control removal
├── Program.cs                      # Host configuration
├── host.json                       # Function app settings
├── local.settings.json            # Local development settings
├── scheidingsdesk-document-generator.csproj
└── README.md
```

### Dependencies
- **Microsoft.Azure.Functions.Worker**: Azure Functions runtime
- **DocumentFormat.OpenXml**: Word document manipulation
- **Microsoft.ApplicationInsights**: Monitoring and diagnostics
- **Microsoft.PowerPlatform.Dataverse.Client**: Dataverse integration

## Security Considerations

- Function key authentication required for all endpoints
- No temporary files created (all processing in memory)
- No sensitive data logged
- Correlation IDs for audit trails
- Input validation and size limits enforced

## Performance

- Optimized for documents up to 50MB
- Typical processing time: 1-5 seconds
- Concurrent request handling supported
- Memory-efficient stream processing

## Support and Contributing

For issues, feature requests, or contributions:
1. Check existing issues in the repository
2. Create detailed bug reports with correlation IDs
3. Include sample documents (sanitized) when reporting issues
4. Follow existing code style and patterns

## License

This project is part of the Scheidingsdesk legal document automation system.