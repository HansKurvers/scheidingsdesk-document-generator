# Deployment Configuratie

## 🎯 Welke Azure Function gebruiken?

**Gebruik: `mediation-document-generator`**

### Waarom?

✅ **mediation-document-generator** is de primaire, actieve Azure Function voor dit project:
- Dit is de Azure Function die door de frontend wordt gebruikt
- Workflow: `deploy-mediation.yml` (voorheen `main_mediation-document-generator.yml`)
- Werkt met automatische deployments (geactiveerd in oktober 2025)
- **URL**: `https://mediation-document-generator.azurewebsites.net/api/ouderschapsplan`

ℹ️ **scheidingsdesk-document-service** is een alternatieve/test function:
- Andere Azure Function die parallel draait
- Workflow was gebruikt tijdens ontwikkeling
- Kan gebruikt worden voor testing
- **URL**: `https://scheidingsdesk-document-service.azurewebsites.net/api/ouderschapsplan`
- **Note**: Legacy naming, app is nu i-docx

## 📋 Frontend Configuratie

### Actieve URL (PRIMARY - mediation-document-generator):
```
https://mediation-document-generator.azurewebsites.net/api/ouderschapsplan?code=XXX
```

### Alternatieve URL (TESTING - scheidingsdesk-document-service):
```
https://scheidingsdesk-document-service.azurewebsites.net/api/ouderschapsplan?code=XXX
```

### Frontend Environment Variabele

**Locatie**: `ouderschaps-web/.env`

**Variabele**: `VITE_DOCUMENT_GENERATOR_URL`

**Standaard configuratie**:
```env
VITE_DOCUMENT_GENERATOR_URL=https://mediation-document-generator.azurewebsites.net/api/ouderschapsplan?code=FUNCTION_CODE_HIER
```

⚠️ **Let op**: Je hebt de Azure Function Code nodig voor `mediation-document-generator`. Deze vind je in:
- Azure Portal → Function Apps → mediation-document-generator → Functions → ouderschapsplan → Function Keys

## 🔄 GitHub Actions Workflows

### Primaire Workflow: `deploy-mediation.yml` (voorheen `main_mediation-document-generator.yml`)
- **Trigger**: Automatisch bij push naar `main` + handmatig via `workflow_dispatch`
- **Target**: `mediation-document-generator` Azure Function ✅
- **Status**: ✅ Actief en werkend (geactiveerd oktober 2025)

### Alternatieve Workflow: `main_scheidingsdesk-document-service.yml`
- **Trigger**: Handmatig via `workflow_dispatch` (push trigger is disabled)
- **Target**: `scheidingsdesk-document-service` Azure Function
- **Status**: ⚠️ Alleen voor testing/development

## 📝 Deployment Proces

1. **Maak wijzigingen** in code
2. **Commit en push** naar `main` branch
3. **GitHub Actions** triggert automatisch `deploy-mediation.yml`
4. **Deployment** naar `mediation-document-generator` binnen ~2 minuten
5. **Nieuwe code** is live op `mediation-document-generator.azurewebsites.net`

## 🧪 Testen na Deployment

1. Check GitHub Actions run status: https://github.com/HansKurvers/scheidingsdesk-document-generator/actions (repository naam nog niet gewijzigd)
2. Test document generatie via frontend
3. Check Azure Application Insights logs voor errors

## 📚 Geschiedenis

- **Tot september 2025**: Gebruikt `mediation-document-generator` (workflow werkte)
- **September 2025**: Workflow `mediation-document-generator` had technische problemen
- **Oktober 2025**: Workflow `mediation-document-generator` opnieuw geactiveerd en werkend
- **Oktober 2025**: Kinderrekening functionaliteit toegevoegd met backwards compatibility
- **29 oktober 2025**: Bevestigd `mediation-document-generator` als primaire Azure Function voor dit project

## ⚙️ Azure Function Instellingen

### mediation-document-generator (PRIMAIR)
- **Tier**: Flex Consumption Plan
- **Runtime**: .NET 9.0
- **Region**: West Europe
- **Function Key**: Zie Azure Portal
- **Status**: ✅ Actief voor productie

### scheidingsdesk-document-service (ALTERNATIEF/TESTING)
- **Tier**: Consumption Plan
- **Runtime**: .NET 9.0
- **Region**: West Europe
- **Function Key**: Zie Azure Portal
- **Status**: ⚠️ Gebruikt voor development/testing

## 🔐 Secrets & Configuration

GitHub Secrets nodig voor deployment naar mediation-document-generator:
- `AZUREAPPSERVICE_CLIENTID_A821858039D9459B91B5D1B42EF3FFA6`
- `AZUREAPPSERVICE_TENANTID_E96E1C556AEA43A387C60396E8E3C0A6`
- `AZUREAPPSERVICE_SUBSCRIPTIONID_7518C02736D4430B93ADF6172819C893`

Deze zijn al geconfigureerd in GitHub repository settings voor beide Azure Functions.
