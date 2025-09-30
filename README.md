# Scheidingsdesk Document Generator

Een Azure Functions applicatie voor het automatisch genereren van juridische documenten voor scheidingszaken, specifiek gericht op ouderschapsplannen. Dit systeem integreert met een SQL database en Azure Blob Storage om op maat gemaakte Word documenten te genereren op basis van dossiergegevens.

## Inhoudsopgave

1. [Overzicht](#overzicht)
2. [Functionaliteiten](#functionaliteiten)
3. [Architectuur](#architectuur)
4. [API Endpoints](#api-endpoints)
5. [Data Models](#data-models)
6. [Development Setup](#development-setup)
7. [Deployment](#deployment)
8. [Testing](#testing)
9. [Troubleshooting](#troubleshooting)

## Overzicht

De Scheidingsdesk Document Generator is een serverless applicatie gebouwd met Azure Functions v4 (.NET 9, isolated worker model) die Word documenten genereert voor scheidingszaken. Het systeem haalt data op uit een SQL database, downloadt een template vanuit Azure Blob Storage, en vult deze met gepersonaliseerde informatie zoals:

- Persoonsgegevens van beide partijen
- Kindergegevens inclusief leeftijden en namen
- Omgangsregelingen (visitation schedules)
- Zorgregelingen (care arrangements)
- Alimentatie informatie
- Vakantie- en feestdagenregelingen

## Functionaliteiten

### Hoofd Features

1. **Ouderschapsplan Generatie** (`/api/ouderschapsplan`)
   - Genereert complete ouderschapsplannen op basis van dossier ID
   - Haalt alle benodigde data op uit SQL database
   - Vult Word template met gepersonaliseerde informatie
   - Genereert dynamische tabellen voor omgang en zorg

2. **Document Processing** (`/api/process`)
   - Verwijdert content controls uit Word documenten
   - Verwerkt placeholder teksten
   - Zorgt voor correcte tekstopmaak

3. **Grammatica Regels**
   - Automatische aanpassing van enkelvoud/meervoud op basis van aantal kinderen
   - Geslachtsspecifieke voornaamwoorden (hij/zij/hen)
   - Nederlandse taalregels voor lijsten ("en" tussen laatste twee items)

4. **Dynamische Tabellen**
   - Omgangstabellen per week regeling met dagindeling
   - Zorgtabellen per categorie
   - Vakantieregelingen (voorjaar, mei, zomer, herfst, kerst)
   - Feestdagenregelingen (Pasen, Koningsdag, Sinterklaas, etc.)

5. **Template Placeholder Systemen**
   - Ondersteunt meerdere placeholder formaten: `[[Key]]`, `{Key}`, `<<Key>>`, `[Key]`
   - Dynamische vervanging van persoons-, kind- en dossiergegevens
   - Speciale placeholders voor tabellen en lijsten

## Architectuur

### Project Structuur

```
/scheidingsdesk-document-generator/
├── Models/                          # Data modellen
│   ├── DossierData.cs              # Hoofdmodel voor dossier
│   ├── PersonData.cs               # Persoonsgegevens (partijen)
│   ├── ChildData.cs                # Kindergegevens
│   └── OuderschapsplanInfoData.cs  # Ouderschapsplan specifieke info
├── Services/                        # Business logic services
│   └── DatabaseService.cs          # Database interactie
├── Ouderschapsplan/                 # Templates
│   └── Ouderschapsplan NIEUW.docx  # Word template
├── OuderschapsplanFunction.cs      # Hoofdfunctie voor ouderschapsplan
├── ProcessDocumentFunction.cs      # Document processing functie
├── DocumentProcessor.cs            # Core document processing logic
├── RemoveContentControls.cs        # Legacy content control removal
├── HealthCheckFunction.cs          # Health check endpoint
├── Program.cs                      # Host configuratie en DI setup
├── host.json                       # Azure Functions configuratie
├── local.settings.json             # Lokale development settings
└── scheidingsdesk-document-generator.csproj  # Project file
```

### Technologie Stack

- **.NET 9.0** - Runtime framework
- **Azure Functions v4** - Serverless computing platform
- **DocumentFormat.OpenXml 3.0.2** - Word document manipulatie
- **Microsoft.Data.SqlClient 5.2.1** - SQL database connectiviteit
- **Microsoft.ApplicationInsights** - Monitoring en logging
- **Newtonsoft.Json 13.0.3** - JSON parsing

### Data Flow

```
1. HTTP Request (POST /api/ouderschapsplan)
   ↓
2. OuderschapsplanFunction ontvangt DossierId
   ↓
3. DatabaseService haalt data op uit SQL database
   - Dossier informatie
   - Partijen (rol_id 1 en 2)
   - Kinderen met parent relaties
   - Omgang (visitation arrangements)
   - Zorg (care arrangements)
   - Alimentatie (optioneel)
   - Ouderschapsplan info (optioneel)
   ↓
4. Template wordt gedownload van Azure Blob Storage
   ↓
5. Document generatie proces:
   - Grammatica regels aanmaken op basis van aantal kinderen
   - Alle placeholders vervangen met data
   - Dynamische tabellen genereren
   - Headers en footers verwerken
   ↓
6. Gegenereerd document wordt teruggegeven als file download
```

## API Endpoints

### 1. Ouderschapsplan Generatie

**Endpoint**: `POST /api/ouderschapsplan`
**Authorization**: Function key vereist
**Content-Type**: `application/json`

#### Request Body

```json
{
  "DossierId": 123
}
```

#### Response

- **Success (200)**: Returns Word document
  - Content-Type: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
  - Filename: `Ouderschapsplan_Dossier_{DossierId}_{yyyyMMdd}.docx`

- **Error (400/500)**: Returns JSON error
  ```json
  {
    "error": "Error message",
    "correlationId": "unique-tracking-id",
    "details": "Additional information"
  }
  ```

#### Voorbeeld cURL Request

```bash
curl -X POST https://your-function-app.azurewebsites.net/api/ouderschapsplan \
  -H "Content-Type: application/json" \
  -H "x-functions-key: your-function-key" \
  -d '{"DossierId": 123}' \
  -o ouderschapsplan.docx
```

### 2. Document Processing

**Endpoint**: `POST /api/process`
**Authorization**: Function key vereist
**Content-Type**: `multipart/form-data` of binary Word document

#### Request

Upload een Word document als:
- Form data met field name `document` of `file`
- Direct binaire upload in request body

#### Response

Processed Word document met verwijderde content controls.

### 3. Health Check

**Endpoint**: `GET /api/health`
**Authorization**: Function key vereist

Retourneert status informatie van de applicatie.

## Data Models

### DossierData

Hoofdmodel dat alle informatie over een scheidingsdossier bevat.

```csharp
public class DossierData
{
    public int Id { get; set; }
    public string DossierNummer { get; set; }
    public DateTime AangemaaktOp { get; set; }
    public DateTime GewijzigdOp { get; set; }
    public string Status { get; set; }
    public int GebruikerId { get; set; }
    public bool? IsAnoniem { get; set; }

    // Collections
    public List<PersonData> Partijen { get; set; }      // rol_id 1 en 2
    public List<ChildData> Kinderen { get; set; }
    public List<OmgangData> Omgang { get; set; }
    public List<ZorgData> Zorg { get; set; }
    public AlimentatieData? Alimentatie { get; set; }
    public OuderschapsplanInfoData? OuderschapsplanInfo { get; set; }

    // Convenience properties
    public PersonData? Partij1 => Partijen.FirstOrDefault(p => p.RolId == 1);
    public PersonData? Partij2 => Partijen.FirstOrDefault(p => p.RolId == 2);
}
```

### PersonData

Persoonsgegevens voor partijen (ouders).

```csharp
public class PersonData
{
    public int Id { get; set; }
    public string? Voornamen { get; set; }
    public string? Roepnaam { get; set; }
    public string? Tussenvoegsel { get; set; }
    public string Achternaam { get; set; }
    public string? Adres { get; set; }
    public string? Postcode { get; set; }
    public string? Plaats { get; set; }
    public string? GeboortePlaats { get; set; }
    public DateTime? GeboorteDatum { get; set; }
    public string? Telefoon { get; set; }
    public string? Email { get; set; }
    public int? RolId { get; set; }          // 1 = Partij 1, 2 = Partij 2
    public string VolledigeNaam { get; }     // Computed property
}
```

### ChildData

Kindergegevens met berekende leeftijd.

```csharp
public class ChildData
{
    public int Id { get; set; }
    public string? Voornamen { get; set; }
    public string? Roepnaam { get; set; }
    public string? Achternaam { get; set; }
    public DateTime? GeboorteDatum { get; set; }
    public string? Geslacht { get; set; }

    // Computed properties
    public string VolledigeNaam { get; }
    public int? Leeftijd { get; }

    // Relations
    public List<ParentChildRelation> ParentRelations { get; set; }
}
```

### OmgangData

Omgangsregelingen (wie heeft het kind wanneer).

```csharp
public class OmgangData
{
    public int Id { get; set; }
    public int DagId { get; set; }              // 1-7 (Ma-Zo)
    public string? DagNaam { get; set; }
    public int DagdeelId { get; set; }          // 1=Ochtend, 2=Middag, 3=Avond, 4=Nacht
    public string? DagdeelNaam { get; set; }
    public int VerzorgerId { get; set; }        // Persoon ID
    public string? VerzorgerNaam { get; set; }
    public string? WisselTijd { get; set; }
    public int WeekRegelingId { get; set; }
    public string? WeekRegelingOmschrijving { get; set; }
    public string? WeekRegelingAnders { get; set; }  // Custom override

    public string EffectieveRegeling { get; }   // Returns WeekRegelingAnders or WeekRegelingOmschrijving
}
```

### ZorgData

Zorgregelingen en afspraken over de kinderen.

```csharp
public class ZorgData
{
    public int Id { get; set; }
    public int ZorgCategorieId { get; set; }
    public string? ZorgCategorieNaam { get; set; }   // Bijv. "Onderwijs", "Medisch"
    public int ZorgSituatieId { get; set; }
    public string? ZorgSituatieNaam { get; set; }
    public string Overeenkomst { get; set; }         // De gemaakte afspraak
    public string? SituatieAnders { get; set; }      // Custom override

    public string EffectieveSituatie { get; }        // Returns SituatieAnders or ZorgSituatieNaam
}
```

## Development Setup

### Prerequisites

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Azure Functions Core Tools v4** - [Installatie instructies](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- **Visual Studio 2022** of **VS Code** met C# extensie
- **Azure Storage Emulator** of **Azurite** voor lokale development
- **SQL Server** toegang (lokaal of Azure)

### Stap 1: Clone Repository

```bash
git clone <repository-url>
cd scheidingsdesk-document-generator
```

### Stap 2: Database Setup

De applicatie verwacht een SQL database met de volgende tabellen:

- `dossiers` - Hoofdtabel voor dossiers
- `personen` - Persoonsgegevens
- `dossiers_partijen` - Koppeling dossier <-> partijen (met rol_id)
- `dossiers_kinderen` - Koppeling dossier <-> kinderen
- `kinderen_ouders` - Parent-child relaties
- `omgang` - Omgangsregelingen
- `dagen` - Referentietabel dagen (Ma-Zo)
- `dagdelen` - Referentietabel dagdelen (Ochtend, Middag, Avond, Nacht)
- `week_regelingen` - Week regelingen
- `zorg` - Zorgregelingen
- `zorg_categorieen` - Zorgcategorieën (Onderwijs, Medisch, etc.)
- `zorg_situaties` - Zorgsituaties
- `alimentaties` - Alimentatie informatie (optioneel)
- `ouderschapsplan_info` - Ouderschapsplan specifieke info (optioneel)

Schema details zijn te vinden in `DatabaseService.cs` queries.

### Stap 3: Configuratie

Maak een `local.settings.json` bestand aan:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "your-app-insights-connection-string"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.database.windows.net;Database=your-database;User Id=your-user;Password=your-password;Encrypt=True;"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*"
  }
}
```

**Environment Variables** (kan ook in `local.settings.json` onder `Values`):

```json
{
  "TemplateStorageUrl": "https://yourstorageaccount.blob.core.windows.net/templates/Ouderschapsplan%20NIEUW.docx?sp=r&st=2024-01-01T00:00:00Z&se=2025-12-31T23:59:59Z&sv=2021-06-08&sr=b&sig=your-sas-token"
}
```

### Stap 4: Build en Run

```bash
# Restore NuGet packages
dotnet restore

# Build project
dotnet build

# Run Azure Function locally
func start
```

De applicatie draait nu op `http://localhost:7071`

### Stap 5: Test de Endpoints

**Health Check:**

```bash
curl http://localhost:7071/api/health
```

**Ouderschapsplan Generatie:**

```bash
curl -X POST http://localhost:7071/api/ouderschapsplan \
  -H "Content-Type: application/json" \
  -d '{"DossierId": 1}' \
  -o test_ouderschapsplan.docx
```

## Template Placeholders

De Word template ondersteunt de volgende placeholders (meerdere formaten worden herkend):

### Dossier Informatie

- `[[DossierNummer]]` - Dossier nummer
- `[[DossierDatum]]` - Aanmaak datum (dd-MM-yyyy)
- `[[HuidigeDatum]]` - Huidige datum (dd MMMM yyyy, Nederlands)
- `[[IsAnoniem]]` - Ja/Nee

### Partij Gegevens

**Partij 1:**
- `[[Partij1Naam]]` - Volledige naam
- `[[Partij1Voornaam]]` - Voornaam/voornamen
- `[[Partij1Roepnaam]]` - Roepnaam
- `[[Partij1Achternaam]]` - Achternaam
- `[[Partij1Adres]]` - Adres
- `[[Partij1Postcode]]` - Postcode
- `[[Partij1Plaats]]` - Plaats
- `[[Partij1Geboorteplaats]]` - Geboorteplaats
- `[[Partij1VolledigAdres]]` - Adres, Postcode Plaats
- `[[Partij1Geboortedatum]]` - Geboortedatum (dd-MM-yyyy)
- `[[Partij1Telefoon]]` - Telefoonnummer
- `[[Partij1Email]]` - Email adres

**Partij 2:** Zelfde structuur met `Partij2` prefix

### Kind Gegevens

**Per kind (1-based index):**
- `[[Kind1Naam]]` - Volledige naam
- `[[Kind1Voornaam]]` - Voornaam/voornamen
- `[[Kind1Roepnaam]]` - Roepnaam
- `[[Kind1Achternaam]]` - Achternaam
- `[[Kind1Geboortedatum]]` - Geboortedatum (dd-MM-yyyy)
- `[[Kind1Leeftijd]]` - Leeftijd in jaren
- `[[Kind1Geslacht]]` - Geslacht (M/V)

**Aggregaten:**
- `[[AantalKinderen]]` - Totaal aantal kinderen
- `[[AantalMinderjarigeKinderen]]` - Aantal kinderen < 18 jaar
- `[[KinderenNamen]]` - Lijst van voornamen (met "en")
- `[[KinderenRoepnamen]]` - Lijst van roepnamen (met "en")
- `[[RoepnamenMinderjarigeKinderen]]` - Lijst van roepnamen minderjarigen
- `[[KinderenVolledigeNamen]]` - Lijst van volledige namen (met "en")

### Ouderschapsplan Specifieke Info

- `[[SoortRelatie]]` - Type relatie (Gehuwd, Samenwonend, etc.)
- `[[SoortRelatieVerbreking]]` - Type verbreking
- `[[BetrokkenheidKind]]` - Betrokkenheid van het kind
- `[[Kiesplan]]` - Gekozen plan type
- `[[GezagPartij]]` - Wie heeft gezag (roepnaam)
- `[[WaOpNaamVan]]` - WA verzekering op naam van (roepnaam)
- `[[ZorgverzekeringOpNaamVan]]` - Zorgverzekering op naam van (roepnaam)
- `[[KinderbijslagOntvanger]]` - Wie ontvangt kinderbijslag (roepnaam of "Kinderrekening")
- `[[KeuzeDevices]]` - Afspraken over devices
- `[[Hoofdverblijf]]` - Hoofdverblijf regeling
- `[[Zorgverdeling]]` - Verdeling van zorg
- `[[OpvangKinderen]]` - Opvang regelingen
- `[[ParentingCoordinator]]` - Parenting coordinator info

### Grammatica Placeholders

Deze worden automatisch vervangen op basis van aantal minderjarige kinderen:

- `[[ons kind/onze kinderen]]` → "ons kind" of "onze kinderen"
- `[[heeft/hebben]]` → "heeft" of "hebben"
- `[[is/zijn]]` → "is" of "zijn"
- `[[verblijft/verblijven]]` → "verblijft" of "verblijven"
- `[[kan/kunnen]]` → "kan" of "kunnen"
- `[[zal/zullen]]` → "zal" of "zullen"
- `[[moet/moeten]]` → "moet" of "moeten"
- `[[wordt/worden]]` → "wordt" of "worden"
- `[[blijft/blijven]]` → "blijft" of "blijven"
- `[[gaat/gaan]]` → "gaat" of "gaan"
- `[[komt/komen]]` → "komt" of "komen"
- `[[hem/haar/hen]]` → "hem", "haar" of "hen" (gebaseerd op geslacht)
- `[[hij/zij/ze]]` → "hij", "zij" of "ze" (gebaseerd op geslacht)

### Dynamische Tabellen

Deze placeholders worden vervangen door dynamisch gegenereerde tabellen:

- `[[TABEL_OMGANG]]` - Genereert omgangstabellen per week regeling
- `[[TABEL_ZORG]]` - Genereert zorgtabellen per categorie
- `[[TABEL_VAKANTIES]]` - Genereert vakantieregelingen tabel
- `[[TABEL_FEESTDAGEN]]` - Genereert feestdagen tabel
- `[[LIJST_KINDEREN]]` - Genereert opsomming van kinderen met details

## Deployment

### Deploy naar Azure

**Stap 1: Azure Resources aanmaken**

```bash
# Resource group
az group create --name rg-scheidingsdesk --location westeurope

# Storage account (voor Azure Functions)
az storage account create \
  --name stscheidingsdesk \
  --resource-group rg-scheidingsdesk \
  --location westeurope \
  --sku Standard_LRS

# Application Insights
az monitor app-insights component create \
  --app ai-scheidingsdesk \
  --location westeurope \
  --resource-group rg-scheidingsdesk

# Function App
az functionapp create \
  --resource-group rg-scheidingsdesk \
  --consumption-plan-location westeurope \
  --runtime dotnet-isolated \
  --runtime-version 9.0 \
  --functions-version 4 \
  --name func-scheidingsdesk \
  --storage-account stscheidingsdesk \
  --app-insights ai-scheidingsdesk
```

**Stap 2: Configuratie Settings**

```bash
# Connection string
az functionapp config connection-string set \
  --name func-scheidingsdesk \
  --resource-group rg-scheidingsdesk \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=your-server.database.windows.net;Database=your-db;User Id=user;Password=pass;"

# Template URL met SAS token
az functionapp config appsettings set \
  --name func-scheidingsdesk \
  --resource-group rg-scheidingsdesk \
  --settings TemplateStorageUrl="https://yourstorage.blob.core.windows.net/templates/Ouderschapsplan%20NIEUW.docx?sp=r&st=2024-01-01&..."
```

**Stap 3: Deploy de applicatie**

```bash
# Via Azure Functions Core Tools
func azure functionapp publish func-scheidingsdesk

# Of via Visual Studio
# Right-click project → Publish → Select target: Azure → Function App (Windows)
```

### Continuous Deployment via GitHub Actions

Maak `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure Functions

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v2

    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '9.0.x'

    - name: Build
      run: dotnet build --configuration Release

    - name: Publish
      run: dotnet publish --configuration Release --output ./output

    - name: Deploy to Azure Functions
      uses: Azure/functions-action@v1
      with:
        app-name: func-scheidingsdesk
        package: ./output
        publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
```

## Testing

### Unit Tests

Het project gebruikt een test-driven approach. Voorbeeld test scenario's:

**DatabaseService Tests:**
```csharp
[Fact]
public async Task GetDossierDataAsync_ValidId_ReturnsCompleteData()
{
    // Arrange
    var service = new DatabaseService(configuration, logger);
    int dossierId = 1;

    // Act
    var result = await service.GetDossierDataAsync(dossierId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(dossierId, result.Id);
    Assert.NotEmpty(result.Partijen);
    Assert.NotEmpty(result.Kinderen);
}
```

### Integration Tests

Test met een echte database en template:

```bash
# Start function app
func start

# Test ouderschapsplan generatie
curl -X POST http://localhost:7071/api/ouderschapsplan \
  -H "Content-Type: application/json" \
  -d '{"DossierId": 1}' \
  -o test_output.docx

# Verificeer het gegenereerde document
```

### Postman Collection

Import `Scheidingsdesk.postman_collection.json` voor ready-to-use API tests.

## Troubleshooting

### Veel voorkomende problemen

#### 1. "Template not found" of "Failed to download template"

**Oorzaak**: Template URL is niet correct geconfigureerd of SAS token is verlopen.

**Oplossing**:
```bash
# Check environment variable
az functionapp config appsettings list --name func-scheidingsdesk --resource-group rg-scheidingsdesk | grep TemplateStorageUrl

# Update met nieuwe SAS token
az functionapp config appsettings set --name func-scheidingsdesk --resource-group rg-scheidingsdesk --settings TemplateStorageUrl="new-url-with-sas"
```

#### 2. "Database connection failed"

**Oorzaak**: Connection string is incorrect of firewall blokkeert toegang.

**Oplossing**:
```bash
# Voeg Azure Functions IP toe aan SQL firewall
az sql server firewall-rule create \
  --resource-group rg-scheidingsdesk \
  --server your-sql-server \
  --name AllowAzureFunctions \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

#### 3. "No data found for DossierId"

**Oorzaak**: Dossier bestaat niet in database of heeft geen gekoppelde data.

**Oplossing**:
- Verifieer dat dossier ID bestaat in `dossiers` tabel
- Check of er partijen zijn gekoppeld met `rol_id` 1 en 2
- Verifieer dat er kinderen gekoppeld zijn in `dossiers_kinderen`

#### 4. Placeholders worden niet vervangen

**Oorzaak**: Placeholder formaat komt niet overeen of data ontbreekt.

**Oplossing**:
- Check of placeholders correct gespeld zijn in template
- Gebruik ondersteunde formaten: `[[Key]]`, `{Key}`, `<<Key>>`, `[Key]`
- Verifieer dat data beschikbaar is in database
- Check logs voor specifieke fouten: Application Insights → Logs

#### 5. Tabel generatie werkt niet

**Oorzaak**: Placeholder staat niet op eigen regel of data ontbreekt.

**Oplossing**:
- Zorg dat tabel placeholders (`[[TABEL_OMGANG]]`, etc.) op een eigen paragraph staan
- Verifieer dat omgang/zorg data aanwezig is in database
- Check dat `DagId` waarden tussen 1-7 liggen en `DagdeelId` tussen 1-4

### Logging en Monitoring

**Lokaal:**
```bash
# Verbose logging
export LOGGING__LOGLEVEL__DEFAULT=Debug
func start
```

**Azure - Application Insights Queries:**

```kusto
// Recent errors
traces
| where severityLevel >= 3
| where timestamp > ago(1h)
| order by timestamp desc
| project timestamp, message, severityLevel, customDimensions.CorrelationId

// Performance metrics
requests
| where timestamp > ago(24h)
| summarize
    avg(duration),
    percentile(duration, 95),
    count()
  by name
| order by avg_duration desc

// Dossier processing success rate
requests
| where name == "Ouderschapsplan"
| summarize
    Total = count(),
    Success = countif(success == true),
    Failed = countif(success == false)
| extend SuccessRate = (Success * 100.0) / Total
```

### Support Contact

Voor verdere hulp:
- Check Application Insights logs met correlation ID
- Raadpleeg database voor data consistentie
- Verifieer Azure resources configuratie

## Security & Best Practices

### Security Considerations

1. **Function Key Authentication**: Alle endpoints zijn beveiligd met function keys
2. **SQL Injection Prevention**: Gebruik van parameterized queries
3. **Secrets Management**: Connection strings en SAS tokens via Azure App Settings
4. **No Data Persistence**: Alle verwerking gebeurt in-memory
5. **Minimal Logging**: Geen gevoelige data wordt gelogd

### Performance Best Practices

1. **Database Connection Pooling**: Enabled by default in SqlClient
2. **Streaming**: Large documents worden gestreamd (niet volledig in geheugen)
3. **Caching**: Template caching zou geïmplementeerd kunnen worden
4. **Batch Processing**: Multi-resultset queries verminderen database roundtrips

### Code Quality

- **Error Handling**: Comprehensive try-catch met specifieke exception types
- **Logging**: Structured logging met correlation IDs
- **Code Documentation**: XML comments op alle publieke methods
- **Separation of Concerns**: Duidelijke scheiding tussen layers (Controllers, Services, Models)

## Toekomstige Verbeteringen

Mogelijke verbeteringen voor volgende versies:

1. **Template Caching**: Cache templates in memory om downloads te verminderen
2. **Queue Processing**: Gebruik Azure Storage Queues voor lange-lopende operations
3. **Batch Generation**: Genereer meerdere documenten in één request
4. **PDF Export**: Optie om direct naar PDF te converteren
5. **Email Integration**: Automatisch versturen van gegenereerde documenten
6. **Webhook Support**: Notificaties bij voltooiing van document generatie
7. **Versioning**: Template versie beheer en fallback mechanisme
8. **Multi-language**: Ondersteuning voor meerdere talen

## License

Dit project is eigendom van Scheidingsdesk en bedoeld voor interne gebruik in het juridisch document automation systeem.

## Changelog

### v1.2.0 (Current)
- Toegevoegd: Ouderschapsplan functionaliteit
- Toegevoegd: Dynamische tabel generatie
- Toegevoegd: Grammatica regels systeem
- Toegevoegd: Alimentatie ondersteuning
- Verbeterd: Error handling en logging

### v1.1.0
- Toegevoegd: Document processing endpoint
- Verbeterd: Content control verwijdering
- Toegevoegd: Health check endpoint

### v1.0.0
- Initiele release
- Basis document processing functionaliteit