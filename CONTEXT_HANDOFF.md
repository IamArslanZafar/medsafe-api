# MedSafe — Context Handoff

Is file ko naye Claude Code session (jo `D:\medsafe-api\medsafe-api` se chal raha hai) ko paste/reference karwa den taake wo bina confusion ke aage kaam continue kar sake. Purani chat ki poori history transfer nahi hoti (Claude Code session per-folder store hota hai), isliye ye summary uska replacement hai.

## Project
MedSafe — medication-error/ADR (Adverse Drug Reaction) reporting system.
- Backend: ASP.NET Core (.NET 9) Web API, EF Core 9, SQL Server (SmarterASP.NET shared hosting).
- Frontend: React (Vite, Ant Design) — separate repo/folder at `D:\MedSafeFrontend` (not yet pushed to GitHub, not part of `medsafe-api` repo).
- Live API: `https://medsafe-001-site1.etempurl.com/`
- Deploy: `.\deploy.ps1` (needs `$env:WEBDEPLOY_PASSWORD` set — no hardcoded fallback anymore, removed for security).

## Two local backend copies (important — avoid divergence)
- `D:\MedSafe` — original working copy, has live `appsettings.json` with real secrets (gitignored), all deploys were done from here.
- `D:\medsafe-api\medsafe-api` — user's fresh GitHub Desktop clone of the same repo (`github.com/IamArslanZafar/medsafe-api`), now the folder being worked in going forward. Missing `appsettings.json` (gitignored) — copy it from `D:\MedSafe\MedSafe.Api\appsettings.json` or recreate from `appsettings.Example.json` + real secrets before running/deploying from here.
- Both are on commit `8deef45` as of this writing. Keep them in sync via git push/pull — don't edit both independently without pushing/pulling between them.

## Recent major backend work (all done, deployed, and DB-migrated on both local SQLEXPRESS and live production)
1. **Incident Reports redesign** — new normalized schema, replacing the old broken `ReportsController`/`ReportCreateDto`:
   - `POST /api/incident-reports` — submit a report. Backend generates `IncidentReportNumber` (`IR-yyyyMMdd-XXXXXXXX`) and `PatientReferenceToken` (`PT-<guid>`); `SubmittedByUserId`/`SubmittedByRole` come from the JWT, not the request body.
   - `POST /api/incident-reports/{id}/attachments` — separate endpoint, multipart file upload, 10MB limit, stored outside `wwwroot` under `App_Data/IncidentAttachments`, SHA256-hashed.
   - `GET /api/incident-reports`, `GET /api/incident-reports/{id}` (Nurse role forbidden from viewing), `GET /api/incident-reports/{id}/attachments/{attachmentId}/download`.
   - Multi-select fields (contributing factors, seriousness criteria, allergies, current medications) are **junction tables**, not string columns: `IncidentReportContributingFactor`, `IncidentReportSeriousnessCriterion`, `IncidentReportAllergy`, `IncidentReportCurrentMedication`. Request body uses **ID arrays**: `contributingFactorIds`, `seriousnessCriterionIds`, `knownAllergyIds`, `currentMedicationIds` — all `List<int>`.
   - The old `KnownPatientAllergies`/`CurrentPatientMedications` free-text NVARCHAR columns on `IncidentReports` were dropped entirely (migration `DropLegacyAllergyMedicationColumns`) — that data now only lives in the junction tables.
2. **New Configuration CRUD APIs**: `NotificationRecipientType`, `NotificationMethod`, `NotificationStatus`, `NotificationUrgency` (Code+Name shape), `Allergy`, `CurrentMedication` (Name-only shape, e.g. "Amoxicillin 500mg" as a distinct row from "Amoxicillin 250mg").
3. **Notifications**: harm-level escalation (`AlertService`) now creates real `IncidentNotification` rows (not just logs) when harm level is E–I.
4. **`ProfessionId`** added to `Users`/`RegisterDto`/`AuthController` — **purely informational, NOT used for authorization**. Auth/permissions remain 100% Role-based (`Admin`/`Physician`/`Nurse` via JWT `Role` claim) — user explicitly said to keep it that way, do not change this.
5. Cleanup: deleted legacy `ConfigurationsController`, `IReportRepository`/`ReportRepository`/`Class1.cs` (dead code), reorganized DTOs/Models into `Identity/Reports/Configurations` subfolders.
6. Fixed a production 800MB RAM cap issue via workstation GC (`ServerGarbageCollection=false` in `.csproj`).

## Currently open / in-progress task (this is the "2nd kaam" to continue)
**Frontend is NOT yet wired to the new Incident Reports contract.** Found while inspecting `D:\MedSafeFrontend\src\components\form\ReportingWizard.jsx`:

- Line ~161: submit payload sends `knownPatientAllergies: knownAllergies` and `currentPatientMedications: currentMedications` — these are **arrays of allergy/medication NAME strings**, and don't match any field in the backend's `SubmitIncidentReportRequest` DTO (which expects `knownAllergyIds`/`currentMedicationIds` as `List<int>`). ASP.NET Core model binding silently ignores unknown JSON properties, so these links currently never get saved — confirmed by querying the live DB (`IncidentReportAllergy`/`IncidentReportCurrentMedication` are empty for existing test submissions).
- Line ~463 and ~484: the Allergy/Medication `<Select>` dropdown `<Option>` elements use `value={a.name}` / `value={m.name}` — so even the selected value stored in state is a name, not an ID. These need to become `value={a.id}` (keeping `a.name` as the visible label).

**Fix needed** (not yet done):
1. Change dropdown `Option value` from `a.name`/`m.name` to `a.id`/`m.id` (lines ~463, ~484 in `ReportingWizard.jsx`).
2. Change submit payload keys from `knownPatientAllergies`/`currentPatientMedications` to `knownAllergyIds`/`currentMedicationIds` (line ~161), now holding the selected IDs.
3. Check `isAllergyConflict` logic (line ~140) and the review-step display (`reviewTagRow('Known Allergies', knownAllergies)` etc., line ~861-862) — these currently assume `knownAllergies`/`currentMedications` hold display names; if the state itself switches to holding IDs, these need a name-lookup (`allergiesData.find(a => a.id === id)?.name`) for display purposes, or keep separate `label`/`value` state.
4. Verify against the full `SubmitIncidentReportRequest` DTO shape (in `MedSafe.Api/DTOs/Reports/IncidentReportDtos.cs` on the backend) that no other field names/types are mismatched — only allergies/medications were checked so far in this pass.

## Migration/schema convention (if any more DB changes are needed)
Every schema change this session used a guarded, idempotent raw-SQL EF Core migration, e.g.:
```sql
IF COL_LENGTH('dbo.TableName', 'ColumnName') IS NULL
    ALTER TABLE [dbo].[TableName] ADD [ColumnName] ...;
```
applied identically to local SQLEXPRESS and production (safe no-op if already applied). Follow this pattern for any further schema work. All Configuration/child tables use `ExcludeFromMigrations` in `AppDbContext` since they're managed via hand-written migrations, not EF's auto-diff.
