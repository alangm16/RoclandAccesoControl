# Rocland AccesoControl

Access control system for visitors, vendors, and clients.  
Digitizes entry and exit registration with real-time notifications.

## Technology Stack

- **Backend:** ASP.NET Core 10, Entity Framework Core, SignalR
- **Frontend:** Razor Pages, Chart.js, Bootstrap
- **Mobile App:** .NET MAUI 10 (Android / Windows)
- **Database:** SQL Server 2019/2022
- **Infrastructure:** Windows Server + IIS

## Solution Structure

RoclandAccesoControl/  
├── RoclandAccesoControl.Web/      # REST API + Razor Pages + SignalR  
├── RoclandAccesoControl.Mobile/   # Guard App (MAUI)  
└── RoclandAccesoControl.Tests/    # Unit tests (xUnit)

## Initial Setup (Development)

### 1. Prerequisites

- Visual Studio 2022 17.x with workloads: ASP.NET, .NET MAUI
- .NET SDK 10.0
- SQL Server 2019/2022 + SSMS
- Git

### 2. Database

Run the script in SSMS:  
`RoclandAccesoControl.Web/Scripts/RoclandAccesoControl_v1.0.sql`

### 3. Secrets Configuration

Create `appsettings.Secrets.json` in `RoclandAccesoControl.Web/`  
(not committed to Git — it’s in `.gitignore`):

```json
{
  "ConnectionStrings": {
    "Default": "Server=DEV-SQL-01;Database=RoclandAccesoControl;User Id=rc_admin;Password=Zx9!Qw7@Lm2#;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "A9fK3LmP8QxR7W2ZC0VnH5T4yEJdBMuS",
    "Issuer": "Rocland.Internal.Api",
    "Audience": "Rocland.Access.Clients",
    "ExpirationHours": 12
  },
  "AppSettings": {
    "AutoCerrarSalidaHoras": 36
  }
}
4. Run
# Web (with Swagger at /swagger)
dotnet run --project RoclandAccesoControl.Web

# Tests
dotnet test RoclandAccesoControl.Tests
5. First Admin Panel Access
Generate your password hash:
GET https://localhost:7000/api/auth/dev/hash?pwd=MyP@ssw0rd!2026
Insert the admin user in SSMS using that hash
Access: https://localhost:7000/Admin/Login
Production Deployment (IIS)

See DEPLOY.md
 for detailed instructions.

Run Tests
dotnet test RoclandAccesoControl.Tests --logger "console;verbosity=detailed"
Git Workflow
# New feature
git checkout -b feature/access-logs-export
git commit -m "feat(access): export visit logs to CSV"
git push origin feature/access-logs-export
# → Pull Request → merge into main
Versioning
v1.0.0 — Sprint 6, initial production release
