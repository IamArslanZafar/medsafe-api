# MedSafe API

Backend for MedSafe, a medication-error and Adverse Drug Reaction (ADR) reporting system for hospital units. Built with ASP.NET Core (.NET 9) and EF Core 9 on SQL Server.

Live API: `https://medsafe-001-site1.etempurl.com/`

## Tech stack

- ASP.NET Core 9 Web API
- Entity Framework Core 9 (SQL Server)
- JWT bearer authentication + BCrypt password hashing
- Hosted on SmarterASP.NET shared hosting via Web Deploy (MSDeploy)

## Project structure

```
MedSafe.Api             API host — controllers, DTOs, services, background services, migrations
MedSafe.Application     Application-layer logic
MedSafe.Core            Core abstractions
MedSafe.Identity        Identity-related code
MedSafe.Infrastructure  EF Core DbContext, repositories, data access
MedSafe.Logging         Logging infrastructure
MedSafe.Models          EF entity models
MedSafe.Shared          Shared types/utilities
MedSafe.Sql             SQL assets
MedSafe.Test            Tests
HashGen                 Utility for generating BCrypt password hashes
```

Controllers are grouped by area under `MedSafe.Api/Controllers/`:
- `Identity/` — `AuthController` (login/refresh/logout/register), `UsersController`
- `Reports/` — `IncidentReportsController`, `AlertsController`, `AuditController`, `FeedbackController`
- `Configurations/` — CRUD for lookup/reference data (Allergies, CurrentMedications, ContributingFactors, DoseUnits, Routes, Frequencies, Formulations, ErrorCategories, PatientOutcomes, Professions, SeriousnessCriteria, StageOfProcess, NotificationMethods, NotificationRecipientTypes, NotificationStatuses, NotificationUrgencies)

## Authentication & authorization

Auth is JWT-based (`POST /api/auth/login`, `/refresh`, `/logout`, `/register`). Access tokens carry a `Role` claim (`Admin` / `Physician` / `Nurse`), and **all authorization is role-based only** via `[Authorize(Roles = "...")]`. Users also have an optional `ProfessionId` (e.g. their clinical specialty), but this is informational only and must never be used for permission checks — keep authorization strictly role-based when adding new endpoints.

## Incident reports

`POST /api/incident-reports` accepts a normalized `SubmitIncidentReportRequest` (see `MedSafe.Api/DTOs/Reports/IncidentReportDtos.cs`). The backend generates `IncidentReportNumber` (`IR-yyyyMMdd-XXXXXXXX`) and `PatientReferenceToken` (`PT-<guid>`); `SubmittedByUserId`/`SubmittedByRole` come from the JWT, not the request body.

Multi-select fields — contributing factors, seriousness criteria, known allergies, current medications — are **junction tables**, not free-text columns: `IncidentReportContributingFactor`, `IncidentReportSeriousnessCriterion`, `IncidentReportAllergy`, `IncidentReportCurrentMedication`. The request body carries these as ID arrays (`contributingFactorIds`, `seriousnessCriterionIds`, `knownAllergyIds`, `currentMedicationIds`).

Attachments are uploaded separately via `POST /api/incident-reports/{id}/attachments` (multipart, 10MB limit), stored outside `wwwroot` under `App_Data/IncidentAttachments`, SHA256-hashed. Nurses cannot view incident reports submitted by other users (`GET /api/incident-reports/{id}` returns `403` for them in that case).

## Alerts & notifications

`AlertService.EvaluateIncidentAsync` runs after an incident report's DB transaction commits (a notification failure never rolls back the report). Based on `HarmLevelCode`:
- **H, I** → creates `IncidentNotification` rows for `SAFETY_OFFICER` and `ADMINISTRATOR`
- **E, F, G** → creates a row for `PHYSICIAN`
- **A–D** → no notification

Created rows have `Status = "PENDING"` — this only records that a notification is needed; actual delivery (email/SMS) is intentionally not implemented here and would need a separate background worker to pick up pending rows and send them.

Separately, `AlertMonitorService` (a `BackgroundService`) runs hourly and logs warnings for two pattern-based rules (RULE-004: same error category ≥3 times in 7 days; RULE-005: reports stuck in "Submitted" status >48 hours). It only logs — it does not create `IncidentNotification` rows or contact anyone.

## Local setup

1. Copy `MedSafe.Api/appsettings.Example.json` to `MedSafe.Api/appsettings.json` and fill in a real connection string and JWT key. `appsettings.json` is gitignored — never commit real secrets.
2. Point `ConnectionStrings:Default` at a local SQL Server/SQLEXPRESS instance.
3. Apply migrations:
   ```
   dotnet ef database update --project MedSafe.Api
   ```
4. Run the API:
   ```
   dotnet run --project MedSafe.Api
   ```

## Database migrations

Schema changes for Configuration/child tables (`Allergy`, `CurrentMedication`, `IncidentReportAllergy`, etc.) are hand-written as guarded, idempotent raw-SQL EF Core migrations rather than relying on EF's auto-diff — those entities are marked `ExcludeFromMigrations` in `AppDbContext`. The convention is:

```sql
IF COL_LENGTH('dbo.TableName', 'ColumnName') IS NULL
    ALTER TABLE [dbo].[TableName] ADD [ColumnName] ...;
```

This makes a migration safe to re-run (no-op if already applied) and safe to apply identically to both local and production databases. Follow this pattern for any further schema work in this area.

## Deployment

Deployment is via Web Deploy (MSDeploy) to SmarterASP.NET, driven by `deploy.ps1`:

```powershell
$env:WEBDEPLOY_PASSWORD = "..."
.\deploy.ps1
```

The script publishes a Release build, syncs it to the live site via `msdeploy.exe`, and does a basic reachability check against the live URL afterward. There is no hardcoded password fallback — `WEBDEPLOY_PASSWORD` must be set in the environment (or passed via `-Password`) or the script throws before doing anything.
