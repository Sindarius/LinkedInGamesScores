# Maintenance Guide

## Regular Maintenance Tasks

### Daily Tasks (Automated)

#### Application Health Monitoring
```bash
# Automated health check script (run via Azure Automation)
#!/bin/bash

# Check API health
API_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://api-linkedin-game-scores.azurewebsites.net/api/test/health)
if [ $API_STATUS -ne 200 ]; then
    echo "API health check failed: $API_STATUS"
    # Send alert
fi

# Check database connectivity
DB_STATUS=$(psql "host=psql-linkedin-game-scores.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" -c "SELECT 1" 2>/dev/null && echo "OK" || echo "FAIL")
if [ "$DB_STATUS" != "OK" ]; then
    echo "Database connectivity failed"
    # Send alert
fi

# Check storage account
STORAGE_STATUS=$(az storage account show --name "salinkedingamescores" --resource-group "rg-linkedin-game-scores" --query "statusOfPrimary" -o tsv)
if [ "$STORAGE_STATUS" != "available" ]; then
    echo "Storage account issue: $STORAGE_STATUS"
    # Send alert
fi
```

#### Log Review
```bash
# Daily log analysis script
az webapp log download \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores"

# Extract and analyze errors
grep -i "error\|exception\|fail" *.log | tail -100 > daily_errors.log

# Check for patterns
grep -c "500\|502\|503\|504" *.log
```

### Weekly Tasks

#### 1. Performance Review

**Application Insights Analysis**:
```kusto
// Weekly performance report
requests
| where timestamp > ago(7d)
| summarize 
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    RequestCount = count(),
    ErrorRate = countif(success == false) * 100.0 / count()
by bin(timestamp, 1d)
| render table
```

**Database Performance**:
```sql
-- Weekly database performance check
SELECT 
  schemaname,
  tablename,
  seq_scan,
  seq_tup_read,
  idx_scan,
  idx_tup_fetch,
  n_tup_ins,
  n_tup_upd,
  n_tup_del
FROM pg_stat_user_tables
ORDER BY seq_scan DESC;

-- Check for unused indexes
SELECT 
  indexrelname,
  idx_scan,
  idx_tup_read,
  idx_tup_fetch
FROM pg_stat_user_indexes
WHERE idx_scan = 0;
```

#### 2. Security Review

**Access Log Analysis**:
```bash
# Review access patterns
az webapp log tail \
  --name "api-linkedin-game-scores" \
  --resource-group "rg-linkedin-game-scores" | grep -E "POST|PUT|DELETE"

# Check for suspicious activity
az security alert list \
  --resource-group "rg-linkedin-game-scores"
```

**SSL Certificate Check**:
```bash
# Check SSL certificate expiration
az webapp config ssl list \
  --resource-group "rg-linkedin-game-scores" \
  --query "[].{name:name, thumbprint:thumbprint, expirationDate:expirationDate}"
```

#### 3. Backup Verification

**Database Backup Test**:
```bash
# Test backup restoration (to staging)
pg_dump "host=psql-linkedin-game-scores.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" > weekly_backup_test.sql

# Restore to staging database
psql "host=psql-linkedin-game-scores-staging.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" < weekly_backup_test.sql

# Verify data integrity
psql "host=psql-linkedin-game-scores-staging.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" -c "SELECT COUNT(*) FROM \"GameScores\";"
```

### Monthly Tasks

#### 1. Dependency Updates

**Frontend Dependencies**:
```bash
cd game.client

# Check for outdated packages
npm outdated

# Update dependencies (patch versions)
npm update

# Audit for security vulnerabilities
npm audit
npm audit fix

# Test after updates
npm run build
npm run lint
```

**Backend Dependencies**:
```bash
cd game.api

# Check for outdated packages
dotnet list package --outdated

# Update packages
dotnet add package Microsoft.EntityFrameworkCore --version latest
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version latest

# Run security audit
dotnet list package --vulnerable
```

#### 2. Performance Optimization

**Database Maintenance**:
```sql
-- Monthly database maintenance
VACUUM ANALYZE;
REINDEX DATABASE gamescores;

-- Update table statistics
ANALYZE "GameScores";
ANALYZE "Games";

-- Check for table bloat
SELECT 
  schemaname,
  tablename,
  pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

**Storage Cleanup**:
```bash
# Clean up old blob storage files
az storage blob list \
  --container-name "score-images" \
  --account-name "salinkedingamescores" \
  --query "[?properties.lastModified < '2024-01-01']" \
  --output table

# Set up lifecycle management
az storage account management-policy create \
  --account-name "salinkedingamescores" \
  --resource-group "rg-linkedin-game-scores" \
  --policy @lifecycle-policy.json
```

#### 3. Cost Analysis

**Monthly Cost Review**:
```bash
# Get cost analysis for last month
az consumption usage list \
  --start-date $(date -d "last month" +%Y-%m-01) \
  --end-date $(date -d "last month" +%Y-%m-31)

# Resource-specific costs
az consumption budget list \
  --resource-group "rg-linkedin-game-scores"

# Optimization recommendations
az advisor recommendation list \
  --category Cost \
  --resource-group "rg-linkedin-game-scores"
```

### Quarterly Tasks

#### 1. Security Audit

**Complete Security Review**:
```bash
# Run Azure Security Center assessment
az security assessment list \
  --resource-group "rg-linkedin-game-scores"

# Check compliance status
az security compliance list

# Review access policies
az webapp auth show \
  --resource-group "rg-linkedin-game-scores" \
  --name "api-linkedin-game-scores"
```

**Penetration Testing**:
```bash
# Basic security scan (use proper tools for production)
nmap -sV api-linkedin-game-scores.azurewebsites.net

# SSL/TLS configuration check
sslscan api-linkedin-game-scores.azurewebsites.net

# HTTP security headers check
curl -I https://api-linkedin-game-scores.azurewebsites.net
```

#### 2. Disaster Recovery Testing

**Full DR Test**:
```bash
# Test complete disaster recovery procedure
# 1. Create secondary environment
az group create --name "rg-linkedin-game-scores-dr" --location "West US 2"

# 2. Restore database backup
pg_dump "host=psql-linkedin-game-scores.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" > dr_test_backup.sql

# 3. Create DR PostgreSQL instance
az postgres flexible-server create \
  --resource-group "rg-linkedin-game-scores-dr" \
  --name "psql-linkedin-game-scores-dr" \
  --location "West US 2" \
  --admin-user gameadmin \
  --admin-password "YourSecurePassword123!"

# 4. Restore data
psql "host=psql-linkedin-game-scores-dr.postgres.database.azure.com port=5432 dbname=gamescores user=gameadmin sslmode=require" < dr_test_backup.sql

# 5. Deploy applications to DR environment
# 6. Test functionality
# 7. Clean up DR environment
az group delete --name "rg-linkedin-game-scores-dr" --yes --no-wait
```

#### 3. Capacity Planning

**Resource Utilization Analysis**:
```bash
# CPU and Memory trends
az monitor metrics list \
  --resource "/subscriptions/SUBSCRIPTION_ID/resourceGroups/rg-linkedin-game-scores/providers/Microsoft.Web/sites/api-linkedin-game-scores" \
  --metric "CpuPercentage,MemoryPercentage" \
  --start-time $(date -d "3 months ago" --iso-8601) \
  --end-time $(date --iso-8601)

# Database storage growth
az postgres flexible-server show \
  --resource-group "rg-linkedin-game-scores" \
  --name "psql-linkedin-game-scores" \
  --query "{storageSize:storageProfile.storageMB, usedStorage:storageProfile.storageUsageMB}"

# Blob storage growth analysis
az storage account show-usage \
  --account-name "salinkedingamescores"
```

## Automated Maintenance Scripts

### 1. Azure Automation Runbook

**Weekly Maintenance Runbook** (PowerShell):
```powershell
# Weekly-Maintenance.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName = "rg-linkedin-game-scores"
)

# Connect to Azure
Connect-AzAccount -Identity

# Check service health
$apiApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name "api-linkedin-game-scores"
if ($apiApp.State -ne "Running") {
    Write-Output "API App is not running: $($apiApp.State)"
    # Send alert
}

# Check database status
$pgServer = Get-AzPostgreSqlFlexibleServer -ResourceGroupName $ResourceGroupName -Name "psql-linkedin-game-scores"
if ($pgServer.State -ne "Ready") {
    Write-Output "PostgreSQL server is not ready: $($pgServer.State)"
    # Send alert
}

# Backup database
$backupName = "automated-backup-$(Get-Date -Format 'yyyy-MM-dd')"
# Implement backup logic

# Clean old backups (keep 30 days)
$cutoffDate = (Get-Date).AddDays(-30)
# Implement cleanup logic

Write-Output "Weekly maintenance completed successfully"
```

### 2. GitHub Actions Maintenance Workflow

**Monthly Maintenance** (`.github/workflows/monthly-maintenance.yml`):
```yaml
name: Monthly Maintenance

on:
  schedule:
    - cron: '0 2 1 * *'  # First day of every month at 2 AM
  workflow_dispatch:

jobs:
  maintenance:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Update Dependencies
      run: |
        # Frontend dependencies
        cd game.client
        npm audit fix
        npm update
        
        # Backend dependencies
        cd ../game.api
        dotnet list package --outdated
    
    - name: Security Scan
      run: |
        # Run security scans
        az security assessment list --resource-group rg-linkedin-game-scores
    
    - name: Performance Analysis
      run: |
        # Generate performance report
        az monitor app-insights query \
          --app ai-linkedin-game-scores \
          --analytics-query "requests | where timestamp > ago(30d) | summarize avg(duration), count() by bin(timestamp, 1d)"
    
    - name: Cost Analysis
      run: |
        # Generate cost report
        az consumption usage list \
          --start-date $(date -d "last month" +%Y-%m-01) \
          --end-date $(date -d "last month" +%Y-%m-31)
    
    - name: Create Maintenance Report
      run: |
        echo "# Monthly Maintenance Report - $(date)" > maintenance-report.md
        echo "## Dependencies Updated" >> maintenance-report.md
        echo "## Security Status" >> maintenance-report.md
        echo "## Performance Metrics" >> maintenance-report.md
        echo "## Cost Analysis" >> maintenance-report.md
    
    - name: Create Issue
      uses: actions/github-script@v6
      with:
        script: |
          const fs = require('fs');
          const report = fs.readFileSync('maintenance-report.md', 'utf8');
          
          github.rest.issues.create({
            owner: context.repo.owner,
            repo: context.repo.repo,
            title: `Monthly Maintenance Report - ${new Date().toISOString().slice(0, 7)}`,
            body: report,
            labels: ['maintenance', 'report']
          });
```

## Monitoring & Alerting Setup

### 1. Application Insights Alerts

**Performance Alerts**:
```bash
# High response time alert
az monitor metrics alert create \
  --name "High API Response Time" \
  --resource-group "rg-linkedin-game-scores" \
  --scopes "/subscriptions/SUBSCRIPTION_ID/resourceGroups/rg-linkedin-game-scores/providers/Microsoft.Web/sites/api-linkedin-game-scores" \
  --condition "avg requests/duration > 5000" \
  --description "API response time is above 5 seconds" \
  --evaluation-frequency 5m \
  --window-size 15m \
  --severity 2

# High error rate alert
az monitor metrics alert create \
  --name "High Error Rate" \
  --resource-group "rg-linkedin-game-scores" \
  --scopes "/subscriptions/SUBSCRIPTION_ID/resourceGroups/rg-linkedin-game-scores/providers/Microsoft.Web/sites/api-linkedin-game-scores" \
  --condition "avg requests/failed > 10" \
  --description "API error rate is above 10%" \
  --evaluation-frequency 5m \
  --window-size 15m \
  --severity 1
```

**Database Alerts**:
```bash
# High CPU alert
az monitor metrics alert create \
  --name "High Database CPU" \
  --resource-group "rg-linkedin-game-scores" \
  --scopes "/subscriptions/SUBSCRIPTION_ID/resourceGroups/rg-linkedin-game-scores/providers/Microsoft.DBforPostgreSQL/flexibleServers/psql-linkedin-game-scores" \
  --condition "avg cpu_percent > 80" \
  --description "Database CPU usage is above 80%" \
  --evaluation-frequency 5m \
  --window-size 15m \
  --severity 2

# Storage alert
az monitor metrics alert create \
  --name "High Database Storage" \
  --resource-group "rg-linkedin-game-scores" \
  --scopes "/subscriptions/SUBSCRIPTION_ID/resourceGroups/rg-linkedin-game-scores/providers/Microsoft.DBforPostgreSQL/flexibleServers/psql-linkedin-game-scores" \
  --condition "avg storage_percent > 85" \
  --description "Database storage usage is above 85%" \
  --evaluation-frequency 5m \
  --window-size 15m \
  --severity 1
```

### 2. Action Groups

**Create Action Group for Notifications**:
```bash
az monitor action-group create \
  --resource-group "rg-linkedin-game-scores" \
  --name "MaintenanceAlerts" \
  --short-name "MainAlert" \
  --email-receivers name="admin" email="admin@yourcompany.com" \
  --sms-receivers name="oncall" country-code="1" phone-number="1234567890"
```

## Documentation Maintenance

### 1. Keep Documentation Updated

**Monthly Documentation Review**:
- Update API documentation with any new endpoints
- Review and update deployment procedures
- Update troubleshooting guide with new issues encountered
- Review and update security procedures

### 2. Knowledge Base

**Maintain Internal Wiki**:
- Common issues and solutions
- Deployment procedures
- Emergency contacts
- Service dependencies
- Configuration details

## Maintenance Calendar

### Scheduled Maintenance Windows

**Monthly Scheduled Maintenance**:
- **Date**: First Sunday of each month
- **Time**: 2:00 AM - 4:00 AM EST
- **Duration**: 2 hours maximum
- **Scope**: Database maintenance, dependency updates, security patches

**Quarterly Scheduled Maintenance**:
- **Date**: First Sunday of each quarter
- **Time**: 2:00 AM - 6:00 AM EST  
- **Duration**: 4 hours maximum
- **Scope**: Major updates, DR testing, security audit

### Maintenance Notification

**Advance Notice**:
```bash
# Send maintenance notification
curl -X POST https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK \
  -H 'Content-type: application/json' \
  --data '{
    "text": "🔧 Scheduled Maintenance Notice",
    "attachments": [
      {
        "color": "warning",
        "fields": [
          {"title": "Date", "value": "Sunday, January 7, 2024", "short": true},
          {"title": "Time", "value": "2:00 AM - 4:00 AM EST", "short": true},
          {"title": "Expected Impact", "value": "Brief service interruptions possible", "short": false}
        ]
      }
    ]
  }'
```

## Emergency Procedures

### 1. Critical Issue Response

**Immediate Actions**:
1. Assess severity and impact
2. Implement immediate workaround if available
3. Notify stakeholders
4. Begin troubleshooting using [Troubleshooting Guide](./Troubleshooting.md)
5. Document issue and resolution

### 2. Communication Plan

**Stakeholder Notification**:
- **Severity 1 (Critical)**: Immediate notification via phone/SMS
- **Severity 2 (High)**: Email notification within 15 minutes
- **Severity 3 (Medium)**: Email notification within 1 hour
- **Severity 4 (Low)**: Include in next scheduled report

---

## Maintenance Checklist Templates

### Weekly Checklist
- [ ] Review application health dashboard
- [ ] Check error logs and rates
- [ ] Verify backup completion
- [ ] Review security alerts
- [ ] Check SSL certificate status
- [ ] Review performance metrics
- [ ] Update maintenance log

### Monthly Checklist
- [ ] Update dependencies (frontend & backend)
- [ ] Run security vulnerability scan
- [ ] Database maintenance (VACUUM, ANALYZE)
- [ ] Review and clean storage
- [ ] Cost analysis and optimization
- [ ] Review monitoring and alerting rules
- [ ] Update documentation
- [ ] Disaster recovery test (quarterly)

### Annual Checklist
- [ ] Complete security audit
- [ ] Review and update disaster recovery plan
- [ ] Capacity planning analysis
- [ ] Technology stack review and upgrades
- [ ] Contract and license renewals
- [ ] Performance benchmark comparison
- [ ] Update maintenance procedures