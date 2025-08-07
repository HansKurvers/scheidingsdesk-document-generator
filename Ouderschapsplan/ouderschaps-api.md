# Ouderschaps-Api

Dit is een api die data ophaalt.

## Azure Functions v4 Setup

This project is built with Azure Functions v4, Node.js, and TypeScript for managing dossiers.

### Prerequisites

- Node.js 18+
- Azure Functions Core Tools v4
- MongoDB instance (local or cloud)
- Azure Storage Emulator or Azurite (for local development)

### Installation

```bash
npm install
```

### Configuration

Ensure `local.settings.json` has the correct MongoDB connection string:

```json
{
  "Values": {
    "MONGODB_URI": "mongodb://localhost:27017/ouderschaps-db"
  }
}
```

### Development

Build: `npm run build`
Watch: `npm run watch`
Start: `npm start`

## API Documentation

### Authentication
Most endpoints require authentication via the `x-user-id` header. Only health check and lookup endpoints are publicly accessible.

### Endpoints

#### Health Check
- **GET** `/api/health` - Health check
  - **Authentication**: None
  - **Response**: Health status with version, timestamp, and environment info

#### Dossier Management

- **GET** `/api/dossiers` - Get dossiers with optional filters
  - **Authentication**: Required (`x-user-id` header)
  - **Query Parameters**:
    - `includeInactive` (optional): Include both active and inactive dossiers - boolean (default: false)
    - `onlyInactive` (optional): Show only inactive/completed dossiers - boolean (default: false)
    - `limit` (optional): Number of results (1-100, default 10)
    - `offset` (optional): Pagination offset (default 0)
  - **Response**: Array of dossiers for authenticated user
  - **Default behavior**: Returns only active dossiers (status = false)

- **POST** `/api/dossiers` - Create new dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: Dossier data
  - **Response**: Created dossier object

- **GET** `/api/dossiers/{dossierId}` - Get specific dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Detailed dossier information (user access required)

- **PUT** `/api/dossiers/{dossierId}` - Update dossier status
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: `{ "status": boolean }` (false = active/in progress, true = completed)
  - **Response**: Updated dossier object

- **DELETE** `/api/dossiers/{dossierId}` - Delete dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Deletion confirmation

#### Dossier Parties Management

- **GET** `/api/dossiers/{dossierId}/partijen` - Get dossier parties
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Array of parties (persons with roles) for the dossier

- **POST** `/api/dossiers/{dossierId}/partijen` - Add party to dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: Either `{ "persoonId": "id", "rolId": "id" }` or `{ "persoonData": {...}, "rolId": "id" }`
  - **Response**: Created party association

- **DELETE** `/api/dossiers/{dossierId}/partijen/{partijId}` - Remove party from dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Removal confirmation

#### Person Management

- **GET** `/api/personen` - Get all personen with pagination
  - **Authentication**: Required (`x-user-id` header)
  - **Query Parameters**:
    - `limit` (optional): Number of results (1-100, default 50)
    - `offset` (optional): Pagination offset (default 0)
  - **Response**: Array of personen with pagination metadata

- **POST** `/api/personen` - Create new person
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: Person data with email validation
  - **Response**: Created person object

- **GET** `/api/personen/{persoonId}` - Get specific person
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Detailed person information

- **PUT** `/api/personen/{persoonId}` - Update person
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: Updated person data
  - **Response**: Updated person object

- **DELETE** `/api/personen/{persoonId}` - Delete person
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Deletion confirmation

#### FASE 3: Children & Parent-Child Relationships

- **GET** `/api/dossiers/{dossierId}/kinderen` - Get children in dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Array of children with their parent relationships

- **POST** `/api/dossiers/{dossierId}/kinderen` - Add child to dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: Either `{ "kindId": "id", "ouderRelaties": [...] }` or `{ "kindData": {...}, "ouderRelaties": [...] }`
  - **Response**: Created child association with parent relationships

- **DELETE** `/api/dossiers/{dossierId}/kinderen/{dossierKindId}` - Remove child from dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Removal confirmation

- **GET** `/api/kinderen/{kindId}/ouders` - Get parents of a child
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Array of parent relationships for the child

- **POST** `/api/kinderen/{kindId}/ouders` - Add parent to child
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: `{ "ouderId": "id", "relatieTypeId": "id" }`
  - **Response**: Created parent-child relationship

- **PUT** `/api/kinderen/{kindId}/ouders/{ouderId}` - Update parent-child relationship
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: `{ "relatieTypeId": "id" }`
  - **Response**: Updated relationship

- **DELETE** `/api/kinderen/{kindId}/ouders/{ouderId}` - Remove parent from child
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Removal confirmation

#### FASE 4: Visitation & Care (Omgang & Zorg)

##### Visitation Schedules (Omgang)

- **GET** `/api/dossiers/{dossierId}/omgang` - Get visitation schedules for dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Array of visitation schedules with details

- **POST** `/api/dossiers/{dossierId}/omgang` - Create visitation schedule
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: `{ "dagId": 1-7, "dagdeelId": "id", "verzorgerId": "id", "wisselTijd": "HH:MM", "weekRegelingId": "id", "weekRegelingAnders": "string" }`
  - **Response**: Created visitation schedule

- **PUT** `/api/dossiers/{dossierId}/omgang/{omgangId}` - Update visitation schedule
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: Partial visitation schedule data
  - **Response**: Updated visitation schedule

- **DELETE** `/api/dossiers/{dossierId}/omgang/{omgangId}` - Delete visitation schedule
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Deletion confirmation

##### Care Arrangements (Zorg)

- **GET** `/api/dossiers/{dossierId}/zorg` - Get care arrangements for dossier
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Array of care arrangements with details

- **POST** `/api/dossiers/{dossierId}/zorg` - Create care arrangement
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: `{ "zorgCategorieId": "id", "zorgSituatieId": "id", "situatieAnders": "string", "overeenkomst": "string" }`
  - **Response**: Created care arrangement

- **PUT** `/api/dossiers/{dossierId}/zorg/{zorgId}` - Update care arrangement
  - **Authentication**: Required (`x-user-id` header)
  - **Request Body**: Partial care arrangement data
  - **Response**: Updated care arrangement

- **DELETE** `/api/dossiers/{dossierId}/zorg/{zorgId}` - Delete care arrangement
  - **Authentication**: Required (`x-user-id` header)
  - **Response**: Deletion confirmation

#### Lookup Data

- **GET** `/api/rollen` - Get available roles
  - **Authentication**: None
  - **Response**: Array of available roles for dossier parties
  - **Note**: Response is cached for 5 minutes for performance

- **GET** `/api/relatie-types` - Get relationship types
  - **Authentication**: None
  - **Response**: Array of parent-child relationship types
  - **Note**: Response is cached for 5 minutes for performance

- **GET** `/api/dagen` - Get days of the week
  - **Authentication**: None
  - **Response**: Array of days (1-7 for Monday-Sunday)
  - **Note**: Response is cached for 5 minutes for performance

- **GET** `/api/dagdelen` - Get parts of day
  - **Authentication**: None
  - **Response**: Array of day parts (morning, afternoon, evening, etc.)
  - **Note**: Response is cached for 5 minutes for performance

- **GET** `/api/week-regelingen` - Get week arrangements
  - **Authentication**: None
  - **Response**: Array of weekly visitation arrangements
  - **Note**: Response is cached for 5 minutes for performance

- **GET** `/api/zorg-categorieen` - Get care categories
  - **Authentication**: None
  - **Response**: Array of care categories
  - **Note**: Response is cached for 5 minutes for performance

- **GET** `/api/zorg-situaties` - Get care situations
  - **Authentication**: None
  - **Query Parameters**: `categorieId` (optional) - Filter by care category
  - **Response**: Array of care situations
  - **Note**: Response is cached for 5 minutes for performance

### Error Handling

All endpoints return appropriate HTTP status codes:
- `200` - Success
- `201` - Created
- `400` - Bad Request (validation errors)
- `401` - Unauthorized
- `403` - Forbidden (access denied)
- `404` - Not Found
- `500` - Internal Server Error

### Request Validation

All endpoints use Joi validation for request data. Invalid requests return detailed error messages with status code 400.
