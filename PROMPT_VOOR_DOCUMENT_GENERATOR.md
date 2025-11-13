# Update Document Generator: Nieuwe Alimentatie Velden voor Synchronisatie

## Context
We hebben zojuist de backend bijgewerkt met nieuwe velden in de alimentatie tabel om synchronisatie van afspraken voor alle kinderen tegelijk te ondersteunen. Deze velden moeten nu ook beschikbaar worden gemaakt in de Document Generator voor gebruik in templates en placeholders.

## Nieuwe Database Velden
De volgende velden zijn toegevoegd aan de `dbo.alimentaties` tabel:
- `afspraken_alle_kinderen_gelijk` (BIT NULL) - Geeft aan of alle kinderen dezelfde afspraken hebben
- `hoofdverblijf_alle_kinderen` (VARCHAR(255) NULL) - Hoofdverblijf setting voor alle kinderen
- `inschrijving_alle_kinderen` (VARCHAR(255) NULL) - Inschrijving setting voor alle kinderen  
- `kinderbijslag_ontvanger_alle_kinderen` (VARCHAR(255) NULL) - Kinderbijslag ontvanger voor alle kinderen
- `kindgebonden_budget_alle_kinderen` (VARCHAR(255) NULL) - Kindgebonden budget ontvanger voor alle kinderen

## Wat moet er gebeuren

### 1. Update AlimentatieData Model
Voeg de nieuwe velden toe aan het AlimentatieData model in `Models/AlimentatieData.cs`:

```csharp
public bool? AfsprakenAlleKinderenGelijk { get; set; }
public string HoofdverblijfAlleKinderen { get; set; }
public string InschrijvingAlleKinderen { get; set; }
public string KinderbijslagOntvangerAlleKinderen { get; set; }
public string KindgebondenBudgetAlleKinderen { get; set; }
```

### 2. Update DatabaseRepository
In `Services/DatabaseRepository.cs`, update de `GetAlimentatieData` methode om de nieuwe velden op te halen:

```sql
SELECT 
    -- Bestaande velden...
    afspraken_alle_kinderen_gelijk,
    hoofdverblijf_alle_kinderen,
    inschrijving_alle_kinderen,
    kinderbijslag_ontvanger_alle_kinderen,
    kindgebonden_budget_alle_kinderen
FROM dbo.alimentaties
WHERE dossier_id = @dossierId
```

En map deze in de reader:
```csharp
AfsprakenAlleKinderenGelijk = reader["afspraken_alle_kinderen_gelijk"] as bool?,
HoofdverblijfAlleKinderen = reader["hoofdverblijf_alle_kinderen"]?.ToString(),
InschrijvingAlleKinderen = reader["inschrijving_alle_kinderen"]?.ToString(),
KinderbijslagOntvangerAlleKinderen = reader["kinderbijslag_ontvanger_alle_kinderen"]?.ToString(),
KindgebondenBudgetAlleKinderen = reader["kindgebonden_budget_alle_kinderen"]?.ToString()
```

### 3. Update PlaceholderProcessor
In `Services/PlaceholderProcessor.cs`, voeg nieuwe placeholders toe voor deze velden:

```csharp
// In AddAlimentatiePlaceholders methode:
AddPlaceholder("AfsprakenAlleKinderenGelijk", alimentatie?.AfsprakenAlleKinderenGelijk == true ? "Ja" : "Nee");
AddPlaceholder("HoofdverblijfAlleKinderen", alimentatie?.HoofdverblijfAlleKinderen ?? "");
AddPlaceholder("InschrijvingAlleKinderen", alimentatie?.InschrijvingAlleKinderen ?? "");
AddPlaceholder("KinderbijslagOntvangerAlleKinderen", GetPartyName(alimentatie?.KinderbijslagOntvangerAlleKinderen));
AddPlaceholder("KindgebondenBudgetAlleKinderen", GetPartyName(alimentatie?.KindgebondenBudgetAlleKinderen));
```

### 4. Nieuwe Placeholders voor Templates
De volgende placeholders worden beschikbaar:
- `[[AfsprakenAlleKinderenGelijk]]` - "Ja" of "Nee"
- `[[HoofdverblijfAlleKinderen]]` - Naam van de ouder of lege string
- `[[InschrijvingAlleKinderen]]` - Naam van de ouder of lege string
- `[[KinderbijslagOntvangerAlleKinderen]]` - Roepnaam van de ouder of "Kinderrekening"
- `[[KindgebondenBudgetAlleKinderen]]` - Roepnaam van de ouder of "Kinderrekening"

### 5. Update placeholders.md
Voeg deze nieuwe placeholders toe aan de documentatie onder de sectie "Alimentatie/Financiële Informatie".

## Belangrijke Opmerkingen
1. Deze velden zijn bedoeld voor het synchroniseren van settings naar alle kinderen tegelijk
2. De waarden kunnen "partij1", "partij2", of "kinderrekening" bevatten
3. Voor de placeholder output moet je de juiste naam/roepnaam tonen (niet de database waarde)
4. Alle velden zijn optioneel en kunnen NULL zijn

## Test Scenario
Na implementatie, test het volgende:
1. Maak een dossier met meerdere kinderen
2. Zet `afspraken_alle_kinderen_gelijk` op true in de database
3. Vul de andere velden in met bijv. "partij1" of "kinderrekening"
4. Genereer een document en controleer of de placeholders correct worden vervangen
5. Controleer specifiek dat partijnamen worden getoond (niet "partij1" letterlijk)

## Verwachte Output Voorbeeld
Als in de database staat:
- `kinderbijslag_ontvanger_alle_kinderen = "partij1"`
- En partij1 heeft roepnaam "Jan"

Dan moet `[[KinderbijslagOntvangerAlleKinderen]]` vervangen worden door: "Jan"

Als het "kinderrekening" is, dan moet het "Kinderrekening" tonen.