# Troubleshooting Guide

## Common Deployment Issues

### 1. Container Registry Authentication

**Symptom**: `unauthorized: authentication required` when pushing to ACR

**Solution**:
```bash
# Re-authenticate with ACR
az acr login --name acrlinkedingamescores

# Check ACR credentials
az acr credential show --name acrlinkedingamescores

# Update App Service with correct credentials
az webapp config container set \
  --name "api-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --docker-registry-server-password "$(az acr credential show --name acrlinkedingamescores --query passwords[0].value --output tsv)"
```

### 2. Database Connection Issues

**Symptom**: `Npgsql.NpgsqlException: Connection refused` or timeout errors

**Solutions**:

#### Check Firewall Rules
```bash
# List current firewall rules
az postgres flexible-server firewall-rule list \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores"

# Add App Service IP to firewall
az webapp show --name "api-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --query possibleOutboundIpAddresses --output tsv

# Add each IP to firewall rules
az postgres flexible-server firewall-rule create \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores" \
  --rule-name "AppServiceIPs" \
  --start-ip-address "IP_ADDRESS" \
  --end-ip-address "IP_ADDRESS"
```

#### Verify Connection String
```bash
# Test connection string format
"Host=psql-linkedin-game-scores.postgres.database.azure.com;Database=gamescores;Username=gameadmin;Password=YourPassword;SSL Mode=Require;"

# Check in App Service configuration
az webapp config appsettings list \
  --name "api-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" \
  --query "[?name=='ConnectionStrings__DefaultConnection']"
```

### 3. Static Web App Deployment Issues

**Symptom**: Build fails or site doesn't update

**Solutions**:

#### Check Build Configuration
```yaml
# Verify staticwebapp.config.json in game.client/public/
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    },
    {
      "route": "/*",
      "serve": "/index.html",
      "statusCode": 200
    }
  ],
  "responseOverrides": {
    "404": {
      "serve": "/index.html",
      "statusCode": 200
    }
  }
}
```

#### Force Rebuild
```bash
# Trigger manual deployment
az staticwebapp deployment show \
  --name "swa-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores"

# Check deployment logs
az staticwebapp deployment list \
  --name "swa-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores"
```

### 4. CORS Issues

**Symptom**: `Access to fetch at 'API_URL' from origin 'FRONTEND_URL' has been blocked by CORS policy`

**Solution**:
```bash
# Add frontend domain to CORS
az webapp cors add \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --allowed-origins "https://your-frontend-domain.com"

# List current CORS settings
az webapp cors show \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores"

# For development, allow all origins (NOT for production)
az webapp cors add \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --allowed-origins "*"
```

### 5. SSL Certificate Issues

**Symptom**: `NET::ERR_CERT_AUTHORITY_INVALID` or SSL errors

**Solutions**:

#### For App Service
```bash
# Check SSL binding
az webapp config ssl list \
  --resource-group "rg-linkedin-game-scores"

# Force HTTPS redirect
az webapp update \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --https-only true
```

#### For Custom Domain
```bash
# Check domain configuration
az webapp config hostname list \
  --webapp-name "api-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores"

# Add SSL certificate
az webapp config ssl upload \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --certificate-file "path/to/certificate.pfx" \
  --certificate-password "password"
```

### 6. Image Upload Issues

**Symptom**: Images not saving or displaying

**Solutions**:

#### Check Blob Storage Configuration
```bash
# Verify storage account exists
az storage account show \
  --name "salinkedingamescores" \
  --resource-group "rg-linkedin-game-scores"

# Check container permissions
az storage container show \
  --name "score-images" \
  --account-name "salinkedingamescores"

# Test connection string
az storage account show-connection-string \
  --name "salinkedingamescores" \
  --resource-group "rg-linkedin-game-scores"
```

#### Update App Service Configuration
```bash
# Get storage connection string
STORAGE_CONNECTION=$(az storage account show-connection-string \
  --name "salinkedingamescores" \
  --resource-group "rg-linkedin-game-scores" \
  --query connectionString --output tsv)

# Update app settings
az webapp config appsettings set \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --settings "AzureStorage__ConnectionString=$STORAGE_CONNECTION"
```

## Performance Issues

### 1. Slow API Response Times

**Diagnosis**:
```bash
# Check Application Insights
az monitor app-insights query \
  --app "ai-linkedin-game-scores" \
  --analytics-query "requests | where timestamp > ago(1h) | summarize avg(duration) by bin(timestamp, 5m)"

# Check database performance
az postgres flexible-server show \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores"
```

**Solutions**:

#### Database Optimization
```sql
-- Check slow queries
SELECT query, mean_time, calls 
FROM pg_stat_statements 
ORDER BY mean_time DESC 
LIMIT 10;

-- Add indexes for common queries
CREATE INDEX IF NOT EXISTS idx_gamescores_gameid_date 
ON "GameScores" ("GameId", "DateAchieved" DESC);

CREATE INDEX IF NOT EXISTS idx_gamescores_playerName 
ON "GameScores" ("PlayerName");
```

#### App Service Scaling
```bash
# Scale up App Service Plan
az appservice plan update \
  --resource-group "rg-linkedin-game-scores" \
  --name "asp-linkedin-game-scores" \
  --sku S1

# Enable auto-scaling
az monitor autoscale create \
  --resource-group "rg-linkedin-game-scores" \
  --resource "asp-linkedin-game-scores" \
  --resource-type Microsoft.Web/serverfarms \
  --name "api-autoscale" \
  --min-count 1 \
  --max-count 3 \
  --count 1
```

### 2. Frontend Loading Issues

**Solutions**:

#### Enable Compression
```json
// Add to staticwebapp.config.json
{
  "mimeTypes": {
    ".json": "application/json",
    ".js": "application/javascript",
    ".css": "text/css"
  },
  "globalHeaders": {
    "Cache-Control": "public, max-age=31536000, immutable"
  }
}
```

#### Optimize Build
```bash
# Enable production optimizations
cd game.client
npm run build -- --mode production

# Analyze bundle size
npm install -g webpack-bundle-analyzer
npx webpack-bundle-analyzer dist/assets
```

## Monitoring & Debugging

### 1. Application Insights Queries

#### API Performance
```kusto
requests
| where timestamp > ago(24h)
| summarize 
    avg(duration), 
    percentile(duration, 95),
    count() 
by bin(timestamp, 1h)
| render timechart
```

#### Error Analysis
```kusto
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc
```

#### Database Queries
```kusto
dependencies
| where type == "SQL"
| where timestamp > ago(1h)
| summarize avg(duration), count() by name
| order by avg_duration desc
```

### 2. Log Analysis

#### API Logs
```bash
# Stream live logs
az webapp log tail \
  --name "api-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores"

# Download logs
az webapp log download \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores"
```

#### Database Logs
```bash
# Check PostgreSQL logs
az postgres flexible-server server-logs list \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores"

# Download specific log
az postgres flexible-server server-logs download \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores" \
  --file-name "postgresql-2024-01-01_000000.log"
```

### 3. Health Checks

#### API Health Check
```bash
# Test API health endpoint
curl -f https://api-linkedin-game-scores.azurewebsites.net/api/test/health

# Detailed health check
curl -s https://api-linkedin-game-scores.azurewebsites.net/api/test/health | jq .
```

#### Database Health Check
```bash
# Connect to database
psql "host=psql-linkedin-game-scores.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require"

# Check database status
SELECT 
  schemaname,
  tablename,
  n_tup_ins as inserts,
  n_tup_upd as updates,
  n_tup_del as deletes
FROM pg_stat_user_tables;
```

## Disaster Recovery

### 1. Database Backup & Restore

#### Manual Backup
```bash
# Create backup
pg_dump "host=psql-linkedin-game-scores.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" > backup.sql

# Restore from backup
psql "host=psql-linkedin-game-scores.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" < backup.sql
```

#### Automated Backup
```bash
# Enable automated backups
az postgres flexible-server update \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores" \
  --backup-retention 30

# List backup information
az postgres flexible-server backup list \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores"
```

### 2. Blob Storage Recovery

```bash
# Enable soft delete
az storage account blob-service-properties update \
  --account-name "salinkedingamescores" \
  --enable-delete-retention true \
  --delete-retention-days 30

# List deleted blobs
az storage blob list \
  --container-name "score-images" \
  --account-name "salinkedingamescores" \
  --include-deleted

# Restore deleted blob
az storage blob undelete \
  --container-name "score-images" \
  --name "deleted-image.jpg" \
  --account-name "salinkedingamescores"
```

### 3. Application Recovery

#### Rollback Deployment
```bash
# List deployment history
az webapp deployment list-publishing-profiles \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores"

# Rollback to previous deployment
az webapp deployment slot swap \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --slot staging \
  --target-slot production
```

## Security Issues

### 1. Unauthorized Access

**Check Security Configuration**:
```bash
# Review network security
az webapp config access-restriction show \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores"

# Check authentication settings
az webapp auth show \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores"
```

### 2. Suspicious Activity

**Monitor with Security Center**:
```bash
# Check security alerts
az security alert list \
  --resource-group "rg-linkedin-game-scores"

# Review security assessments
az security assessment list \
  --resource-group "rg-linkedin-game-scores"
```

## Cost Optimization Issues

### 1. Unexpected High Costs

**Analyze Resource Usage**:
```bash
# Check cost analysis
az consumption usage list \
  --start-date 2024-01-01 \
  --end-date 2024-01-31

# Review resource utilization
az monitor metrics list \
  --resource "/subscriptions/SUBSCRIPTION_ID/resourceGroups/rg-linkedin-game-scores/providers/Microsoft.Web/sites/api-linkedin-game-scores" \
  --metric "CpuPercentage"
```

**Optimization Actions**:
```bash
# Scale down during off-hours
az webapp config appsettings set \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores" \
  --settings "WEBSITE_TIME_ZONE=Eastern Standard Time"

# Configure auto-shutdown for development
az webapp stop \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores-staging"
```

## Emergency Contacts & Escalation

### 1. Critical Issues Checklist

- [ ] Check Azure Service Health for outages
- [ ] Verify all services are running
- [ ] Check Application Insights for errors
- [ ] Review recent deployments
- [ ] Check database connectivity
- [ ] Verify storage account access

### 2. Support Resources

- **Azure Support**: Create support ticket through Azure Portal
- **GitHub Issues**: Report issues at repository issues page
- **Documentation**: [Azure App Service Troubleshooting](https://docs.microsoft.com/en-us/azure/app-service/troubleshoot-http-502-http-503)
- **Community**: [Stack Overflow Azure tag](https://stackoverflow.com/questions/tagged/azure)

### 3. Escalation Process

1. **Level 1**: Check this troubleshooting guide
2. **Level 2**: Review Azure service health and logs
3. **Level 3**: Contact Azure Support with detailed information
4. **Level 4**: Implement disaster recovery procedures

---

## Prevention Best Practices

1. **Regular Health Checks**: Implement automated monitoring
2. **Backup Strategy**: Regular database and configuration backups
3. **Testing**: Test disaster recovery procedures monthly
4. **Updates**: Keep dependencies and frameworks updated
5. **Security**: Regular security assessments and updates
6. **Documentation**: Keep runbooks and procedures updated

## Quick Reference Commands

### Essential Commands
```bash
# Check all services status
az resource list --resource-group "rg-linkedin-game-scores" --output table

# View application logs
az webapp log tail --name "api-linkedin-game-scores" --resource-group "rg-linkedin-game-scores"

# Test API health
curl https://api-linkedin-game-scores.azurewebsites.net/api/test/health

# Check database connectivity
psql "host=psql-linkedin-game-scores.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" -c "\conninfo"

# Restart API service
az webapp restart --name "api-linkedin-game-scores" --resource-group "rg-linkedin-game-scores"
```