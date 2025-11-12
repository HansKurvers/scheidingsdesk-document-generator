# API Endpoints Documentation

## Template Management Endpoints

### Get Available Template Types
Retrieves all available template types from the database.

**Endpoint:** `GET /api/template-types`

**Authorization:** Function key required

**Response:**
```json
{
  "templateTypes": ["Feestdag", "Vakantie", "Algemeen", "Bijzondere dag"],
  "count": 4,
  "correlationId": "uuid-string"
}
```

**Default Types:**
- `Feestdag` - Holiday templates
- `Vakantie` - Vacation templates  
- `Algemeen` - General templates
- `Bijzondere dag` - Special day templates

### Get Templates by Type
Retrieves all templates for a specific type.

**Endpoint:** `GET /api/templates/{templateType}`

**Authorization:** Function key required

**Parameters:**
- `templateType` (path parameter) - The type of templates to retrieve (URL-encoded if contains spaces)

**Examples:**
- `/api/templates/Feestdag`
- `/api/templates/Vakantie`
- `/api/templates/Algemeen`
- `/api/templates/Bijzondere%20dag` (URL-encoded for "Bijzondere dag")

**Response:**
```json
{
  "type": "Feestdag",
  "templates": [
    {
      "id": 1,
      "templateNaam": "partij1",
      "templateTekst": "{KIND} is tijdens {FEESTDAG} bij {PARTIJ1}",
      "meervoudKinderen": false,
      "type": "Feestdag"
    },
    {
      "id": 2,
      "templateNaam": "partij1_kinderen",
      "templateTekst": "De kinderen zijn tijdens {FEESTDAG} bij {PARTIJ1}",
      "meervoudKinderen": true,
      "type": "Feestdag"
    }
  ],
  "count": 2,
  "correlationId": "uuid-string"
}
```

## Adding New Template Types

To add a new template type like "Bijzondere dag":

1. **Database:** Add templates to the `dbo.regelingen_templates` table with the new type:
   ```sql
   INSERT INTO dbo.regelingen_templates (template_naam, template_tekst, meervoud_kinderen, type)
   VALUES 
   ('partij1_bijzonder', '{KIND} is tijdens {BIJZONDERE_DAG} bij {PARTIJ1}', 0, 'Bijzondere dag'),
   ('partij1_bijzonder_kinderen', 'De kinderen zijn tijdens {BIJZONDERE_DAG} bij {PARTIJ1}', 1, 'Bijzondere dag');
   ```

2. **Code Constants:** The template type is already included in the default types in `TemplateTypes.cs`

3. **API Usage:** The new type will automatically be available through the API endpoints

## Template Placeholders

Templates support the following placeholders:
- `{KIND}` - Child's name (singular)
- `{KINDEREN}` - Children (plural)
- `{PARTIJ1}` - Party 1's name
- `{PARTIJ2}` - Party 2's name
- `{FEESTDAG}` - Holiday name
- `{VAKANTIE}` - Vacation name
- `{BIJZONDERE_DAG}` - Special day name

## Error Handling

All endpoints return consistent error responses:
```json
{
  "error": "Error message description",
  "correlationId": "uuid-string"
}
```

HTTP Status Codes:
- `200` - Success
- `400` - Bad Request (invalid parameters)
- `500` - Internal Server Error