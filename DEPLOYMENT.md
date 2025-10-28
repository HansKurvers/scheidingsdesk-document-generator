# Deployment Configuratie

## 🎯 Welke Azure Function gebruiken?

**Gebruik: `scheidingsdesk-document-service`**

### Waarom?

✅ **scheidingsdesk-document-service** is de primaire, actieve Azure Function:
- Automatische deployments werken perfect bij elke push naar `main`
- Alle nieuwe features en fixes worden hier automatisch gedeployed
- Bewezen stabiliteit: 12+ succesvolle deployments sinds mei 2025
- **URL**: `https://scheidingsdesk-document-service.azurewebsites.net/api/ouderschapsplan`

❌ **mediation-document-generator** is de oude/alternatieve function:
- Workflow was lange tijd niet werkend (tot oktober 2025)
- Kreeg geen automatische updates
- Verouderde code
- Alleen voor backwards compatibility

## 📋 Frontend Configuratie

### Huidige URL (OUDE - mediation-document-generator):
```
https://mediation-document-generator.azurewebsites.net/api/ouderschapsplan?code=XXX
```

### Nieuwe URL (AANBEVOLEN - scheidingsdesk-document-service):
```
https://scheidingsdesk-document-service.azurewebsites.net/api/ouderschapsplan?code=XXX
```

### Frontend Environment Variabele

**Locatie**: `ouderschaps-web/.env`

**Variabele**: `VITE_DOCUMENT_GENERATOR_URL`

**Te wijzigen naar**:
```env
VITE_DOCUMENT_GENERATOR_URL=https://scheidingsdesk-document-service.azurewebsites.net/api/ouderschapsplan?code=FUNCTION_CODE_HIER
```

⚠️ **Let op**: Je hebt de Azure Function Code nodig voor `scheidingsdesk-document-service`. Deze vind je in:
- Azure Portal → Function Apps → scheidingsdesk-document-service → Functions → ouderschapsplan → Function Keys

## 🔄 GitHub Actions Workflows

### Actieve Workflow: `main_scheidingsdesk-document-service.yml`
- **Trigger**: Automatisch bij push naar `main`
- **Target**: `scheidingsdesk-document-service` Azure Function
- **Status**: ✅ Actief en werkend

### Inactieve Workflow: `deploy-mediation.yml` (voorheen `main_mediation-document-generator.yml`)
- **Trigger**: Alleen handmatig via `workflow_dispatch`
- **Target**: `mediation-document-generator` Azure Function
- **Status**: ⚠️ Disabled voor automatische deployments

## 📝 Deployment Proces

1. **Maak wijzigingen** in code
2. **Commit en push** naar `main` branch
3. **GitHub Actions** triggert automatisch `main_scheidingsdesk-document-service.yml`
4. **Deployment** naar `scheidingsdesk-document-service` binnen ~2 minuten
5. **Nieuwe code** is live op `scheidingsdesk-document-service.azurewebsites.net`

## 🧪 Testen na Deployment

1. Check GitHub Actions run status: https://github.com/HansKurvers/scheidingsdesk-document-generator/actions
2. Test document generatie via frontend
3. Check Azure Application Insights logs voor errors

## 📚 Geschiedenis

- **September 2025**: Poging om over te schakelen naar `mediation-document-generator` (workflow werkte niet)
- **Oktober 2025**: Teruggeschakeld naar `scheidingsdesk-document-service` (bewezen stabiel)
- **Oktober 2025**: Kinderrekening functionaliteit toegevoegd en gedeployed naar beide functions

## ⚙️ Azure Function Instellingen

### scheidingsdesk-document-service (PRIMAIR)
- **Tier**: Consumption Plan
- **Runtime**: .NET 9.0
- **Region**: West Europe
- **Function Key**: Zie Azure Portal

### mediation-document-generator (BACKWARDS COMPATIBILITY)
- **Tier**: Flex Consumption Plan
- **Runtime**: .NET 9.0
- **Region**: West Europe (waarschijnlijk)
- **Function Key**: Zie Azure Portal

## 🔐 Secrets & Configuration

GitHub Secrets nodig voor deployment (scheidingsdesk-document-service):
- `AZUREAPPSERVICE_CLIENTID_XXX`
- `AZUREAPPSERVICE_TENANTID_XXX`
- `AZUREAPPSERVICE_SUBSCRIPTIONID_XXX`

Deze zijn al geconfigureerd in GitHub repository settings.
