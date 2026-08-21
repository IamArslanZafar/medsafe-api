-- Adds a "View Notifications" permission — the notification bell previously had
-- no permission gate at all, so it showed for every logged-in user regardless of
-- what was granted in Roles & Permissions. Also renames the "Incident Reports"
-- permission's label to "View Reports Hub" so an Admin assigning permissions can
-- tell at a glance that it's what actually controls the Reports Hub sidebar link,
-- rather than reading like a generic module-group toggle.
--
-- Idempotent — safe to run more than once. No embedded USE: run with
-- `sqlcmd -d <database>` against each target (local dev DB and live DB).

-- 1) Rename Permission Id=1's label only — PermissionTag stays 'incident_reports'
--    so RolePermission grants and the frontend's PERMISSION_TAGS mapping are untouched.
UPDATE dbo.Permissions SET [Name] = 'View Reports Hub' WHERE [Name] = 'Incident Reports' AND [PermissionTag] = 'incident_reports';
GO

-- 2) "Notifications" module + "View Notifications" permission.
IF NOT EXISTS (SELECT 1 FROM dbo.SystemModules WHERE [Name] = 'Notifications')
    INSERT INTO dbo.SystemModules ([Name], [Description], [DisplayOrder]) VALUES ('Notifications', 'Viewing the in-app notification bell', 10);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE [PermissionTag] = 'notifications.view')
    INSERT INTO dbo.Permissions ([Name], [ParentId], [PermissionTag], [SystemModuleId])
    SELECT 'View Notifications', NULL, 'notifications.view', m.Id
    FROM dbo.SystemModules m WHERE m.[Name] = 'Notifications';
GO

-- 3) Grant it to every existing role (built-in and custom) so nobody loses the
--    bell they already see today — an Admin can restrict it per-role afterwards.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE p.[PermissionTag] = 'notifications.view'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);
GO
