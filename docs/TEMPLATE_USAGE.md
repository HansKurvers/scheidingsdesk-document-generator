# Template Usage Guide

## Overzicht

De document generator ondersteunt nu dynamische templates uit de API voor verschillende soorten regelingen, inclusief het nieuwe type "Bijzondere dag".

## Template Types

### 1. Feestdag
Algemene feestdagen zoals Kerst, Pasen, etc.

### 2. Vakantie
Schoolvakanties en vakantieperiodes

### 3. Algemeen
Algemene afspraken

### 4. Bijzondere dag
Speciale dagen met subtypes:
- Vaderdag
- Moederdag
- Sinterklaas
- Verjaardag kind
- Verjaardag ouder
- Overige bijzondere dag

## Placeholder System

### Nieuwe placeholders

#### {KIND} / {KINDEREN}
- `{KIND}`: Gebruikt enkelvoud als er 1 kind is, anders meervoud
- `{KINDEREN}`: Altijd meervoud vorm

Voorbeelden:
- 1 kind: "{KIND} verblijft op Vaderdag bij vader" → "Emma verblijft op Vaderdag bij vader"
- 2+ kinderen: "{KIND} verblijven op Vaderdag bij vader" → "Emma en Luuk verblijven op Vaderdag bij vader"

### Werkwoordsvervoegingen

De volgende werkwoordsvervoegingen worden automatisch aangepast:
- `{heeft/hebben}`
- `{is/zijn}`
- `{verblijft/verblijven}`
- `{kan/kunnen}`
- `{zal/zullen}` / `{zou/zouden}`
- `{moet/moeten}`
- `{wordt/worden}`
- `{blijft/blijven}`
- `{gaat/gaan}`
- `{komt/komen}`
- `{wil/willen}`
- `{mag/mogen}`
- `{doet/doen}`
- `{krijgt/krijgen}`
- `{neemt/nemen}`
- `{brengt/brengen}`
- `{haalt/halen}`

### Andere placeholders

- `{PARTIJ1}` / `{PARTIJ2}`: Namen van de ouders
- `{FEESTDAG}`: Naam van de feestdag/bijzondere dag
- `{DATUM}`: Relevante datum
- `{JAAR}`: Relevant jaar

## API Integration

Templates worden opgehaald via:
```
GET /api/lookups/regelingen-templates?type=Bijzondere+dag&meervoudKinderen=false
```

Response:
```json
[
  {
    "id": 1,
    "templateTekst": "{KIND} {verblijft/verblijven} op Vaderdag bij vader.",
    "templateNaam": "vaderdag_vader",
    "templateSubtype": "Vaderdag",
    "type": "Bijzondere dag",
    "meervoudKinderen": false
  }
]
```

## Code Voorbeeld

```csharp
// Inject de service
private readonly IRegelingenTemplateService _templateService;

// Haal templates op
var templates = await _templateService.GetTemplatesByTypeAsync("Bijzondere dag", meervoudKinderen: false);

// Of haal specifieke template op
var vaderdagTemplate = await _templateService.GetTemplateBySubtypeAsync(
    "Bijzondere dag", 
    "Vaderdag", 
    meervoudKinderen: kinderen.Count > 1
);

// Gebruik de template tekst
if (vaderdagTemplate != null)
{
    var templateText = vaderdagTemplate.TemplateTekst;
    // Template text wordt automatisch verwerkt door PlaceholderProcessor
}
```

## Belangrijke punten

1. **Enkelvoud/Meervoud**: Altijd de juiste `meervoudKinderen` parameter meegeven op basis van aantal kinderen
2. **Placeholder formaat**: Gebruik altijd accolades `{placeholder}` voor nieuwe templates
3. **Subtypes**: Voor "Bijzondere dag" altijd het juiste subtype specificeren
4. **Fallback**: Als geen template gevonden wordt, gebruik een default tekst