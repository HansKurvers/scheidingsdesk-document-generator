# Update: Meerdere Overige Afspraken

## Wijziging Overzicht

De document generator ondersteunt nu **meerdere "overige afspraken"** per dossier.

### Wat is veranderd?

**OUD**: Maximaal één "overige afspraak" (zorgSituatieId: 15) per dossier
**NIEUW**: Onbeperkt aantal "overige afspraken" per dossier

### Technische implementatie

De `ZorgTableGenerator` is aangepast om:

1. **Overige afspraken te scheiden** van reguliere zorg categorieën
   ```csharp
   var overigeAfspraken = data.Zorg.Where(z => z.ZorgSituatieId == 15).ToList();
   var regularZorg = data.Zorg.Where(z => z.ZorgSituatieId != 15).ToList();
   ```

2. **Elke overige afspraak eigen sectie** te geven
   - Hoofdkop: "Overige afspraken"
   - Subkop: Waarde van `SituatieAnders` veld
   - Inhoud: De afspraak tekst in een tabel

3. **SituatieAnders als titel** te gebruiken voor betere organisatie

## Document Output Voorbeeld

```
## Overige afspraken

### Zwemles afspraken
┌─────────────────────────────────────┐
│ De kinderen gaan elke zaterdag      │
│ naar zwemles om 10:00 uur.          │
└─────────────────────────────────────┘

### Muziekles afspraken  
┌─────────────────────────────────────┐
│ Emma heeft pianoles op woensdag     │
│ om 16:00 uur bij muziekschool.      │
└─────────────────────────────────────┘

### Sportclub regeling
┌─────────────────────────────────────┐
│ Luuk traint op dinsdag en           │
│ donderdag bij voetbalclub.          │
└─────────────────────────────────────┘
```

## Database Structuur

Overige afspraken worden opgeslagen in de `zorg` tabel met:
- `zorg_situatie_id`: 15 (vaste waarde voor "overige afspraken")
- `situatie_anders`: Titel/naam van de afspraak (bijv. "Zwemles afspraken")
- `overeenkomst`: De daadwerkelijke afspraak tekst

## Voordelen

1. **Onbeperkt aantal** overige afspraken mogelijk
2. **Betere organisatie** door gebruik van subtitels
3. **Flexibiliteit** voor mediators om alle relevante afspraken op te nemen
4. **Duidelijke structuur** in het gegenereerde document