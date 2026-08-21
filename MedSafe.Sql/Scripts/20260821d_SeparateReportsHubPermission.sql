-- Splits "View Reports Hub" out into its own real, independently-checkable
-- permission instead of reusing the "Incident Reports" group-parent row.
--
-- Why: in the Assign Role Permissions modal, a parent row's checked state is
-- entirely DERIVED from whether every one of its children is checked (see
-- AssignPermissionsModal.jsx's toggleRow re-sync loop) — it can't be checked
-- on its own. Since "Incident Reports" (Id=1) was also the tag driving
-- Reports Hub sidebar visibility, an Admin checking only "Submit Report"
-- (without also checking View All Reports/Export/Submit Medication
-- Error/Submit ADR) left the parent — and therefore Reports Hub visibility —
-- unchecked, even though it looked like it should show. Reports Hub now has
-- its own standalone permission that isn't tied to any sibling's state.
--
-- Idempotent — safe to run more than once. No embedded USE: run with
-- `sqlcmd -d <database>` against each target (local dev DB and live DB).

-- 1) Revert Id=1's label back to a plain group header now that it's no longer
--    doing double duty as the Reports Hub visibility gate.
UPDATE dbo.Permissions SET [Name] = 'Incident Reports' WHERE [PermissionTag] = 'incident_reports' AND [Name] = 'View Reports Hub';
GO

-- 2) New standalone "View Reports Hub" permission, same module, no parent —
--    a plain sibling row, not nested under "Incident Reports".
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE [PermissionTag] = 'incident_reports.view_hub')
    INSERT INTO dbo.Permissions ([Name], [ParentId], [PermissionTag], [SystemModuleId])
    SELECT 'View Reports Hub', NULL, 'incident_reports.view_hub', m.SystemModuleId
    FROM dbo.Permissions m WHERE m.[PermissionTag] = 'incident_reports';
GO

-- 3) Grant it to every role that currently has the old "incident_reports" tag,
--    so nobody who can see Reports Hub today loses it once the frontend
--    switches to checking the new tag instead.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT rp.RoleId, newPerm.Id
FROM dbo.RolePermissions rp
JOIN dbo.Permissions oldPerm ON oldPerm.Id = rp.PermissionId AND oldPerm.[PermissionTag] = 'incident_reports'
JOIN dbo.Permissions newPerm ON newPerm.[PermissionTag] = 'incident_reports.view_hub'
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermissions existing
    WHERE existing.RoleId = rp.RoleId AND existing.PermissionId = newPerm.Id
);
GO
