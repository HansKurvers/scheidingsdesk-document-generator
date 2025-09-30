# Scheidingsdesk Document Generator

Een Azure Functions applicatie voor het automatisch genereren van juridische documenten voor scheidingszaken, specifiek gericht op ouderschapsplannen. Dit systeem integreert met een SQL database en Azure Blob Storage om op maat gemaakte Word documenten te genereren op basis van dossiergegevens.

## Inhoudsopgave

1. [Overzicht](#overzicht)
2. [Functionaliteiten](#functionaliteiten)
3. [Architectuur](#architectuur)
4. [API Endpoints](#api-endpoints)
5. [Data Models](#data-models)
6. [Development Setup](#development-setup)
7. [Template Placeholders](#template-placeholders)
8. [Deployment](#deployment)
9. [Testing](#testing)
10. [Troubleshooting](#troubleshooting)

## Overzicht

De Scheidingsdesk Document Generator is een serverless applicatie gebouwd met Azure Functions v4 (.NET 9, isolated worker model) die Word documenten genereert voor scheidingszaken. Het systeem haalt data op uit een SQL database, downloadt een template vanuit Azure Blob Storage, en vult deze met gepersonaliseerde informatie zoals:

- Persoonsgegevens van beide partijen
- Kindergegevens inclusief leeftijden en namen
- Omgangsregelingen (visitation schedules)
- Zorgregelingen (care arrangements)
- Alimentatie informatie
- Vakantie- en feestdagenregelingen

**Belangrijke refactoring (v2.0.0)**: Het systeem is volledig gerefactored volgens SOLID principes en DRY. De oorspronkelijke monolithische functie (1669 regels) is opgesplitst in 18 modulaire, herbruikbare services en generators, wat heeft geleid tot een **91.5% code reductie** in het endpoint zelf (142 regels).

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

### Belangrijke Design Principes

Deze applicatie is gebouwd met de volgende principes in gedachten:

- **SOLID Principes**: Elke class heeft één verantwoordelijkheid, services zijn open voor extensie maar gesloten voor modificatie
- **DRY (Don't Repeat Yourself)**: Code duplicatie is geëlimineerd door herbruikbare helpers en services
- **Strategy Pattern**: Nieuwe tabel generators kunnen worden toegevoegd zonder bestaande code te wijzigen
- **Dependency Injection**: Alle dependencies worden geïnjecteerd, wat testbaarheid en onderhoudbaarheid verbetert
- **Separation of Concerns**: Duidelijke scheiding tussen endpoints, orchestrators, processors, generators en helpers

### Project Structuur

```
/scheidingsdesk-document-generator/
├── Models/                                      # Data modellen
│   ├── DossierData.cs                          # Hoofdmodel voor dossier
│   ├── PersonData.cs                           # Persoonsgegevens (partijen)
│   ├── ChildData.cs                            # Kindergegevens
│   ├── OmgangData.cs                           # Omgangsregelingen
│   ├── ZorgData.cs                             # Zorgregelingen
│   ├── AlimentatieData.cs                      # Alimentatie informatie
│   └── OuderschapsplanInfoData.cs              # Ouderschapsplan specifieke info
│
├── Services/                                    # Business logic services
│   ├── DatabaseService.cs                      # Database interactie (SQL queries)
│   │
│   └── DocumentGeneration/                     # Document generatie module
│       ├── DocumentGenerationService.cs        # 🎯 ORCHESTRATOR - coördineert alle stappen
│       ├── IDocumentGenerationService.cs
│       ├── TemplateProvider.cs                 # Template download van Azure Blob
│       ├── ITemplateProvider.cs
│       │
│       ├── Helpers/                            # Herbruikbare utility classes
│       │   ├── DutchLanguageHelper.cs          # 🇳🇱 Nederlandse grammatica regels
│       │   ├── DataFormatter.cs                # 📝 Data formatting (datums, namen, adressen)
│       │   ├── OpenXmlHelper.cs                # 📄 Word document element creatie
│       │   └── GrammarRulesBuilder.cs          # 🔤 Grammar rules op basis van kinderen
│       │
│       ├── Processors/                         # Document verwerking
│       │   ├── PlaceholderProcessor.cs         # Vervangt placeholders in document
│       │   ├── IPlaceholderProcessor.cs
│       │   ├── ContentControlProcessor.cs      # Verwerkt content controls en tabel placeholders
│       │   └── IContentControlProcessor.cs
│       │
│       └── Generators/                         # Strategy Pattern: Tabel generators
│           ├── ITableGenerator.cs              # Interface voor alle generators
│           ├── OmgangTableGenerator.cs         # 📅 Omgangstabellen (visitation)
│           ├── ZorgTableGenerator.cs           # 🏥 Zorgtabellen (care, vakanties, feestdagen)
│           └── ChildrenListGenerator.cs        # 👶 Kinderen lijst generatie
│
├── OuderschapsplanFunction.cs                   # ✨ HTTP Endpoint (142 regels, was 1669)
├── OuderschapsplanFunction.cs.OLD               # 📦 Backup van originele versie
├── ProcessDocumentFunction.cs                   # Document processing endpoint
├── HealthCheckFunction.cs                       # Health check endpoint
├── Program.cs                                   # 🔧 DI configuratie en host setup
├── host.json                                    # Azure Functions configuratie
├── local.settings.json                          # Lokale development settings
└── scheidingsdesk-document-generator.csproj     # Project file
```

### Technologie Stack

- **.NET 9.0** - Runtime framework
- **Azure Functions v4** - Serverless computing platform (isolated worker model)
- **DocumentFormat.OpenXml 3.0.2** - Word document manipulatie
- **Microsoft.Data.SqlClient 5.2.1** - SQL database connectiviteit
- **Microsoft.ApplicationInsights** - Monitoring en logging
- **Newtonsoft.Json 13.0.3** - JSON parsing

### Data Flow

```
1. HTTP Request (POST /api/ouderschapsplan)
   ↓
2. OuderschapsplanFunction (Endpoint)
   - Valideert request
   - Genereert correlation ID voor tracking
   ↓
3. DocumentGenerationService (Orchestrator) coördineert:
   ↓
   ├─→ TemplateProvider
   │   └─→ Download template van Azure Blob Storage
   │
   ├─→ DatabaseService
   │   └─→ Haalt dossier data op (partijen, kinderen, omgang, zorg)
   │
   ├─→ GrammarRulesBuilder
   │   └─→ Bouwt Nederlandse grammatica regels op basis van kinderen
   │
   ├─→ PlaceholderProcessor
   │   └─→ Bouwt alle placeholder vervangingen (500+ placeholders)
   │
   └─→ Document Processing:
       │
       ├─→ PlaceholderProcessor.ProcessDocument()
       │   └─→ Vervangt alle tekst placeholders in body, headers, footers
       │
       ├─→ ContentControlProcessor.ProcessTablePlaceholders()
       │   └─→ Gebruikt Strategy Pattern voor tabel generatie:
       │       ├─→ OmgangTableGenerator
       │       ├─→ ZorgTableGenerator (handelt alle zorg categorieën af)
       │       └─→ ChildrenListGenerator
       │
       └─→ ContentControlProcessor.RemoveContentControls()
           └─→ Verwijdert Word content controls, behoudt content
   ↓
4. Gegenereerd document wordt teruggegeven als file download
```

### Service Layer Uitleg

#### 🎯 Orchestrator

**DocumentGenerationService** - De hoofdorchestrator die alle stappen coördineert:
```csharp
public async Task<Stream> GenerateDocumentAsync(int dossierId, string templateUrl, string correlationId)
{
    // Step 1: Download template
    var templateBytes = await _templateProvider.GetTemplateAsync(templateUrl);

    // Step 2: Get dossier data
    var dossierData = await _databaseService.GetDossierDataAsync(dossierId);

    // Step 3: Build grammar rules
    var grammarRules = _grammarRulesBuilder.BuildRules(dossierData.Kinderen, correlationId);

    // Step 4: Build placeholder replacements
    var replacements = _placeholderProcessor.BuildReplacements(dossierData, grammarRules);

    // Step 5: Process document
    return await ProcessDocumentAsync(templateBytes, dossierData, replacements, correlationId);
}
```

#### 📝 Helpers (Stateless Utilities)

Deze helpers bevatten geen state en bieden herbruikbare functionaliteit:

1. **DutchLanguageHelper** - Nederlandse taalregels
   - Lijsten formatteren met "en" tussen laatste items
   - Voornaamwoorden (hij/zij/hen, hem/haar/hen)
   - Werkwoorden in enkelvoud/meervoud (heeft/hebben, is/zijn, etc.)

2. **DataFormatter** - Data formatting
   - Datums (dd-MM-yyyy, Nederlandse lange datum)
   - Namen (voornaam + tussenvoegsel + achternaam)
   - Adressen, valuta, conversies

3. **OpenXmlHelper** - Word document elementen
   - Gestylede tabel cellen en headers
   - Tabellen met borders en kleuren
   - Paragrafen en headings

4. **GrammarRulesBuilder** (Injectable service)
   - Bouwt grammatica regels op basis van kinderen data
   - Bepaalt enkelvoud/meervoud aan de hand van minderjarige kinderen

#### 🔄 Processors

1. **PlaceholderProcessor** - Vervangt alle text placeholders
   - Bouwt dictionary met 500+ placeholders
   - Verwerkt body, headers, footers
   - Ondersteunt 4 formaten: `[[Key]]`, `{Key}`, `<<Key>>`, `[Key]`

2. **ContentControlProcessor** - Verwerkt speciale content
   - Gebruikt Strategy Pattern voor tabel generators
   - Verwijdert Word content controls
   - Behoudt en fix formatting van content

#### 🏭 Generators (Strategy Pattern)

Elke generator implementeert `ITableGenerator`:

```csharp
public interface ITableGenerator
{
    string PlaceholderTag { get; }  // Bijv. "[[TABEL_OMGANG]]"
    List<OpenXmlElement> Generate(DossierData data, string correlationId);
}
```

**Voordelen van dit pattern:**
- ✅ Nieuwe tabel types toevoegen zonder bestaande code te wijzigen
- ✅ Elke generator is onafhankelijk testbaar
- ✅ Duidelijke scheiding van verantwoordelijkheden
- ✅ Makkelijk te onderhouden en uitbreiden

**Beschikbare generators:**
1. **OmgangTableGenerator** - Genereert omgangstabellen per week regeling
2. **ZorgTableGenerator** - Genereert zorgtabellen per categorie (inclusief vakanties, feestdagen, en alle andere zorg categorieën uit de database)
3. **ChildrenListGenerator** - Genereert bullet-point lijst van kinderen

### Dependency Injection Setup

Alle services worden geregistreerd in `Program.cs`:

```csharp
// Core services
services.AddSingleton<DatabaseService>();

// Document generation services
services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
services.AddScoped<ITemplateProvider, TemplateProvider>();
services.AddScoped<IPlaceholderProcessor, PlaceholderProcessor>();
services.AddScoped<IContentControlProcessor, ContentControlProcessor>();
services.AddScoped<GrammarRulesBuilder>();

// Table generators (Strategy Pattern)
services.AddScoped<ITableGenerator, OmgangTableGenerator>();
services.AddScoped<ITableGenerator, ZorgTableGenerator>(); // Handles ALL zorg categories including vakanties & feestdagen
services.AddScoped<ITableGenerator, ChildrenListGenerator>();
```

**Waarom deze opzet?**
- ✅ **Testbaarheid**: Elke service kan gemockt worden voor unit tests
- ✅ **Herbruikbaarheid**: Services kunnen worden gebruikt in andere functies/endpoints
- ✅ **Onderhoudbaarheid**: Wijzigingen in één service beïnvloeden andere niet
- ✅ **Uitbreidbaarheid**: Nieuwe features toevoegen is simpel (bijv. nieuwe tabel generator)

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
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "your-app-insights-connection-string",
    "TemplateStorageUrl": "https://yourstorageaccount.blob.core.windows.net/templates/Ouderschapsplan%20NIEUW.docx?sp=r&st=2024-01-01T00:00:00Z&se=2025-12-31T23:59:59Z&sv=2021-06-08&sr=b&sig=your-sas-token"
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

**Belangrijke configuratie variabelen:**

- `TemplateStorageUrl`: Volledige URL naar Word template in Azure Blob Storage met SAS token
- `DefaultConnection`: SQL Server connection string
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Voor logging en monitoring

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

### Development Tips voor Junior Developers

#### Code Toevoegen: Nieuwe Tabel Generator

Wil je een nieuwe tabel type toevoegen? Volg deze stappen:

1. **Create nieuwe generator class** in `Services/DocumentGeneration/Generators/`

```csharp
public class MijnNieuweTabelGenerator : ITableGenerator
{
    private readonly ILogger<MijnNieuweTabelGenerator> _logger;

    public string PlaceholderTag => "[[TABEL_MIJN_NIEUWE]]";

    public MijnNieuweTabelGenerator(ILogger<MijnNieuweTabelGenerator> logger)
    {
        _logger = logger;
    }

    public List<OpenXmlElement> Generate(DossierData data, string correlationId)
    {
        var elements = new List<OpenXmlElement>();

        // Gebruik OpenXmlHelper voor tabel creatie
        var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.Blue);

        // Add header row
        var headerRow = OpenXmlHelper.CreateHeaderRow(
            new[] { "Kolom 1", "Kolom 2" },
            OpenXmlHelper.Colors.Blue,
            OpenXmlHelper.Colors.White
        );
        table.Append(headerRow);

        // Add data rows
        // ... jouw logica hier ...

        elements.Add(table);
        return elements;
    }
}
```

2. **Registreer in Program.cs**

```csharp
services.AddScoped<ITableGenerator, MijnNieuweTabelGenerator>();
```

3. **Gebruik in template**

Voeg `[[TABEL_MIJN_NIEUWE]]` toe op een eigen regel in je Word template.

**Dat is alles!** Je hoeft geen bestaande code te wijzigen. Het Strategy Pattern zorgt ervoor dat je generator automatisch wordt gebruikt.

#### Debuggen

**Lokaal debuggen in Visual Studio:**
1. Zet breakpoints in de service die je wilt debuggen
2. Start met F5 (Debug mode)
3. Gebruik cURL of Postman om request te sturen
4. Code stopt bij je breakpoint

**Logs bekijken:**
- Lokaal: Logs verschijnen in console waar `func start` draait
- Azure: Gebruik Application Insights in Azure Portal

**Correlation IDs:**
Elke request krijgt een unique `correlationId` die door alle logs loopt. Dit maakt het makkelijk om één specifieke request te volgen:

```kusto
traces
| where customDimensions.CorrelationId == "de-correlation-id-van-je-request"
| order by timestamp asc
```

#### Code Lezen Tips

Als je nieuwe code leest, volg de flow:

1. **Start bij endpoint** (`OuderschapsplanFunction.cs`)
2. **Ga naar orchestrator** (`DocumentGenerationService.cs`)
3. **Bekijk welke services worden aangeroepen** (TemplateProvider, DatabaseService, etc.)
4. **Duik in specifieke processors/generators** als je details wilt weten

**Handige shortcuts:**
- Ctrl+Click (VS Code) of F12 (Visual Studio) op een method om naar definitie te gaan
- Shift+F12 om alle plekken te vinden waar een method wordt gebruikt

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

Deze worden automatisch vervangen op basis van aantal minderjarige kinderen en geslacht:

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

**Hoe werkt dit?** De `GrammarRulesBuilder` analyseert de kinderen data en bouwt automatisch de juiste vervoegingen. Als je 1 kind hebt wordt het enkelvoud gebruikt, bij meerdere kinderen meervoud.

### Dynamische Tabellen

Deze placeholders worden vervangen door dynamisch gegenereerde tabellen:

- `[[TABEL_OMGANG]]` - Genereert omgangstabellen per week regeling uit database
- `[[TABEL_ZORG]]` - Genereert zorgtabellen per categorie uit database (inclusief vakanties, feestdagen, bijzondere dagen, beslissingen, etc.)
- `[[LIJST_KINDEREN]]` - Genereert opsomming van kinderen met details

**Let op:** Deze placeholders moeten op een eigen paragraph staan (niet inline in een zin).

**Belangrijke opmerking over Vakanties & Feestdagen:**
Vakanties en feestdagen worden automatisch gegenereerd als onderdeel van `[[TABEL_ZORG]]`. In de database zitten deze als zorg categorieën met namen zoals "Vakanties", "Feestdagen", etc. Er zijn geen aparte placeholders `[[TABEL_VAKANTIES]]` of `[[TABEL_FEESTDAGEN]]` nodig.

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

Het project is ontworpen voor testbaarheid door dependency injection en scheiding van concerns.

**Voorbeeld test voor PlaceholderProcessor:**

```csharp
[Fact]
public void BuildReplacements_WithValidData_ReturnsAllPlaceholders()
{
    // Arrange
    var processor = new PlaceholderProcessor(mockLogger.Object);
    var dossierData = CreateTestDossierData();
    var grammarRules = new Dictionary<string, string>();

    // Act
    var result = processor.BuildReplacements(dossierData, grammarRules);

    // Assert
    Assert.NotEmpty(result);
    Assert.True(result.ContainsKey("DossierNummer"));
    Assert.True(result.ContainsKey("Partij1Naam"));
}
```

**Voorbeeld test voor TableGenerator:**

```csharp
[Fact]
public void Generate_WithValidOmgangData_CreatesTable()
{
    // Arrange
    var generator = new OmgangTableGenerator(mockLogger.Object);
    var dossierData = CreateTestDossierData();

    // Act
    var elements = generator.Generate(dossierData, "test-correlation-id");

    // Assert
    Assert.NotEmpty(elements);
    Assert.Contains(elements, e => e is Table);
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

### Mocking Services

**DatabaseService mocken:**

```csharp
var mockDatabaseService = new Mock<DatabaseService>();
mockDatabaseService
    .Setup(x => x.GetDossierDataAsync(It.IsAny<int>()))
    .ReturnsAsync(testDossierData);
```

**TemplateProvider mocken:**

```csharp
var mockTemplateProvider = new Mock<ITemplateProvider>();
mockTemplateProvider
    .Setup(x => x.GetTemplateAsync(It.IsAny<string>()))
    .ReturnsAsync(templateBytes);
```

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

#### 6. Service not found / Dependency injection error

**Oorzaak**: Service is niet geregistreerd in `Program.cs`

**Oplossing**:
- Check of alle services geregistreerd zijn in `Program.cs`
- Verify dat interfaces correct zijn geïmplementeerd
- Bij nieuwe generator: check of `ITableGenerator` implementatie is geregistreerd

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

// Follow een specifieke request
traces
| where customDimensions.CorrelationId == "jouw-correlation-id"
| order by timestamp asc
| project timestamp, message, severityLevel
```

### Debug Tips

**Stap-voor-stap debuggen van document generatie:**

1. **Check template download**
   ```
   Zoek in logs naar: "Downloading template from Azure Storage"
   Verify: "Template downloaded successfully. Size: X bytes"
   ```

2. **Check database data**
   ```
   Zoek naar: "Step 2: Retrieving dossier data"
   Verify data is opgehaald voor partijen, kinderen, omgang, zorg
   ```

3. **Check placeholder vervanging**
   ```
   Zoek naar: "Built X placeholder replacements"
   Zoek naar: "Processing placeholders"
   ```

4. **Check tabel generatie**
   ```
   Zoek naar: "Found placeholder: [[TABEL_OMGANG]]"
   Zoek naar: "Generated omgang table"
   ```

5. **Check content control removal**
   ```
   Zoek naar: "Removing X content controls"
   Zoek naar: "Content controls removal completed"
   ```

## Security & Best Practices

### Security Considerations

1. **Function Key Authentication**: Alle endpoints zijn beveiligd met function keys
2. **SQL Injection Prevention**: Gebruik van parameterized queries
3. **Secrets Management**: Connection strings en SAS tokens via Azure App Settings (niet in code!)
4. **No Data Persistence**: Alle verwerking gebeurt in-memory, geen tijdelijke bestanden
5. **Minimal Logging**: Geen gevoelige data (persoonsgegevens) wordt gelogd

### Performance Best Practices

1. **Database Connection Pooling**: Enabled by default in SqlClient
2. **Streaming**: Documents worden gestreamd (niet volledig in geheugen geladen)
3. **Async/Await**: Alle I/O operaties zijn async voor betere schaalbaarheid
4. **Scoped Services**: Services worden per request aangemaakt en disposed

### Code Quality

- **SOLID Principes**: Toegepast in hele architectuur
- **Error Handling**: Comprehensive try-catch met specifieke exception types
- **Logging**: Structured logging met correlation IDs voor traceability
- **Code Documentation**: XML comments op alle publieke methods en classes
- **Separation of Concerns**: Duidelijke scheiding tussen endpoints, orchestrators, processors, generators en helpers

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
9. **Unit Tests**: Volledige test coverage voor alle services en generators
10. **Nieuwe Document Types**: Andere juridische documenten (convenant, etc.)

**Hoe nieuwe document types toe te voegen:**

Dankzij de modulaire opzet kun je eenvoudig nieuwe document types toevoegen:

1. Maak een nieuwe functie (bijv. `ConvenantFunction.cs`)
2. Hergebruik `DocumentGenerationService` als orchestrator
3. Maak specifieke generators voor nieuwe tabel types
4. Voeg nieuwe placeholders toe in `PlaceholderProcessor`

Alle helpers (`DutchLanguageHelper`, `DataFormatter`, `OpenXmlHelper`) zijn volledig herbruikbaar!

## License

Dit project is eigendom van Scheidingsdesk en bedoeld voor interne gebruik in het juridisch document automation systeem.

## Changelog

### v2.0.0 (Current) - 🎉 Grote Refactoring

**Belangrijkste wijzigingen:**
- ♻️ **Volledige refactoring** volgens SOLID principes en DRY
- 📉 **91.5% code reductie** in endpoint (1669 → 142 regels)
- 🏗️ **18 nieuwe modulaire services** voor herbruikbaarheid
- 🎯 **Strategy Pattern** voor tabel generators
- 💉 **Dependency Injection** door hele applicatie
- 🧪 **Volledig testbaar** - alle services kunnen worden gemockt
- 📝 **Uitgebreide documentatie** voor junior developers

**Nieuwe architectuur:**
- DocumentGenerationService (Orchestrator)
- TemplateProvider (Azure Blob Storage)
- PlaceholderProcessor (Text replacement)
- ContentControlProcessor (Content controls & table placeholders)
- GrammarRulesBuilder (Dutch grammar)
- 3 Helpers: DutchLanguageHelper, DataFormatter, OpenXmlHelper
- 3 Generators: Omgang, Zorg (incl. vakanties/feestdagen), Children List

**Backwards Compatibility:**
- ✅ Exact zelfde API endpoint
- ✅ Zelfde request/response format
- ✅ Geen frontend wijzigingen nodig

**Breaking Changes:**
- Geen! De refactoring is 100% backwards compatible.

### v1.2.0

- Toegevoegd: Ouderschapsplan functionaliteit
- Toegevoegd: Dynamische tabel generatie
- Toegevoegd: Grammatica regels systeem
- Toegevoegd: Alimentatie ondersteuning
- Toegevoegd: Geboorteplaats veld
- Verbeterd: Error handling en logging

### v1.1.0

- Toegevoegd: Document processing endpoint
- Verbeterd: Content control verwijdering
- Toegevoegd: Health check endpoint

### v1.0.0

- Initiele release
- Basis document processing functionaliteit

---

**Voor vragen of hulp:** Check de troubleshooting sectie of bekijk Application Insights logs met correlation ID.

**Happy coding! 🚀**