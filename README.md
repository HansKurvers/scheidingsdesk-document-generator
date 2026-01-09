# i-docx Document Generator

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

De Ouderschapsplan Document Generator is een serverless applicatie gebouwd met Azure Functions v4 (.NET 9, isolated worker model) die Word documenten genereert voor scheidingszaken. Het systeem haalt data op uit een SQL database, downloadt een template vanuit Azure Blob Storage, en vult deze met gepersonaliseerde informatie zoals:

- Persoonsgegevens van beide partijen
- Kindergegevens inclusief leeftijden en namen
- Omgangsregelingen (visitation schedules)
- Zorgregelingen (care arrangements)
- Alimentatie informatie
- Vakantie- en feestdagenregelingen

**Belangrijke refactoring (v2.0.0)**: Het systeem is volledig gerefactored volgens SOLID principes en DRY. De oorspronkelijke monolithische functie (1669 regels) is opgesplitst in 18 modulaire, herbruikbare services en generators, wat heeft geleid tot een **91.5% code reductie** in het endpoint zelf (142 regels).

### Versie Compatibiliteit

| Frontend | API | Doc Generator | Status |
|----------|-----|---------------|--------|
| 1.4.x | 1.3.x | 2.4.x | ✅ Actueel |
| 1.3.x | 1.2.x | 2.3.x | ⚠️ Legacy |
| 1.2.x | 1.1.x | 2.2.x | ❌ Niet ondersteund |

> **Let op**: Zorg dat alle componenten compatibele versies draaien om onverwacht gedrag te voorkomen.

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

3. **Template Management** (Nieuw in v2.1.0)
   - **Get Template Types** (`/api/template-types`) - Haalt beschikbare template types op
   - **Get Templates by Type** (`/api/templates/{type}`) - Haalt templates op voor een specifiek type
   - Ondersteunt types: Feestdag, Vakantie, Algemeen, Bijzondere dag

4. **Grammatica Regels**
   - Automatische aanpassing van enkelvoud/meervoud op basis van aantal kinderen
   - Geslachtsspecifieke voornaamwoorden (hij/zij/hen)
   - Nederlandse taalregels voor lijsten ("en" tussen laatste twee items)

5. **Dynamische Tabellen**
   - Omgangstabellen per week regeling met dagindeling
   - Zorgtabellen per categorie
   - Vakantieregelingen (voorjaar, mei, zomer, herfst, kerst)
   - Feestdagenregelingen (Pasen, Koningsdag, Sinterklaas, etc.)

6. **Artikel Bibliotheek Integratie** (Nieuw in v2.3.0)
   - Haalt artikelen op uit database met 3-laags prioriteit (dossier > gebruiker > systeem)
   - Automatische conditionele filtering op basis van dossier data
   - Placeholder vervanging binnen artikel teksten
   - `[[ARTIKELEN]]` placeholder genereert alle actieve artikelen
   - Ondersteunt `[[IF:Veld]]...[[ENDIF:Veld]]` binnen artikelen

7. **Custom Placeholders Integratie** (Nieuw in v2.4.0)
   - Ondersteunt custom placeholders uit de placeholder_catalogus tabel
   - 3-laags waarde prioriteit: dossier > gebruiker > systeem standaardwaarde
   - Automatische integratie met PlaceholderProcessor
   - Werkt naadloos samen met bestaande systeem placeholders

8. **Template Placeholder Systemen**
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
/idocx-document-generator/
├── Models/                                      # Data modellen
│   ├── DossierData.cs                          # Hoofdmodel voor dossier
│   ├── PersonData.cs                           # Persoonsgegevens (partijen)
│   ├── ChildData.cs                            # Kindergegevens
│   ├── OmgangData.cs                           # Omgangsregelingen
│   ├── ZorgData.cs                             # Zorgregelingen
│   ├── AlimentatieData.cs                      # Alimentatie informatie
│   ├── OuderschapsplanInfoData.cs              # Ouderschapsplan specifieke info
│   └── ArtikelData.cs                          # Artikel bibliotheek data (v2.3.0)
│
├── Services/                                    # Business logic services
│   ├── DatabaseService.cs                      # Database interactie (SQL queries)
│   │
│   ├── Artikel/                                # Artikel bibliotheek services (v2.3.0)
│   │   ├── IArtikelService.cs                  # Interface voor artikel verwerking
│   │   └── ArtikelService.cs                   # Conditionele filtering en placeholder vervanging
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
│       │   ├── GrammarRulesBuilder.cs          # 🔤 Grammar rules op basis van kinderen
│       │   └── ArticleNumberingHelper.cs       # 🔢 Automatische artikelnummering
│       │
│       ├── Processors/                         # Document verwerking
│       │   ├── PlaceholderProcessor.cs         # Vervangt placeholders in document
│       │   ├── IPlaceholderProcessor.cs
│       │   ├── ContentControlProcessor.cs      # Verwerkt content controls en tabel placeholders
│       │   ├── IContentControlProcessor.cs
│       │   ├── ConditionalSectionProcessor.cs  # Verwerkt [[IF:]]...[[ENDIF:]] blokken
│       │   └── IConditionalSectionProcessor.cs
│       │
│       └── Generators/                         # Strategy Pattern: Tabel generators
│           ├── ITableGenerator.cs              # Interface voor alle generators
│           ├── OmgangTableGenerator.cs         # 📅 Omgangstabellen (visitation)
│           ├── ZorgTableGenerator.cs           # 🏥 Zorgtabellen (care, vakanties, feestdagen)
│           ├── ChildrenListGenerator.cs        # 👶 Kinderen lijst generatie
│           └── ArtikelContentGenerator.cs      # 📚 Artikelen uit bibliotheek (v2.3.0)
│
├── OuderschapsplanFunction.cs                   # ✨ HTTP Endpoint (142 regels, was 1669)
├── OuderschapsplanFunction.cs.OLD               # 📦 Backup van originele versie
├── ProcessDocumentFunction.cs                   # Document processing endpoint
├── HealthCheckFunction.cs                       # Health check endpoint
├── Program.cs                                   # 🔧 DI configuratie en host setup
├── host.json                                    # Azure Functions configuratie
├── local.settings.json                          # Lokale development settings
└── idocx-document-generator.csproj              # Project file
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
       ├─→ Step 1: PlaceholderProcessor.ProcessDocument()
       │   └─→ Vervangt alle tekst placeholders in body, headers, footers
       │
       ├─→ Step 2: ConditionalSectionProcessor.ProcessConditionalSections()
       │   └─→ Verwerkt [[IF:Veld]]...[[ENDIF:Veld]] blokken
       │       - Verwijdert blokken waar veld leeg is
       │       - Behoudt content en verwijdert alleen tags waar veld gevuld is
       │
       ├─→ Step 3: ArticleNumberingHelper.ProcessArticlePlaceholders()
       │   └─→ Vervangt [[ARTIKEL]] met oplopende nummers (Artikel 1, 2, 3...)
       │
       ├─→ Step 4: ContentControlProcessor.ProcessTablePlaceholders()
       │   └─→ Gebruikt Strategy Pattern voor tabel generatie:
       │       ├─→ OmgangTableGenerator
       │       ├─→ ZorgTableGenerator (handelt alle zorg categorieën af)
       │       └─→ ChildrenListGenerator
       │
       └─→ Step 5: ContentControlProcessor.RemoveContentControls()
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

5. **ArticleNumberingHelper** - Automatische artikelnummering
   - Vervangt `[[ARTIKEL]]` met "Artikel 1", "Artikel 2", etc.
   - Vervangt `[[ARTIKEL_NR]]` met alleen het nummer (1, 2, etc.)
   - Werkt samen met conditionele secties voor correcte nummering

#### 🔄 Processors

1. **PlaceholderProcessor** - Vervangt alle text placeholders
   - Bouwt dictionary met 500+ placeholders
   - Verwerkt body, headers, footers
   - Ondersteunt 4 formaten: `[[Key]]`, `{Key}`, `<<Key>>`, `[Key]`

2. **ConditionalSectionProcessor** - Verwerkt conditionele secties
   - Ondersteunt `[[IF:VeldNaam]]...[[ENDIF:VeldNaam]]` syntax
   - Verwijdert complete blokken als veld leeg is
   - Behoudt content en verwijdert alleen tags als veld gevuld is
   - Ondersteunt geneste conditionele blokken

3. **ContentControlProcessor** - Verwerkt speciale content
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
services.AddScoped<IConditionalSectionProcessor, ConditionalSectionProcessor>();
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
    public CommunicatieAfsprakenData? CommunicatieAfspraken { get; set; }  // Nieuw in v2.2.0
    public Dictionary<string, string> CustomPlaceholders { get; set; }    // Nieuw in v2.4.0

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
    public string? Voorletters { get; set; }           // Nieuw: voor VoorlettersAchternaam
    public string? Adres { get; set; }
    public string? Postcode { get; set; }
    public string? Plaats { get; set; }
    public string? GeboortePlaats { get; set; }
    public DateTime? GeboorteDatum { get; set; }
    public string? Telefoon { get; set; }
    public string? Email { get; set; }
    public string? Geslacht { get; set; }              // M/V voor gender-specifieke teksten
    public string? Nationaliteit1 { get; set; }        // Nieuw: eerste nationaliteit
    public string? Nationaliteit2 { get; set; }        // Nieuw: tweede nationaliteit (optioneel)
    public int? RolId { get; set; }                    // 1 = Partij 1, 2 = Partij 2
    public string VolledigeNaam { get; }               // Computed property
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

### CommunicatieAfsprakenData (Nieuw in v2.2.0)

Communicatie- en praktische afspraken rondom de kinderen.

```csharp
public class CommunicatieAfsprakenData
{
    public int Id { get; set; }
    public int DossierId { get; set; }

    // Kinderen betrokkenheid
    public string? VillaPinedoKinderen { get; set; }
    public string? KinderenBetrokkenheid { get; set; }
    public string? KiesMethode { get; set; }

    // Omgang
    public string? OmgangTekstOfSchema { get; set; }
    public string? OmgangBeschrijving { get; set; }
    public string? Opvang { get; set; }
    public string? InformatieUitwisseling { get; set; }
    public string? BijlageBeslissingen { get; set; }

    // Digitale afspraken
    public string? SocialMedia { get; set; }         // "wel", "geen", "wel_13" etc.
    public string? MobielTablet { get; set; }        // JSON: {"smartphone":12,"tablet":14}
    public string? ToezichtApps { get; set; }        // "wel" / "geen"
    public string? LocatieDelen { get; set; }        // "wel" / "geen"

    // Verzekeringen & documenten
    public string? IdBewijzen { get; set; }          // "ouder_1", "ouder_2", "beide_ouders", etc.
    public string? Aansprakelijkheidsverzekering { get; set; }
    public string? Ziektekostenverzekering { get; set; }
    public string? ToestemmingReizen { get; set; }

    // Toekomst
    public string? Jongmeerderjarige { get; set; }
    public string? Studiekosten { get; set; }

    // Bankrekeningen
    public string? BankrekeningKinderen { get; set; } // JSON array van Kinderrekening objects

    // Evaluatie & conflictoplossing
    public string? Evaluatie { get; set; }
    public string? ParentingCoordinator { get; set; }
    public string? MediationClausule { get; set; }
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
cd idocx-document-generator
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

De Word template ondersteunt **200+ placeholders** in meerdere formaten: `[[...]]`, `{...}`, `<<...>>`, `[...]`

### Placeholder Features

- **Case-insensitive**: `[[Partij1Naam]]` = `[[partij1naam]]` = `[[PARTIJ1NAAM]]`
- **Automatische formatting**:
  - Datums: "15 januari 2024" (Nederlandse lange datum)
  - Bedragen: "€ 350,00" (Nederlands formaat)
  - Lijsten: "Jan, Maria en Piet" (met "en" tussen laatste items)
  - Adressen: "Straat 12, 1234AB Plaats"
- **Anonimiteit ondersteuning**: Bij `IsAnoniem = true` worden namen vervangen door "de vader/moeder/persoon"
- **Dynamische grammatica**: Automatisch enkelvoud/meervoud op basis van aantal minderjarige kinderen
- **API-gegenereerde zinnen**: Sommige placeholders worden automatisch door de backend API gegenereerd met correcte Nederlandse grammatica:
  - `[[GezagZin]]` / `[[GezagRegeling]]` - Complete zin over ouderlijk gezag
  - `[[RelatieAanvangZin]]` - Complete zin over aanvang relatie
  - `[[OuderschapsplanDoelZin]]` - Complete doelzin voor het ouderschapsplan

---

### Dossier Informatie

```
[[DossierNummer]]      - Dossiernummer
[[DossierDatum]]       - Aanmaak datum (dd-MM-yyyy)
[[HuidigeDatum]]       - Huidige datum (dd MMMM yyyy, Nederlands)
[[IsAnoniem]]          - Ja/Nee
```

### Partij Gegevens (Partij1 & Partij2)

```
[[Partij1Naam]] / [[Partij2Naam]]                                    - Volledige naam
[[Partij1Voornaam]] / [[Partij2Voornaam]]                            - Voornaam/voornamen
[[Partij1Roepnaam]] / [[Partij2Roepnaam]]                            - Roepnaam
[[Partij1Achternaam]] / [[Partij2Achternaam]]                        - Achternaam
[[Partij1Tussenvoegsel]] / [[Partij2Tussenvoegsel]]                  - Tussenvoegsel (de, van, van der, etc.)
[[Partij1VolledigeNaamMetTussenvoegsel]] / [[Partij2VolledigeNaamMetTussenvoegsel]]
[[Partij1VolledigeAchternaam]] / [[Partij2VolledigeAchternaam]]      - Achternaam met tussenvoegsel
[[Partij1VoorlettersAchternaam]] / [[Partij2VoorlettersAchternaam]]  - Voorletters + tussenvoegsel + achternaam (bijv. "J.P. de Vries")
[[Partij1Adres]] / [[Partij2Adres]]                                  - Straat + huisnummer
[[Partij1Postcode]] / [[Partij2Postcode]]                            - Postcode
[[Partij1Plaats]] / [[Partij2Plaats]]                                - Woonplaats
[[Partij1Geboorteplaats]] / [[Partij2Geboorteplaats]]                - Geboorteplaats
[[Partij1VolledigAdres]] / [[Partij2VolledigAdres]]                  - Volledig adres
[[Partij1Geboortedatum]] / [[Partij2Geboortedatum]]                  - Geboortedatum (dd-MM-yyyy)
[[Partij1Telefoon]] / [[Partij2Telefoon]]                            - Telefoonnummer
[[Partij1Email]] / [[Partij2Email]]                                  - Email adres
[[Partij1Benaming]] / [[Partij2Benaming]]                            - Context-afhankelijk (roepnaam of "de vader/moeder/persoon")
[[Partij1Nationaliteit1]] / [[Partij2Nationaliteit1]]                - Eerste nationaliteit (bijv. "Nederlandse")
[[Partij1Nationaliteit2]] / [[Partij2Nationaliteit2]]                - Tweede nationaliteit (optioneel)
[[Partij1Nationaliteit1Bijvoeglijk]] / [[Partij2Nationaliteit1Bijvoeglijk]]  - Bijvoeglijke vorm nationaliteit (bijv. "Nederlandse")
[[Partij1Nationaliteit2Bijvoeglijk]] / [[Partij2Nationaliteit2Bijvoeglijk]]  - Bijvoeglijke vorm tweede nationaliteit
```

**Nationaliteit conversie:**
Het systeem converteert nationaliteiten automatisch naar de bijvoeglijke vorm:
- "Nederland" → "Nederlandse"
- "België" → "Belgische"
- "Duitsland" → "Duitse"
- "Turkije" → "Turkse"
- etc.

### Kind Gegevens

**Individueel (Kind1, Kind2, Kind3, etc.):**
```
[[Kind1Naam]] / [[Kind2Naam]] / [[Kind3Naam]] etc.                  - Volledige naam
[[Kind1Voornaam]] / [[Kind2Voornaam]]                                - Voornaam/voornamen
[[Kind1Roepnaam]] / [[Kind2Roepnaam]]                                - Roepnaam
[[Kind1Achternaam]] / [[Kind2Achternaam]]                            - Achternaam
[[Kind1Tussenvoegsel]] / [[Kind2Tussenvoegsel]]                      - Tussenvoegsel (de, van, etc.)
[[Kind1RoepnaamAchternaam]] / [[Kind2RoepnaamAchternaam]]            - Roepnaam + tussenvoegsel + achternaam (bijv. "Jan de Vries")
[[Kind1Geboortedatum]] / [[Kind2Geboortedatum]]                      - Geboortedatum (dd-MM-yyyy)
[[Kind1Geboorteplaats]] / [[Kind2Geboorteplaats]]                    - Geboorteplaats
[[Kind1Leeftijd]] / [[Kind2Leeftijd]]                                - Leeftijd in jaren
[[Kind1Geslacht]] / [[Kind2Geslacht]]                                - Geslacht (M/V)
```

**Samenvattend:**
```
[[AantalKinderen]]                     - Totaal aantal kinderen
[[AantalMinderjarigeKinderen]]         - Aantal kinderen < 18 jaar
[[KinderenNamen]]                      - Alle namen (bijv. "Jan, Maria en Piet")
[[KinderenRoepnamen]]                  - Alle roepnamen (met "en")
[[KinderenVolledigeNamen]]             - Volledige namen (met "en")
[[RoepnamenMinderjarigeKinderen]]      - Roepnamen alleen minderjarigen
```

### Relatie Informatie

```
[[SoortRelatie]]                       - Type relatie (Gehuwd, Geregistreerd partnerschap, etc.)
[[DatumAanvangRelatie]]                - Startdatum relatie
[[PlaatsRelatie]]                      - Plaats waar relatie is aangegaan
[[SoortRelatieVoorwaarden]]            - Juridische voorwaarden (automatisch gegenereerd)
[[SoortRelatieVerbreking]]             - Type verbreking (automatisch gegenereerd)
[[RelatieAanvangZin]]                  - Volledige zin over relatie (automatisch gegenereerd door API)
[[OuderschapsplanDoelZin]]             - Doel ouderschapsplan zin (automatisch gegenereerd door API)
```

**Voorbeelden van RelatieAanvangZin:**
- "Wij zijn op 15 mei 2015 te Amsterdam met elkaar gehuwd."
- "Wij zijn op 22 maart 2018 met elkaar een geregistreerd partnerschap aangegaan."
- "Wij hebben een affectieve relatie gehad."

**Voorbeelden van OuderschapsplanDoelZin:**
- "In dit ouderschapsplan hebben we afspraken gemaakt over onze kinderen omdat we gaan scheiden." (bij huwelijk)
- "In dit ouderschapsplan hebben we afspraken gemaakt over onze kinderen omdat we uit elkaar gaan." (bij samenwonen)

### Gezag (Ouderlijk Gezag)

```
[[GezagPartij]]                        - Gezag optie (1-5)
[[GezagTermijnWeken]]                  - Aantal weken (bij voorlopig gezag)
[[GezagRegeling]]                      - Volledige gezagszin (automatisch gegenereerd door API)
[[GezagZin]]                           - Alias voor GezagRegeling
```

**Gezag Opties:**
- 1: Beiden hebben gezag
- 2: Partij 1 heeft gezag
- 3: Partij 2 heeft gezag
- 4: Voorlopig gezag voor X weken
- 5: Anders

**Voorbeelden van GezagZin/GezagRegeling:**
- "De ouders oefenen gezamenlijk het ouderlijk gezag uit over de minderjarige kinderen."
- "Jan de Vries oefent alleen het ouderlijk gezag uit over de minderjarige kinderen."
- "Maria Jansen oefent voorlopig alleen het ouderlijk gezag uit over de minderjarige kinderen. De ouders zullen binnen 12 weken een regeling treffen om het gezamenlijk ouderlijk gezag te regelen."

### Woonplaats Regelingen

```
[[WoonplaatsOptie]]                    - Woonplaats optie (1-5)
[[WoonplaatsPartij1]]                  - Nieuwe woonplaats partij 1
[[WoonplaatsPartij2]]                  - Nieuwe woonplaats partij 2
[[HuidigeWoonplaatsPartij1]]           - Huidige woonplaats partij 1
[[HuidigeWoonplaatsPartij2]]           - Huidige woonplaats partij 2
[[WoonplaatsRegeling]]                 - Volledige woonplaatszin (automatisch gegenereerd)
```

**Woonplaats Opties:**
- 1: Blijven zoals het is
- 2: Beide verhuizen naar nieuwe plaats
- 3: Partij 1 verhuist
- 4: Partij 2 verhuist
- 5: Anders

### Zorg & Verblijf

```
[[Hoofdverblijf]]                      - Hoofdverblijfplaats kind(eren)
[[Zorgverdeling]]                      - Zorgverdeling beschrijving
[[OpvangKinderen]]                     - Opvang regeling
[[BetrokkenheidKind]]                  - Betrokkenheid bij beslissingen
[[Kiesplan]]                           - Kiesplan beschrijving
[[KiesplanZin]]                        - Gegenereerde zin over kiesplan
[[ParentingCoordinator]]               - Parenting coordinator informatie
[[KeuzeDevices]]                       - Afspraken over devices
[[BankrekeningnummersKind]]            - Bankrekeningnummers
```

### Financieel - Algemeen

```
[[NettoBesteedbaarGezinsinkomen]]      - Netto gezinsinkomen (€ formaat)
[[KostenKinderen]]                     - Totale kosten kinderen (€ formaat)
[[BijdrageKostenKinderen]]             - Bijdrage aan kosten (€ formaat)
[[BijdrageTemplateOmschrijving]]       - Template beschrijving
[[Partij1EigenAandeel]]                - Eigen aandeel partij 1 (€ formaat)
[[Partij2EigenAandeel]]                - Eigen aandeel partij 2 (€ formaat)
```

### Financieel - Verzekeringen & Toeslagen

```
[[WaOpNaamVan]]                        - WA verzekering op naam van (roepnaam)
[[ZorgverzekeringOpNaamVan]]           - Zorgverzekering op naam van (roepnaam)
[[KinderbijslagOntvanger]]             - Wie ontvangt kinderbijslag (roepnaam of "Kinderrekening")
```

### Kinderrekening

```
[[StortingOuder1Kinderrekening]]                          - Maandelijkse storting ouder 1 (€ formaat)
[[StortingOuder2Kinderrekening]]                          - Maandelijkse storting ouder 2 (€ formaat)
[[KinderrekeningKostensoorten]]                           - Toegestane kostensoorten (lijst)
[[KinderrekeningMaximumOpname]]                           - Ja/Nee maximum opname
[[KinderrekeningMaximumOpnameBedrag]]                     - Maximum opname bedrag (€ formaat)
[[KinderbijslagStortenOpKinderrekening]]                  - Ja/Nee
[[KindgebondenBudgetStortenOpKinderrekening]]             - Ja/Nee
```

### Alimentatie

```
[[BedragenAlleKinderenGelijk]]         - Ja/Nee alle bedragen gelijk
[[AlimentatiebedragPerKind]]           - Bedrag per kind (€ formaat)
[[Alimentatiegerechtigde]]             - Wie ontvangt alimentatie (roepnaam)
[[IsKinderrekeningBetaalwijze]]        - Betaling via kinderrekening (Ja/Nee)
[[IsAlimentatieplichtBetaalwijze]]     - Betaling via alimentatieplicht (Ja/Nee)
[[KinderenAlimentatie]]                - Lijst van kinderen met alimentatie
[[ZorgkortingPercentageAlleKinderen]]  - Zorgkorting percentage (bijv. "35%")
[[AfsprakenAlleKinderenGelijk]]        - Ja/Nee afspraken voor alle kinderen gelijk
[[HoofdverblijfAlleKinderen]]          - Hoofdverblijf voor alle kinderen (roepnaam ouder)
[[InschrijvingAlleKinderen]]           - BRP-inschrijving voor alle kinderen (roepnaam ouder)
[[KinderbijslagOntvangerAlleKinderen]] - Wie ontvangt kinderbijslag (roepnaam of "Kinderrekening")
[[KindgebondenBudgetAlleKinderen]]     - Wie ontvangt kindgebonden budget (roepnaam of "Kinderrekening")
[[HoofdverblijfVerdeling]]             - Verdeling hoofdverblijf per kind (gegenereerde tekst)
[[InschrijvingVerdeling]]              - Verdeling BRP-inschrijving per kind (gegenereerde tekst)
[[BetaalwijzeBeschrijving]]            - Volledige beschrijving van de betaalwijze (gegenereerde tekst)
```

**BetaalwijzeBeschrijving:**
Dit is een automatisch gegenereerde beschrijving die alle financiële afspraken samenvat, inclusief:
- Keuze voor kinderrekening of alimentatie
- Toeslagen (kinderbijslag, kindgebonden budget)
- Stortingen door beide ouders
- Verblijfsoverstijgende kosten
- Maximum opname bedragen
- Indexeringsafspraken

### Communicatie Afspraken

Dit is een uitgebreid model voor alle communicatie- en praktische afspraken rondom de kinderen.

**Basis afspraken:**
```
[[VillaPinedoKinderen]]                - Villa Pinedo methode voor kinderen
[[VillaPinedoZin]]                     - Gegenereerde zin over Villa Pinedo
[[KinderenBetrokkenheid]]              - Betrokkenheid kinderen bij beslissingen
[[BetrokkenheidKindZin]]               - Gegenereerde zin over betrokkenheid
[[KiesMethode]]                        - Gekozen methode voor ouderschapsplan
[[OmgangTekstOfSchema]]                - Omgang als tekst of schema
[[OmgangsregelingBeschrijving]]        - Volledige beschrijving omgangsregeling
[[Evaluatie]]                          - Evaluatiefrequentie
[[ParentingCoordinator]]               - Parenting coordinator informatie
[[MediationClausule]]                  - Mediation clausule
```

**Opvang & Informatie:**
```
[[Opvang]]                             - Opvang keuze
[[OpvangBeschrijving]]                 - Gegenereerde beschrijving opvang
[[InformatieUitwisseling]]             - Informatie uitwisseling methode
[[InformatieUitwisselingBeschrijving]] - Gegenereerde beschrijving informatie-uitwisseling
[[BijlageBeslissingen]]                - Bijlage beslissingen
```

**Digitale afspraken:**
```
[[SocialMedia]]                        - Social media keuze (wel/geen/wel_13)
[[SocialMediaKeuze]]                   - Keuze geëxtraheerd (wel/geen/later)
[[SocialMediaLeeftijd]]                - Leeftijd indien van toepassing
[[SocialMediaBeschrijving]]            - Gegenereerde beschrijving social media afspraken
[[MobielTablet]]                       - JSON object met device leeftijden
[[DeviceSmartphone]]                   - Leeftijd voor smartphone
[[DeviceTablet]]                       - Leeftijd voor tablet
[[DeviceSmartwatch]]                   - Leeftijd voor smartwatch
[[DeviceLaptop]]                       - Leeftijd voor laptop
[[DevicesBeschrijving]]                - Gegenereerde beschrijving devices per leeftijd
[[ToezichtApps]]                       - Ouderlijk toezichtapps (wel/geen)
[[ToezichtAppsBeschrijving]]           - Gegenereerde beschrijving toezichtapps
[[LocatieDelen]]                       - Locatie delen (wel/geen)
[[LocatieDelenBeschrijving]]           - Gegenereerde beschrijving locatie delen
```

**Verzekeringen & Documenten:**
```
[[IdBewijzen]]                         - ID-bewijzen beheer (ouder_1/ouder_2/beide_ouders/kinderen_zelf)
[[IdBewijzenBeschrijving]]             - Gegenereerde beschrijving ID-bewijzen
[[Aansprakelijkheidsverzekering]]      - WA-verzekering beheer (ouder_1/ouder_2/beiden)
[[AansprakelijkheidsverzekeringBeschrijving]]  - Gegenereerde beschrijving WA-verzekering
[[Ziektekostenverzekering]]            - Zorgverzekering beheer (ouder_1/ouder_2/hoofdverblijf)
[[ZiektekostenverzekeringBeschrijving]] - Gegenereerde beschrijving zorgverzekering
```

**Reizen & Toekomst:**
```
[[ToestemmingReizen]]                  - Reistoestemming (altijd_overleggen/eu_vrij/vrij/schriftelijk)
[[ToestemmingReizenBeschrijving]]      - Gegenereerde beschrijving reistoestemming
[[Jongmeerderjarige]]                  - Jongmeerderjarige afspraken (optie)
[[JongmeerderjarigeBeschrijving]]      - Gegenereerde beschrijving jongmeerderjarige
[[Studiekosten]]                       - Studiekosten afspraken (optie)
[[StudiekostenBeschrijving]]           - Gegenereerde beschrijving studiekosten
```

**Bankrekeningen kinderen:**
```
[[BankrekeningKinderen]]               - Geformatteerde lijst bankrekeningen
[[BankrekeningenCount]]                - Aantal bankrekeningen
[[Bankrekening1IBAN]]                  - IBAN eerste rekening (geformatteerd met spaties)
[[Bankrekening1Tenaamstelling]]        - Tenaamstelling eerste rekening
[[Bankrekening1BankNaam]]              - Banknaam eerste rekening
[[Bankrekening2IBAN]] etc.             - Idem voor volgende rekeningen
```

**Voorbeelden van gegenereerde beschrijvingen:**

*SocialMediaBeschrijving (bij optie wel_13):*
> "Wij spreken als ouders af dat Jan en Marie social media mogen gebruiken vanaf hun 13e jaar, op voorwaarde dat het op een veilige manier gebeurt."

*DevicesBeschrijving:*
> "Jan en Marie krijgen een smartphone vanaf hun 12e jaar.
> Jan en Marie krijgen een tablet vanaf hun 10e jaar."

*IdBewijzenBeschrijving (bij optie beide_ouders):*
> "De identiteitsbewijzen van Jan en Marie worden bewaard door beide ouders."

*JongmeerderjarigeBeschrijving:*
> "Wij spreken af dat de afspraken in dit ouderschapsplan doorlopen totdat onze kinderen 21 jaar worden."

### Grammatica Regels (Automatisch Enkelvoud/Meervoud)

Deze placeholders worden **automatisch** aangepast op basis van het aantal minderjarige kinderen:

```
[[ons kind/onze kinderen]]             - "ons kind" (1) of "onze kinderen" (2+)
[[heeft/hebben]]                       - Werkwoord hebben
[[is/zijn]]                            - Werkwoord zijn
[[verblijft/verblijven]]               - Werkwoord verblijven
[[kan/kunnen]]                         - Werkwoord kunnen
[[zal/zullen]]                         - Werkwoord zullen
[[moet/moeten]]                        - Werkwoord moeten
[[wordt/worden]]                       - Werkwoord worden
[[blijft/blijven]]                     - Werkwoord blijven
[[gaat/gaan]]                          - Werkwoord gaan
[[komt/komen]]                         - Werkwoord komen
[[hem/haar/hen]]                       - Lijdend voorwerp (gender-specifiek bij 1 kind)
[[hij/zij/ze]]                         - Onderwerp (gender-specifiek bij 1 kind)
```

**Hoe werkt dit?**
- Bij **0 kinderen**: standaard enkelvoud
- Bij **1 kind**: enkelvoud + gender-specifieke voornaamwoorden (op basis van geslacht)
- Bij **2+ kinderen**: altijd meervoud

**Voorbeeld:**
- 1 jongen: "[[ons kind/onze kinderen]] [[heeft/hebben]]" → "ons kind heeft"
- 2 kinderen: "[[ons kind/onze kinderen]] [[heeft/hebben]]" → "onze kinderen hebben"

### Conditionele Secties

De template ondersteunt conditionele secties die alleen worden opgenomen als het veld een waarde heeft:

```
[[IF:GezagRegeling]]
[[ARTIKEL]] - Het gezag over [[ons kind/onze kinderen]]

[[GezagRegeling]]
[[ENDIF:GezagRegeling]]
```

**Hoe het werkt:**
- `[[IF:VeldNaam]]` - Start conditioneel blok
- `[[ENDIF:VeldNaam]]` - Einde conditioneel blok
- Als `VeldNaam` leeg/null is → hele blok wordt verwijderd (inclusief alle paragraphs ertussen)
- Als `VeldNaam` een waarde heeft → alleen de IF/ENDIF tags worden verwijderd, content blijft behouden

**Geneste conditionele blokken:**
Conditionele blokken kunnen genest worden, maar elke IF moet een bijbehorende ENDIF hebben met dezelfde veldnaam:

```
[[IF:WoonplaatsRegeling]]
Artikel X - Woonplaats

[[WoonplaatsRegeling]]

[[IF:WoonplaatsOptie]]
Gekozen optie: [[WoonplaatsOptie]]
[[ENDIF:WoonplaatsOptie]]
[[ENDIF:WoonplaatsRegeling]]
```

### Automatische Artikelnummering

Het systeem ondersteunt automatische multi-level juridische nummering met de volgende placeholders:

| Placeholder | Beschrijving | Output |
|-------------|--------------|--------|
| `[[ARTIKEL]]` | Multi-level list level 0 | "Artikel 1", "Artikel 2", etc. |
| `[[SUBARTIKEL]]` | Multi-level list level 1 | "1.1", "1.2", etc. |
| `[[ARTIKEL_NR]]` | Alleen nummer (platte tekst) | "1", "2", etc. |
| `[[SUBARTIKEL_NR]]` | Alleen subnummer (platte tekst) | "1.1", "1.2", etc. |
| `[[ARTIKEL_RESET]]` | Reset alle tellers naar 1 | (geen output) |

**Basis gebruik:**
```
[[ARTIKEL]] - Respectvol ouderschap
[[ARTIKEL]] - De mening van [[ons kind/onze kinderen]]
[[ARTIKEL]] - Het gezag over [[ons kind/onze kinderen]]
```

Wordt na verwerking:
```
Artikel 1 - Respectvol ouderschap
Artikel 2 - De mening van onze kinderen
Artikel 3 - Het gezag over onze kinderen
```

**Multi-level nummering met subartikelen:**
```
[[ARTIKEL]] - Financiële afspraken
[[SUBARTIKEL]] Kinderalimentatie
[[SUBARTIKEL]] Kinderbijslag
[[SUBARTIKEL]] Kinderrekening

[[ARTIKEL]] - Zorgregeling
[[SUBARTIKEL]] Hoofdverblijf
[[SUBARTIKEL]] Omgangsregeling
```

Wordt na verwerking:
```
Artikel 1 - Financiële afspraken
1.1 Kinderalimentatie
1.2 Kinderbijslag
1.3 Kinderrekening

Artikel 2 - Zorgregeling
2.1 Hoofdverblijf
2.2 Omgangsregeling
```

**Alleen het nummer (voor referenties):**
Gebruik `[[ARTIKEL_NR]]` als "Artikel" al in de template staat:
```
Artikel [[ARTIKEL_NR]] - Respectvol ouderschap
```

**Reset nummering:**
Gebruik `[[ARTIKEL_RESET]]` om de tellers te resetten (bijv. voor bijlagen):
```
[[ARTIKEL]] - Hoofdstuk 1
[[ARTIKEL]] - Hoofdstuk 2

[[ARTIKEL_RESET]]

[[ARTIKEL]] - Bijlage A (wordt weer "Artikel 1")
```

**Combinatie met conditionele secties:**
Als een artikel in een conditionele sectie staat en die sectie wordt verwijderd (omdat het veld leeg is), nummeren de resterende artikelen automatisch correct door. Dit zorgt ervoor dat je nooit "gaten" in de nummering krijgt.

**Voorbeeld:**
```
[[ARTIKEL]] - Respectvol ouderschap

[[IF:GezagRegeling]]
[[ARTIKEL]] - Het gezag
[[GezagRegeling]]
[[ENDIF:GezagRegeling]]

[[ARTIKEL]] - Financiële afspraken
```

Als `GezagRegeling` leeg is, wordt het resultaat:
```
Artikel 1 - Respectvol ouderschap

Artikel 2 - Financiële afspraken
```

### Dynamische Tabellen

Deze placeholders genereren complete tabellen en **moeten op een eigen regel staan**:

```
[[TABEL_ALIMENTATIE]]                  - Genereert alimentatie tabel
[[TABEL_OMGANG]]                       - Genereert omgangsregeling tabel per week
[[TABEL_ZORG]]                         - Genereert zorgtabellen (incl. vakanties, feestdagen, etc.)
[[LIJST_KINDEREN]]                     - Genereert bullet list met kinderen
[[ARTIKELEN]]                          - Genereert alle artikelen uit bibliotheek (v2.3.0)
```

**Let op:**
- Plaats deze placeholders **alleen** op een eigen paragraph (niet inline in een zin)
- `[[TABEL_ZORG]]` omvat alle zorgcategorieën inclusief vakanties en feestdagen
- Er zijn geen aparte `[[TABEL_VAKANTIES]]` of `[[TABEL_FEESTDAGEN]]` placeholders nodig

---

### Custom Placeholders (Nieuw in v2.4.0)

Custom placeholders worden beheerd via de **placeholder_catalogus** tabel en kunnen door professionals worden aangemaakt. Deze placeholders worden automatisch opgenomen in de document generatie.

**Database tabellen:**

```sql
-- Placeholder definities
dbo.placeholder_catalogus (
    id, placeholder_key, categorie, label, beschrijving,
    voorbeeld_waarde, is_systeem, data_type, bron_type,
    standaard_waarde, is_actief, ...
)

-- Waarden per gebruiker of dossier
dbo.placeholder_waarden (
    id, placeholder_id, gebruiker_id, dossier_id, waarde, ...
)
```

**Waarde prioriteit:**

| Prioriteit | Bron | Beschrijving |
|------------|------|--------------|
| 1 (hoogst) | Dossier | Waarde specifiek voor dit dossier |
| 2 | Gebruiker | Standaardwaarde van de professional |
| 3 (laagst) | Systeem | standaard_waarde uit placeholder_catalogus |

**Hoe het werkt:**

1. **DatabaseService.GetDossierDataAsync()** haalt custom placeholder waarden op (result set 13)
2. De query selecteert:
   - Alle actieve custom placeholders (bron_type = 'gebruiker' of 'dossier')
   - Effectieve waarde met prioriteit: `COALESCE(dpw.waarde, upw.waarde, pc.standaard_waarde)`
3. **PlaceholderProcessor.BuildReplacements()** voegt custom placeholders toe aan de replacements dictionary
4. Bestaande systeem placeholders worden NIET overschreven

**Voorbeeld:**

Als een professional de placeholder `[[MijnHandtekening]]` heeft aangemaakt met standaardwaarde "Met vriendelijke groet, Hans Kurvers", dan:

1. Zonder eigen waarde → "Met vriendelijke groet, Hans Kurvers"
2. Met gebruiker waarde "Hartelijk dank, Team Mediatie" → "Hartelijk dank, Team Mediatie"
3. Met dossier waarde "Specifiek voor dit dossier" → "Specifiek voor dit dossier"

**Code integratie** (PlaceholderProcessor.cs):

```csharp
// In BuildReplacements method
if (data.CustomPlaceholders.Any())
{
    foreach (var placeholder in data.CustomPlaceholders)
    {
        // Voeg alleen toe als key nog niet bestaat (systeem heeft voorrang)
        if (!replacements.ContainsKey(placeholder.Key))
        {
            replacements[placeholder.Key] = placeholder.Value;
        }
    }
}
```

---

### Best Practices voor Template Gebruik

1. **Gebruik dubbele vierkante haken**: `[[Partij1Naam]]` (meest leesbaar)
2. **Grammatica regels**: Toon beide vormen: `[[ons kind/onze kinderen]]`
3. **Tabel placeholders**: Plaats op eigen regel, niet inline
4. **Anonimiteit**: Gebruik `[[Partij1Benaming]]` voor automatische anonimiteit handling
5. **Afgeleide placeholders**: Gebruik placeholders zoals `[[RelatieAanvangZin]]` voor complete zinnen
6. **Consistentie**: Zorg dat enkelvoud/meervoud regels consistent zijn door het hele document

### Veelvoorkomende Problemen

**Placeholder wordt niet vervangen?**
- Check spelling (case-insensitive, maar spelling moet kloppen)
- Verifieer dat data bestaat in database
- Controleer of je een ondersteund formaat gebruikt (`[[...]]`, `{...}`, etc.)

**Grammatica regel werkt niet?**
- Zorg dat kindgegevens inclusief geboortedatum aanwezig zijn
- Check dat leeftijd correct berekend wordt (< 18 jaar = minderjarig)
- Gebruik beide vormen in de placeholder: `[[heeft/hebben]]`

**Tabel genereert niet?**
- Zet placeholder op een eigen regel
- Verifieer dat data aanwezig is (omgang/zorg data in database)
- Check logs voor specifieke fouten

## Deployment

### Deploy naar Azure

**Stap 1: Azure Resources aanmaken**

```bash
# Resource group
az group create --name rg-idocx-docgen --location westeurope

# Storage account (voor Azure Functions)
az storage account create \
  --name stidocxdocgen \
  --resource-group rg-idocx-docgen \
  --location westeurope \
  --sku Standard_LRS

# Application Insights
az monitor app-insights component create \
  --app ai-idocx-docgen \
  --location westeurope \
  --resource-group rg-idocx-docgen

# Function App
az functionapp create \
  --resource-group rg-idocx-docgen \
  --consumption-plan-location westeurope \
  --runtime dotnet-isolated \
  --runtime-version 9.0 \
  --functions-version 4 \
  --name func-idocx-docgen \
  --storage-account stidocxdocgen \
  --app-insights ai-idocx-docgen
```

**Stap 2: Configuratie Settings**

```bash
# Connection string
az functionapp config connection-string set \
  --name func-idocx-docgen \
  --resource-group rg-idocx-docgen \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=your-server.database.windows.net;Database=your-db;User Id=user;Password=pass;"

# Template URL met SAS token
az functionapp config appsettings set \
  --name func-idocx-docgen \
  --resource-group rg-idocx-docgen \
  --settings TemplateStorageUrl="https://yourstorage.blob.core.windows.net/templates/Ouderschapsplan%20NIEUW.docx?sp=r&st=2024-01-01&..."
```

**Stap 3: Deploy de applicatie**

```bash
# Via Azure Functions Core Tools
func azure functionapp publish func-idocx-docgen

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
        app-name: func-idocx-docgen
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
az functionapp config appsettings list --name func-idocx-docgen --resource-group rg-idocx-docgen | grep TemplateStorageUrl

# Update met nieuwe SAS token
az functionapp config appsettings set --name func-idocx-docgen --resource-group rg-idocx-docgen --settings TemplateStorageUrl="new-url-with-sas"
```

#### 2. "Database connection failed"

**Oorzaak**: Connection string is incorrect of firewall blokkeert toegang.

**Oplossing**:
```bash
# Voeg Azure Functions IP toe aan SQL firewall
az sql server firewall-rule create \
  --resource-group rg-idocx-docgen \
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

#### 7. "Content control not found" of lege output

**Oorzaak**: Template heeft geen content controls of verkeerde placeholder syntax.

**Oplossing**:
- Open template in Word, ga naar Developer tab → Design Mode
- Controleer of content controls aanwezig zijn met correcte tags
- Verifieer placeholder syntax: `[[Naam]]` niet `[[ Naam ]]` (geen spaties)
- Check of placeholders exact overeenkomen met code (case-insensitive)

#### 8. "Artikel numbering incorrect" of gaten in nummering

**Oorzaak**: Conditionele secties verstoren de artikelnummering.

**Oplossing**:
- Zorg dat `[[ARTIKEL]]` altijd op een eigen paragraph staat (niet inline)
- Controleer of alle `[[IF:VeldNaam]]` tags een bijbehorende `[[ENDIF:VeldNaam]]` hebben
- Verifieer dat veldnamen exact overeenkomen (case-sensitive voor IF/ENDIF)
- Gebruik `[[ARTIKEL_RESET]]` expliciet voor bijlagen/appendices

#### 9. "Foreign key constraint violation" bij verwijderen

**Oorzaak**: Gekoppelde records bestaan nog in gerelateerde tabellen.

**Oplossing**:
- Verwijder eerst kind-records (omgang, zorg) voordat je dossier verwijdert
- Check `dossiers_partijen`, `dossiers_kinderen`, `omgang`, `zorg` tabellen
- Gebruik CASCADE DELETE in database schema voor automatische cleanup

#### 10. Gegenereerd document opent niet of is corrupt

**Oorzaak**: Document generatie is onderbroken of template is beschadigd.

**Oplossing**:
- Download template opnieuw en test met minimale data
- Check Application Insights voor specifieke OpenXML errors
- Verifieer dat alle tabel generators valid XML produceren
- Test met een nieuw, leeg template om template-specifieke issues uit te sluiten

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

Dit project is eigendom van Ouderschapsplan en bedoeld voor interne gebruik in het juridisch document automation systeem.

## Changelog

### v2.4.0 (Current) - Custom Placeholders Integratie

**Nieuwe features:**
- 🔧 **Custom Placeholders Systeem** - Ondersteuning voor custom placeholders uit placeholder_catalogus:
  - `CustomPlaceholders` dictionary toegevoegd aan `DossierData.cs`
  - DatabaseService haalt custom placeholder waarden op (result set 13)
  - PlaceholderProcessor integreert custom placeholders automatisch
  - 3-laags prioriteit: dossier > gebruiker > systeem standaardwaarde
- 📊 **Placeholder Catalogus** - 192+ systeem placeholders + onbeperkt custom:
  - Categorieën: partij1, partij2, kinderen, dossier, relatie, gezag, woonplaats, financieel, communicatie, grammatica, tabellen
  - Data types: tekst, datum, bedrag, ja_nee, getal
  - Bron types: gebruiker, dossier, systeem

**Breaking Changes:**
- Geen! Custom placeholders worden alleen toegevoegd als ze nog niet bestaan (systeem placeholders hebben voorrang).

### v2.3.0 - Artikel Bibliotheek Integratie

**Nieuwe features:**
- 📚 **Artikel Bibliotheek Integratie** - Dynamische artikelen uit database in documenten:
  - `[[ARTIKELEN]]` placeholder genereert alle actieve artikelen
  - 3-laags prioriteit systeem: dossier > gebruiker > systeem
  - Conditionele filtering op basis van dossier data
  - Placeholder vervanging binnen artikel teksten
  - `[[IF:Veld]]...[[ENDIF:Veld]]` ondersteuning binnen artikelen
- 🏗️ **Nieuwe services**:
  - `ArtikelData.cs` - Data model voor artikelen
  - `IArtikelService` / `ArtikelService` - Artikel verwerking
  - `ArtikelContentGenerator` - Word paragraph generatie
- 📄 **Template types** - Nieuw "artikelen" template type voor templates met `[[ARTIKELEN]]`

**Breaking Changes:**
- Geen! De integratie is 100% backwards compatible. Bestaande templates zonder `[[ARTIKELEN]]` blijven werken.

### v2.2.0 - Communicatie Afspraken & Uitbreidingen

**Nieuwe features:**
- 📋 **CommunicatieAfspraken model** - Volledig nieuw model met 40+ placeholders voor:
  - Villa Pinedo methode en kinderen betrokkenheid
  - Digitale afspraken (social media, devices, toezichtapps, locatie delen)
  - Verzekeringen (WA, zorgverzekering)
  - ID-bewijzen beheer
  - Reistoestemming
  - Jongmeerderjarige en studiekosten afspraken
  - Bankrekeningen kinderen
- 📝 **Automatische beschrijvings-generators** - Intelligente tekstgeneratie op basis van keuzes:
  - `[[SocialMediaBeschrijving]]`, `[[DevicesBeschrijving]]`
  - `[[IdBewijzenBeschrijving]]`, `[[ToestemmingReizenBeschrijving]]`
  - `[[JongmeerderjarigeBeschrijving]]`, `[[StudiekostenBeschrijving]]`
  - En vele anderen...
- 🏠 **Hoofdverblijf/Inschrijving verdeling** - Nieuwe placeholders voor kindverdeling:
  - `[[HoofdverblijfVerdeling]]` - Automatische tekst per kind
  - `[[InschrijvingVerdeling]]` - BRP-inschrijving verdeling
- 💰 **BetaalwijzeBeschrijving** - Volledige financiële beschrijving met:
  - Kinderrekening of alimentatie uitleg
  - Toeslagen en stortingen
  - Verblijfsoverstijgende kosten
  - Maximum opnames en indexering

### v2.1.0 - Multi-level Artikelnummering & Nationaliteit

**Nieuwe features:**
- 🔢 **Multi-level juridische nummering**:
  - `[[SUBARTIKEL]]` - Sub-artikelen (1.1, 1.2, etc.)
  - `[[SUBARTIKEL_NR]]` - Alleen subnummer
  - `[[ARTIKEL_RESET]]` - Reset tellers voor bijlagen
- 🌍 **Nationaliteit placeholders**:
  - `[[Partij1Nationaliteit1]]` / `[[Partij2Nationaliteit1]]`
  - `[[Partij1Nationaliteit1Bijvoeglijk]]` - Automatische conversie (Nederland → Nederlandse)
- 👤 **VoorlettersAchternaam** - Nieuwe placeholder voor formele naamnotatie (J.P. de Vries)
- 👶 **Kind placeholders uitgebreid**:
  - `[[KindXTussenvoegsel]]`
  - `[[KindXRoepnaamAchternaam]]`
- 💵 **Alimentatie uitbreidingen**:
  - `[[ZorgkortingPercentageAlleKinderen]]`
  - `[[AfsprakenAlleKinderenGelijk]]`
  - `[[HoofdverblijfAlleKinderen]]`, `[[InschrijvingAlleKinderen]]`
  - `[[KinderbijslagOntvangerAlleKinderen]]`, `[[KindgebondenBudgetAlleKinderen]]`
- 📄 **Paragraph formatting behouden** - Fixes voor correcte opmaak bij placeholder vervanging
- 🔤 **Formeel "Wij"** - Alimentatie teksten gebruiken nu formeel "Wij" i.p.v. "wij"

### v2.0.0 - 🎉 Grote Refactoring

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