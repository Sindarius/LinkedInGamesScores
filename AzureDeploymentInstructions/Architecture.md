# Azure Architecture Overview

## High-Level Architecture

```mermaid
graph TB
    subgraph "User Layer"
        U1[Desktop Users]
        U2[Mobile Users]
        U3[Admin Users]
    end

    subgraph "Azure Cloud"
        subgraph "Frontend Layer"
            SWA[Azure Static Web App<br/>Vue.js SPA]
            CDN[Azure CDN<br/>Global Distribution]
        end

        subgraph "API Layer"
            AS[Azure App Service<br/>.NET 8 WebAPI]
            ACR[Azure Container Registry<br/>Docker Images]
        end

        subgraph "Data Layer"
            PG[Azure Database for PostgreSQL<br/>Flexible Server]
            BS[Azure Blob Storage<br/>Score Images]
        end

        subgraph "Monitoring & Security"
            AI[Application Insights<br/>Monitoring]
            KV[Azure Key Vault<br/>Secrets Management]
        end
    end

    subgraph "External Services"
        GH[GitHub<br/>Source Control]
        LI[LinkedIn APIs<br/>Profile Integration]
    end

    U1 --> CDN
    U2 --> CDN
    U3 --> CDN
    CDN --> SWA
    SWA --> AS
    AS --> PG
    AS --> BS
    AS --> AI
    AS --> KV
    ACR --> AS
    GH --> SWA
    AS --> LI

    classDef frontend fill:#e1f5fe
    classDef backend fill:#f3e5f5
    classDef data fill:#e8f5e8
    classDef monitor fill:#fff3e0
    classDef external fill:#fce4ec

    class SWA,CDN frontend
    class AS,ACR backend
    class PG,BS data
    class AI,KV monitor
    class GH,LI external
```

## Detailed Component Architecture

### Frontend Layer

```mermaid
graph LR
    subgraph "Azure Static Web App"
        subgraph "Vue.js Application"
            R[Vue Router<br/>SPA Navigation]
            C[Vue Components<br/>- GameTabs<br/>- Leaderboard<br/>- ScoreForm<br/>- AdminPanel]
            S[Services<br/>- GameService<br/>- AdminService]
            ST[Stores<br/>- PlayerStore]
        end
        
        subgraph "Build Assets"
            HTML[index.html]
            JS[JavaScript Bundles]
            CSS[CSS/SCSS Styles]
            IMG[Static Images]
        end
    end

    R --> C
    C --> S
    C --> ST
    S --> HTML
    HTML --> JS
    HTML --> CSS
    HTML --> IMG

    classDef vue fill:#4fc08d,color:#fff
    classDef assets fill:#42b883,color:#fff
    
    class R,C,S,ST vue
    class HTML,JS,CSS,IMG assets
```

### Backend Layer

```mermaid
graph TB
    subgraph "Azure App Service"
        subgraph ".NET 8 WebAPI"
            subgraph "Controllers"
                GC[GamesController<br/>Game Management]
                GSC[GameScoresController<br/>Score CRUD + Images]
                TC[TestController<br/>Health Checks]
            end
            
            subgraph "Models"
                GM[Game Model<br/>- Id, Name, ScoringType]
                GSM[GameScore Model<br/>- Score, Time, Images]
                DTO[DTOs<br/>- GameScoreDto<br/>- GameScoreWithImageDto]
            end
            
            subgraph "Data Layer"
                GCX[GameContext<br/>EF Core DbContext]
                MIG[Migrations<br/>Database Schema]
            end
            
            subgraph "Services"
                IS[Image Service<br/>Thumbnail Generation]
                BS[Blob Service<br/>Azure Storage]
            end
        end
    end

    GC --> GM
    GSC --> GSM
    GSC --> DTO
    GC --> GCX
    GSC --> GCX
    GCX --> MIG
    GSC --> IS
    IS --> BS

    classDef controller fill:#512bd4,color:#fff
    classDef model fill:#ff6b6b,color:#fff
    classDef data fill:#4ecdc4,color:#fff
    classDef service fill:#45b7d1,color:#fff

    class GC,GSC,TC controller
    class GM,GSM,DTO model
    class GCX,MIG data
    class IS,BS service
```

### Data Flow Architecture

```mermaid
sequenceDiagram
    participant U as User
    participant F as Frontend (SWA)
    participant A as API (App Service)
    participant D as Database (PostgreSQL)
    participant B as Blob Storage
    participant I as Image Service

    Note over U,I: Score Submission with Image
    
    U->>F: Submit score with image
    F->>A: POST /api/gamescores/with-image
    A->>I: Process & resize image
    I->>B: Store original image
    I->>B: Store thumbnail
    A->>D: Save score record
    D-->>A: Return saved score
    A-->>F: Return success response
    F-->>U: Show success message

    Note over U,I: View Leaderboard with Images
    
    U->>F: View leaderboard
    F->>A: GET /api/gamescores/game/{id}/leaderboard
    A->>D: Query scores
    D-->>A: Return score data
    A-->>F: Return leaderboard
    F->>U: Display leaderboard
    
    U->>F: Hover over image icon
    F->>A: GET /api/gamescores/{id}/image/thumbnail
    A->>B: Retrieve thumbnail
    B-->>A: Return image blob
    A-->>F: Return thumbnail
    F-->>U: Show thumbnail preview

    U->>F: Click image icon
    F->>A: GET /api/gamescores/{id}/image
    A->>B: Retrieve full image
    B-->>A: Return image blob
    A-->>F: Return full image
    F-->>U: Show full image in dialog
```

## Network Architecture

```mermaid
graph TB
    subgraph "Internet"
        INT[Internet Traffic]
    end

    subgraph "Azure Front Door / CDN"
        AFD[Azure Front Door<br/>Global Load Balancer]
        CDN[Azure CDN<br/>Edge Caching]
    end

    subgraph "Azure Region: East US"
        subgraph "Virtual Network (Optional)"
            subgraph "Frontend Subnet"
                SWA[Static Web App<br/>Frontend]
            end
            
            subgraph "API Subnet"
                AS[App Service<br/>API Backend]
            end
            
            subgraph "Data Subnet"
                PG[(PostgreSQL<br/>Database)]
                SA[Storage Account<br/>Blob Storage]
            end
        end
        
        subgraph "Security & Monitoring"
            NSG[Network Security Groups]
            FW[Azure Firewall]
            AI[Application Insights]
        end
    end

    INT --> AFD
    AFD --> CDN
    CDN --> SWA
    SWA --> AS
    AS --> PG
    AS --> SA
    NSG --> AS
    NSG --> PG
    FW --> NSG
    AS --> AI
    SWA --> AI

    classDef internet fill:#ff9999
    classDef cdn fill:#99ccff
    classDef frontend fill:#99ff99
    classDef backend fill:#ffcc99
    classDef data fill:#cc99ff
    classDef security fill:#ffff99

    class INT internet
    class AFD,CDN cdn
    class SWA frontend
    class AS backend
    class PG,SA data
    class NSG,FW,AI security
```

## Security Architecture

```mermaid
graph TB
    subgraph "Security Layers"
        subgraph "Identity & Access"
            AAD[Azure Active Directory<br/>Identity Provider]
            RBAC[Role-Based Access Control<br/>Permissions]
        end
        
        subgraph "Network Security"
            NSG[Network Security Groups<br/>Firewall Rules]
            PE[Private Endpoints<br/>Secure Connections]
            SSL[SSL/TLS Certificates<br/>End-to-End Encryption]
        end
        
        subgraph "Application Security"
            CORS[CORS Policies<br/>Cross-Origin Requests]
            VAL[Input Validation<br/>Data Sanitization]
            CSRF[CSRF Protection<br/>Token Validation]
        end
        
        subgraph "Data Security"
            ENC[Data Encryption<br/>At Rest & In Transit]
            KV[Azure Key Vault<br/>Secrets Management]
            BU[Backup & Recovery<br/>Data Protection]
        end
        
        subgraph "Monitoring & Compliance"
            SC[Security Center<br/>Threat Detection]
            LOG[Audit Logging<br/>Activity Monitoring]
            COMP[Compliance<br/>GDPR, SOC 2]
        end
    end

    classDef identity fill:#4285f4,color:#fff
    classDef network fill:#ea4335,color:#fff
    classDef application fill:#34a853,color:#fff
    classDef data fill:#fbbc04,color:#000
    classDef monitoring fill:#9aa0a6,color:#fff

    class AAD,RBAC identity
    class NSG,PE,SSL network
    class CORS,VAL,CSRF application
    class ENC,KV,BU data
    class SC,LOG,COMP monitoring
```

## Scalability & Performance

### Auto-Scaling Configuration

```mermaid
graph LR
    subgraph "Auto-Scaling Rules"
        subgraph "Frontend Scaling"
            CDN[CDN Edge Locations<br/>Global Distribution]
            SWA[Static Web App<br/>Auto-Scale Built-in]
        end
        
        subgraph "API Scaling"
            ASP[App Service Plan<br/>B1 → S1 → P1V2]
            AI[Auto-Scale Rules<br/>CPU > 70%<br/>Memory > 80%<br/>Queue Length > 100]
        end
        
        subgraph "Database Scaling"
            PG[PostgreSQL Flexible<br/>Burstable → General Purpose]
            RR[Read Replicas<br/>Load Distribution]
        end
        
        subgraph "Storage Scaling"
            BS[Blob Storage<br/>Auto-Scale Built-in]
            CDN2[CDN for Images<br/>Global Caching]
        end
    end

    CDN --> SWA
    ASP --> AI
    PG --> RR
    BS --> CDN2

    classDef frontend fill:#e3f2fd
    classDef api fill:#f3e5f5
    classDef database fill:#e8f5e8
    classDef storage fill:#fff3e0

    class CDN,SWA frontend
    class ASP,AI api
    class PG,RR database
    class BS,CDN2 storage
```

## Cost Optimization Strategy

| Service | Development | Production | Optimization Notes |
|---------|-------------|------------|-------------------|
| App Service Plan | B1 Basic | S1 Standard | Auto-scale based on demand |
| PostgreSQL | Burstable B1ms | General Purpose GP_Standard_D2s_v3 | Scale up during peak hours |
| Storage Account | LRS | GRS | Use lifecycle policies |
| Static Web App | Free | Standard | Pay per usage |
| Application Insights | Basic | Standard | Set retention policies |

## Monitoring & Alerting

```mermaid
graph TB
    subgraph "Monitoring Stack"
        subgraph "Application Monitoring"
            AI[Application Insights<br/>- Performance Metrics<br/>- Error Tracking<br/>- User Analytics]
            LG[Log Analytics<br/>- Centralized Logging<br/>- Custom Queries]
        end
        
        subgraph "Infrastructure Monitoring"
            AM[Azure Monitor<br/>- Resource Metrics<br/>- Platform Logs]
            MG[Metric Alerts<br/>- CPU, Memory, Disk<br/>- Response Time]
        end
        
        subgraph "Alerting & Notifications"
            AG[Action Groups<br/>- Email, SMS, Webhook]
            LA[Log Alerts<br/>- Error Patterns<br/>- Anomaly Detection]
        end
        
        subgraph "Dashboards"
            AZ[Azure Dashboards<br/>- Executive View]
            WB[Workbooks<br/>- Detailed Analysis]
        end
    end

    AI --> LG
    AM --> MG
    MG --> AG
    LG --> LA
    LA --> AG
    AI --> AZ
    AM --> AZ
    LG --> WB

    classDef monitoring fill:#0078d4,color:#fff
    classDef alerting fill:#d83b01,color:#fff
    classDef dashboard fill:#107c10,color:#fff

    class AI,LG,AM,MG monitoring
    class AG,LA alerting
    class AZ,WB dashboard
```

## Disaster Recovery Plan

```mermaid
graph TB
    subgraph "Primary Region: East US"
        P_SWA[Static Web App]
        P_AS[App Service]
        P_PG[PostgreSQL Primary]
        P_BS[Blob Storage Primary]
    end
    
    subgraph "Secondary Region: West US 2"
        S_SWA[Static Web App Backup]
        S_AS[App Service Backup]
        S_PG[PostgreSQL Replica]
        S_BS[Blob Storage Backup]
    end
    
    subgraph "Backup Strategy"
        DB_BU[Daily DB Backups<br/>30-day retention]
        BLOB_BU[Blob Replication<br/>GRS Storage]
        CODE_BU[GitHub Repository<br/>Source Control]
    end

    P_PG -.->|Replication| S_PG
    P_BS -.->|GRS Replication| S_BS
    P_SWA -.->|GitHub Actions| S_SWA
    
    P_PG --> DB_BU
    P_BS --> BLOB_BU
    P_AS --> CODE_BU

    classDef primary fill:#107c10,color:#fff
    classDef secondary fill:#d83b01,color:#fff
    classDef backup fill:#0078d4,color:#fff

    class P_SWA,P_AS,P_PG,P_BS primary
    class S_SWA,S_AS,S_PG,S_BS secondary  
    class DB_BU,BLOB_BU,CODE_BU backup
```

---

## Next Steps

1. **Review the [Main Deployment Guide](./README.md)** for step-by-step instructions
2. **Check the [Troubleshooting Guide](./Troubleshooting.md)** for common issues
3. **Follow the [CI/CD Setup Guide](./CICD.md)** for automated deployments