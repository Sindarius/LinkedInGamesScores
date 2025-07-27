# LinkedIn Game Scores - Azure Deployment Guide

## Overview

This guide provides step-by-step instructions to deploy the LinkedIn Game Scores application to Microsoft Azure. The application consists of:

- **Frontend**: Vue.js 3 SPA with PrimeVue components
- **Backend**: .NET 8 WebAPI with Entity Framework Core
- **Database**: PostgreSQL
- **File Storage**: Azure Blob Storage (for score images)

## Architecture Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Azure CDN     │    │  Azure App       │    │  Azure Database │
│   (Frontend)    │◄───┤  Service         │◄───┤  for PostgreSQL │
│                 │    │  (.NET API)      │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │  Azure Blob     │
                       │  Storage        │
                       │  (Images)       │
                       └─────────────────┘
```

## Prerequisites

Before starting, ensure you have:

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- [Azure subscription](https://azure.microsoft.com/free/) with appropriate permissions
- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed
- [Node.js 18+](https://nodejs.org/) installed
- [.NET 8 SDK](https://dotnet.microsoft.com/download) installed

## Quick Start

1. **[Setup Azure Resources](#step-1-setup-azure-resources)**
2. **[Configure Database](#step-2-configure-postgresql-database)**
3. **[Deploy API](#step-3-deploy-net-api)**
4. **[Deploy Frontend](#step-4-deploy-vue-frontend)**
5. **[Configure Custom Domain & SSL](#step-5-configure-custom-domain--ssl)**

---

## Step 1: Setup Azure Resources

### 1.1 Login to Azure

```bash
# Login to Azure
az login

# Set your subscription (replace with your subscription ID)
az account set --subscription "your-subscription-id"
```

### 1.2 Create Resource Group

```bash
# Create resource group
az group create \
  --name "rg-linkedin-game-scores" \
  --location "East US"
```

### 1.3 Create Azure Container Registry (ACR)

```bash
# Create ACR for storing Docker images
az acr create \
  --resource-group "rg-linkedin-game-scores" \
  --name "acrlinkedingamescores" \
  --sku Basic \
  --admin-enabled true

# Get ACR login server
az acr show --name "acrlinkedingamescores" --query loginServer --output tsv
```

### 1.4 Create Storage Account

```bash
# Create storage account for blob storage
az storage account create \
  --name "salinkedingamescores" \
  --resource-group "rg-linkedin-game-scores" \
  --location "East US" \
  --sku Standard_LRS \
  --kind StorageV2

# Create blob container for score images
az storage container create \
  --name "score-images" \
  --account-name "salinkedingamescores" \
  --public-access off
```

---

## Step 2: Configure PostgreSQL Database

### 2.1 Create Azure Database for PostgreSQL

```bash
# Create PostgreSQL server
az postgres flexible-server create \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores" \
  --location "East US" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --version 15 \
  --admin-user gameadmin \
  --admin-password "YourSecurePassword123!" \
  --storage-size 32 \
  --public-access 0.0.0.0

# Create database
az postgres flexible-server db create \
  --resource-group "rg-linkedin-game-scores" \
  --server-name "psql-linkedin-game-scores" \
  --database-name "gamescores"
```

### 2.2 Configure Firewall Rules

```bash
# Allow Azure services
az postgres flexible-server firewall-rule create \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores" \
  --rule-name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Allow your local IP for initial setup (replace with your IP)
az postgres flexible-server firewall-rule create \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores" \
  --rule-name "AllowMyIP" \
  --start-ip-address "YOUR_IP_ADDRESS" \
  --end-ip-address "YOUR_IP_ADDRESS"
```

### 2.3 Initialize Database Schema

```bash
# Connect and run migrations locally first
cd game.api
dotnet ef database update --connection-string "Host=psql-linkedin-game-scores.postgres.database.azure.com;Database=gamescores;Username=gameadmin;Password=YourSecurePassword123!;SSL Mode=Require;"
```

---

## Step 3: Deploy .NET API

### 3.1 Build and Push API Docker Image

```bash
# Login to ACR
az acr login --name "acrlinkedingamescores"

# Build and push API image
cd game.api
docker build -t acrlinkedingamescores.azurecr.io/gameapi:latest .
docker push acrlinkedingamescores.azurecr.io/gameapi:latest
```

### 3.2 Create App Service Plan

```bash
# Create App Service Plan
az appservice plan create \
  --name "asp-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --sku B1 \
  --is-linux
```

### 3.3 Create Web App for API

```bash
# Create Web App
az webapp create \
  --resource-group "rg-linkedin-game-scores" \
  --plan "asp-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --deployment-container-image-name "acrlinkedingamescores.azurecr.io/gameapi:latest"

# Configure ACR credentials
az webapp config container set \
  --name "api-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --docker-custom-image-name "acrlinkedingamescores.azurecr.io/gameapi:latest" \
  --docker-registry-server-url "https://acrlinkedingamescores.azurecr.io" \
  --docker-registry-server-user "acrlinkedingamescores" \
  --docker-registry-server-password "$(az acr credential show --name acrlinkedingamescores --query passwords[0].value --output tsv)"
```

### 3.4 Configure App Settings

```bash
# Get storage connection string
STORAGE_CONNECTION=$(az storage account show-connection-string --name "salinkedingamescores" --resource-group "rg-linkedin-game-scores" --query connectionString --output tsv)

# Configure app settings
az webapp config appsettings set \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --settings \
    "ConnectionStrings__DefaultConnection=Host=psql-linkedin-game-scores.postgres.database.azure.com;Database=gamescores;Username=gameadmin;Password=YourSecurePassword123!;SSL Mode=Require;" \
    "AzureStorage__ConnectionString=$STORAGE_CONNECTION" \
    "AzureStorage__ContainerName=score-images" \
    "ASPNETCORE_ENVIRONMENT=Production"
```

---

## Step 4: Deploy Vue Frontend

### 4.1 Build Frontend for Production

```bash
cd game.client

# Update API base URL in gameService.js for production
# Replace localhost URLs with your API URL: https://api-linkedin-game-scores.azurewebsites.net/api

npm install
npm run build
```

### 4.2 Create Static Web App

```bash
# Create Static Web App
az staticwebapp create \
  --name "swa-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --source "https://github.com/yourusername/LinkedInGameScores" \
  --location "East US2" \
  --branch "main" \
  --app-location "/game.client" \
  --output-location "dist"
```

### 4.3 Configure Custom Domain (Optional)

```bash
# Add custom domain
az staticwebapp hostname set \
  --name "swa-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --hostname "yourdomain.com"
```

---

## Step 5: Configure Custom Domain & SSL

### 5.1 Configure API Custom Domain

```bash
# Add custom domain to API
az webapp config hostname add \
  --webapp-name "api-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --hostname "api.yourdomain.com"

# Enable SSL
az webapp config ssl bind \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --certificate-thumbprint "your-certificate-thumbprint" \
  --ssl-type SNI
```

### 5.2 Update CORS Settings

```bash
# Configure CORS for production
az webapp cors add \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --allowed-origins "https://yourdomain.com"
```

---

## Step 6: Final Configuration

### 6.1 Update Frontend API URLs

Update the API URLs in your Vue.js application:

```javascript
// game.client/src/services/gameService.js
const API_BASE_URL = 'https://api-linkedin-game-scores.azurewebsites.net/api';

// game.client/src/services/adminService.js  
const API_BASE_URL = 'https://api-linkedin-game-scores.azurewebsites.net/api';
```

### 6.2 Redeploy Frontend

```bash
cd game.client
npm run build

# Deploy to Static Web App (this will trigger automatically via GitHub Actions)
git add .
git commit -m "Update API URLs for production"
git push origin main
```

---

## Post-Deployment Tasks

### Monitor Application

```bash
# View API logs
az webapp log tail --name "api-linkedin-game-scores" --resource-group "rg-linkedin-game-scores"

# View Static Web App logs
az staticwebapp show --name "swa-linkedin-game-scores" --resource-group "rg-linkedin-game-scores"
```

### Setup Application Insights (Recommended)

```bash
# Create Application Insights
az monitor app-insights component create \
  --app "ai-linkedin-game-scores" \
  --location "East US" \
  --resource-group "rg-linkedin-game-scores"

# Get instrumentation key
az monitor app-insights component show \
  --app "ai-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --query instrumentationKey --output tsv
```

---

## Cost Optimization Tips

1. **Use B1 App Service Plan** for development/testing
2. **Scale down PostgreSQL** to Burstable tier for lower costs  
3. **Use Azure CDN** for better performance and reduced bandwidth costs
4. **Enable auto-scaling** only if needed
5. **Set up cost alerts** to monitor spending

---

## Security Checklist

- [ ] Enable HTTPS redirect on App Service
- [ ] Configure proper CORS origins
- [ ] Use Azure Key Vault for sensitive configuration
- [ ] Enable Azure Security Center recommendations
- [ ] Configure network security groups if using VNet
- [ ] Enable diagnostic logging
- [ ] Set up backup for PostgreSQL database

---

## Troubleshooting

See [Troubleshooting Guide](./Troubleshooting.md) for common issues and solutions.

---

## Next Steps

After deployment:

1. **Test all functionality** thoroughly in production
2. **Set up CI/CD pipelines** for automated deployments
3. **Configure monitoring and alerts**
4. **Plan backup and disaster recovery**
5. **Optimize performance** based on usage patterns

---

## Resources

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [Azure Database for PostgreSQL Documentation](https://docs.microsoft.com/en-us/azure/postgresql/)
- [Azure Blob Storage Documentation](https://docs.microsoft.com/en-us/azure/storage/blobs/)