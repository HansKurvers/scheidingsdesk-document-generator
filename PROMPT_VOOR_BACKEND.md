# Prompt voor Claude Code Backend: Database Migratie Kinderrekening Velden

## Context

De document generator (i-docx document-generator, repository: scheidingsdesk-document-generator) is bijgewerkt met 10 nieuwe kinderrekening velden. De code is backwards compatible en kan omgaan met ontbrekende kolommen. Nu moeten de database kolommen worden toegevoegd zodat de frontend data kan opslaan en de document generator de data kan ophalen.

## Database Informatie

**Database**: `ouderschapsplan_db` (Azure SQL Database)
**Tabel**: `dbo.alimentaties`
**API**: `ouderschaps-api` repository

## Te Voegen Kolommen

Voeg de volgende 10 kolommen toe aan de `dbo.alimentaties` tabel:

### 1. Kinderrekening Stortingen (2 kolommen)
```sql
storting_ouder1_kinderrekening DECIMAL(10,2) NULL
storting_ouder2_kinderrekening DECIMAL(10,2) NULL
```

### 2. Kostensoorten (1 kolom - JSON array)
```sql
kinderrekening_kostensoorten NVARCHAR(MAX) NULL
```
**Opmerking**: Bevat JSON array van strings, bijvoorbeeld:
```json
["Kinderopvang kosten (onder werktijd)", "Kleding, schoenen, kapper en persoonlijke verzorging"]
```

### 3. Maximum Opname Settings (2 kolommen)
```sql
kinderrekening_maximum_opname BIT NULL
kinderrekening_maximum_opname_bedrag DECIMAL(10,2) NULL
```

### 4. Kinderbijslag/Kindgebonden Budget (2 kolommen)
```sql
kinderbijslag_storten_op_kinderrekening BIT NULL
kindgebonden_budget_storten_op_kinderrekening BIT NULL
```

### 5. Alimentatie Settings (3 kolommen)
```sql
bedragen_alle_kinderen_gelijk BIT NULL
alimentatiebedrag_per_kind DECIMAL(10,2) NULL
alimentatiegerechtigde VARCHAR(255) NULL
```

## Complete SQL Migratie Script

```sql
-- Migratie: Kinderrekening velden toevoegen aan alimentaties tabel
-- Datum: 2025-10-29
-- Versie: 1.0

USE ouderschapsplan_db;
GO

-- Check of de kolommen al bestaan voordat je ze toevoegt
-- Dit voorkomt errors bij herhaalde uitvoering

-- 1. Kinderrekening stortingen
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'storting_ouder1_kinderrekening')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD storting_ouder1_kinderrekening DECIMAL(10,2) NULL;
    PRINT 'Column storting_ouder1_kinderrekening added';
END
ELSE
    PRINT 'Column storting_ouder1_kinderrekening already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'storting_ouder2_kinderrekening')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD storting_ouder2_kinderrekening DECIMAL(10,2) NULL;
    PRINT 'Column storting_ouder2_kinderrekening added';
END
ELSE
    PRINT 'Column storting_ouder2_kinderrekening already exists';

-- 2. Kostensoorten (JSON array)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'kinderrekening_kostensoorten')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD kinderrekening_kostensoorten NVARCHAR(MAX) NULL;
    PRINT 'Column kinderrekening_kostensoorten added';
END
ELSE
    PRINT 'Column kinderrekening_kostensoorten already exists';

-- 3. Maximum opname settings
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'kinderrekening_maximum_opname')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD kinderrekening_maximum_opname BIT NULL;
    PRINT 'Column kinderrekening_maximum_opname added';
END
ELSE
    PRINT 'Column kinderrekening_maximum_opname already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'kinderrekening_maximum_opname_bedrag')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD kinderrekening_maximum_opname_bedrag DECIMAL(10,2) NULL;
    PRINT 'Column kinderrekening_maximum_opname_bedrag added';
END
ELSE
    PRINT 'Column kinderrekening_maximum_opname_bedrag already exists';

-- 4. Kinderbijslag/kindgebonden budget
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'kinderbijslag_storten_op_kinderrekening')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD kinderbijslag_storten_op_kinderrekening BIT NULL;
    PRINT 'Column kinderbijslag_storten_op_kinderrekening added';
END
ELSE
    PRINT 'Column kinderbijslag_storten_op_kinderrekening already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'kindgebonden_budget_storten_op_kinderrekening')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD kindgebonden_budget_storten_op_kinderrekening BIT NULL;
    PRINT 'Column kindgebonden_budget_storten_op_kinderrekening added';
END
ELSE
    PRINT 'Column kindgebonden_budget_storten_op_kinderrekening already exists';

-- 5. Alimentatie settings
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'bedragen_alle_kinderen_gelijk')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD bedragen_alle_kinderen_gelijk BIT NULL;
    PRINT 'Column bedragen_alle_kinderen_gelijk added';
END
ELSE
    PRINT 'Column bedragen_alle_kinderen_gelijk already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'alimentatiebedrag_per_kind')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD alimentatiebedrag_per_kind DECIMAL(10,2) NULL;
    PRINT 'Column alimentatiebedrag_per_kind added';
END
ELSE
    PRINT 'Column alimentatiebedrag_per_kind already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.alimentaties') AND name = 'alimentatiegerechtigde')
BEGIN
    ALTER TABLE dbo.alimentaties
    ADD alimentatiegerechtigde VARCHAR(255) NULL;
    PRINT 'Column alimentatiegerechtigde added';
END
ELSE
    PRINT 'Column alimentatiegerechtigde already exists';

PRINT '';
PRINT 'Migration completed successfully!';
PRINT 'All 10 kinderrekening columns have been added to dbo.alimentaties table';
GO
```

## API Updates Nodig (ouderschaps-api)

### 1. Update TypeScript Interface

**Locatie**: Waarschijnlijk in `src/models/` of `src/types/`

**Te updaten interface**:
```typescript
interface Alimentatie {
  id: number;
  dossierId: number;
  nettoBesteedbaarGezinsinkomen: number | null;
  kostenKinderen: number | null;
  bijdrageTemplateId: number | null;

  // NIEUWE VELDEN - Kinderrekening
  stortingOuder1Kinderrekening: number | null;
  stortingOuder2Kinderrekening: number | null;
  kinderrekeningKostensoorten: string[]; // JSON array van strings
  kinderrekeningMaximumOpname: boolean | null;
  kinderrekeningMaximumOpnameBedrag: number | null;
  kinderbijslagStortenOpKinderrekening: boolean | null;
  kindgebondenBudgetStortenOpKinderrekening: boolean | null;

  // NIEUWE VELDEN - Alimentatie settings
  bedragenAlleKinderenGelijk: boolean | null;
  alimentatiebedragPerKind: number | null;
  alimentatiegerechtigde: string | null;
}
```

### 2. Update SQL Queries

**Locatie**: API endpoints die alimentatie data ophalen/opslaan

**SELECT queries updaten** om nieuwe kolommen op te halen:
```sql
SELECT
  id,
  dossier_id,
  netto_besteedbaar_gezinsinkomen,
  kosten_kinderen,
  bijdrage_template,
  -- NIEUWE KOLOMMEN
  storting_ouder1_kinderrekening,
  storting_ouder2_kinderrekening,
  kinderrekening_kostensoorten,
  kinderrekening_maximum_opname,
  kinderrekening_maximum_opname_bedrag,
  kinderbijslag_storten_op_kinderrekening,
  kindgebonden_budget_storten_op_kinderrekening,
  bedragen_alle_kinderen_gelijk,
  alimentatiebedrag_per_kind,
  alimentatiegerechtigde
FROM dbo.alimentaties
WHERE dossier_id = @dossierId;
```

**INSERT/UPDATE queries updaten** om nieuwe kolommen op te slaan:
```sql
INSERT INTO dbo.alimentaties (
  dossier_id,
  netto_besteedbaar_gezinsinkomen,
  kosten_kinderen,
  -- NIEUWE KOLOMMEN
  storting_ouder1_kinderrekening,
  storting_ouder2_kinderrekening,
  kinderrekening_kostensoorten,
  kinderrekening_maximum_opname,
  kinderrekening_maximum_opname_bedrag,
  kinderbijslag_storten_op_kinderrekening,
  kindgebonden_budget_storten_op_kinderrekening,
  bedragen_alle_kinderen_gelijk,
  alimentatiebedrag_per_kind,
  alimentatiegerechtigde
) VALUES (
  @dossierId,
  @nettoBesteedbaarGezinsinkomen,
  @kostenKinderen,
  -- NIEUWE WAARDEN
  @stortingOuder1Kinderrekening,
  @stortingOuder2Kinderrekening,
  @kinderrekeningKostensoorten, -- JSON string!
  @kinderrekeningMaximumOpname,
  @kinderrekeningMaximumOpnameBedrag,
  @kinderbijslagStortenOpKinderrekening,
  @kindgebondenBudgetStortenOpKinderrekening,
  @bedragenAlleKinderenGelijk,
  @alimentatiebedragPerKind,
  @alimentatiegerechtigde
);
```

### 3. JSON Handling voor kinderrekening_kostensoorten

**Belangrijk**: `kinderrekening_kostensoorten` is een JSON array van strings.

**Bij opslaan** (TypeScript naar SQL):
```typescript
const kostensoorten = ["Kinderopvang kosten", "Kleding"];
const jsonString = JSON.stringify(kostensoorten);
// Sla op als: '["Kinderopvang kosten","Kleding"]'
```

**Bij ophalen** (SQL naar TypeScript):
```typescript
const jsonString = row.kinderrekening_kostensoorten;
const kostensoorten = jsonString ? JSON.parse(jsonString) : [];
```

## API Endpoint Response Format

De API moet de data teruggeven in dit format (zoals frontend verwacht):

```json
{
  "alimentatie": {
    "id": 1,
    "dossierId": 69,
    "nettoBesteedbaarGezinsinkomen": 5000.00,
    "kostenKinderen": 1500.00,
    "bijdrageTemplateId": 5,

    "stortingOuder1Kinderrekening": 350.00,
    "stortingOuder2Kinderrekening": 350.00,
    "kinderrekeningKostensoorten": [
      "Kinderopvang kosten (onder werktijd)",
      "Kleding, schoenen, kapper en persoonlijke verzorging"
    ],
    "kinderrekeningMaximumOpname": true,
    "kinderrekeningMaximumOpnameBedrag": 250.00,
    "kinderbijslagStortenOpKinderrekening": true,
    "kindgebondenBudgetStortenOpKinderrekening": false,
    "bedragenAlleKinderenGelijk": false,
    "alimentatiebedragPerKind": null,
    "alimentatiegerechtigde": null
  }
}
```

## Verificatie na Migratie

### 1. Test in Azure Portal
```sql
-- Check of alle kolommen zijn toegevoegd
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'alimentaties'
AND COLUMN_NAME IN (
  'storting_ouder1_kinderrekening',
  'storting_ouder2_kinderrekening',
  'kinderrekening_kostensoorten',
  'kinderrekening_maximum_opname',
  'kinderrekening_maximum_opname_bedrag',
  'kinderbijslag_storten_op_kinderrekening',
  'kindgebonden_budget_storten_op_kinderrekening',
  'bedragen_alle_kinderen_gelijk',
  'alimentatiebedrag_per_kind',
  'alimentatiegerechtigde'
)
ORDER BY COLUMN_NAME;
```

Verwacht resultaat: 10 rijen met de nieuwe kolommen

### 2. Test API Endpoint
```bash
# GET request naar alimentatie endpoint
GET /api/dossiers/69/alimentatie

# Verwacht: Response bevat alle 10 nieuwe velden (met null waarden voor bestaande records)
```

### 3. Test Document Generator
Na de migratie en API updates:
1. Frontend: Vul kinderrekening data in voor een dossier
2. Document generator: Download document
3. Verwacht: Kinderrekening sectie verschijnt in document met ingevulde data

## Rollback Plan (indien nodig)

Als er problemen zijn, kunnen de kolommen verwijderd worden:

```sql
USE ouderschapsplan_db;
GO

-- Verwijder alle 10 kolommen (in omgekeerde volgorde)
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS alimentatiegerechtigde;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS alimentatiebedrag_per_kind;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS bedragen_alle_kinderen_gelijk;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS kindgebonden_budget_storten_op_kinderrekening;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS kinderbijslag_storten_op_kinderrekening;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS kinderrekening_maximum_opname_bedrag;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS kinderrekening_maximum_opname;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS kinderrekening_kostensoorten;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS storting_ouder2_kinderrekening;
ALTER TABLE dbo.alimentaties DROP COLUMN IF EXISTS storting_ouder1_kinderrekening;

PRINT 'Rollback completed - All kinderrekening columns removed';
GO
```

## Belangrijke Notities

1. ✅ **Document Generator is klaar**: De code is al backwards compatible en kan omgaan met NULL waarden
2. ⚠️ **API moet worden bijgewerkt**: TypeScript interfaces + SQL queries moeten alle 10 velden bevatten
3. ✅ **Frontend is klaar**: UI is al gebouwd en klaar om data op te slaan
4. 🎯 **Na migratie**: Volledige flow werkt: Frontend → API → Database → Document Generator

## Test Checklist

- [ ] Database migratie uitgevoerd (10 kolommen toegevoegd)
- [ ] API TypeScript interface bijgewerkt
- [ ] API GET endpoint retourneert nieuwe velden
- [ ] API POST/PUT endpoints accepteren nieuwe velden
- [ ] JSON parsing werkt voor kinderrekening_kostensoorten
- [ ] Frontend kan data opslaan
- [ ] Frontend kan opgeslagen data ophalen
- [ ] Document generator toont kinderrekening sectie
- [ ] Document generator toont conditionele kolommen correct

## Contact

Bij vragen over de document generator implementatie, zie:
- Repository: `scheidingsdesk-document-generator` (i-docx document generator)
- Documentatie: `DEPLOYMENT.md`
- Database service code: `Services/DatabaseService.cs` (regels 314-327)
