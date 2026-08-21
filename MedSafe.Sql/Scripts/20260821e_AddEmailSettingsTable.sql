-- Moves the outgoing-SMTP credentials out of appsettings.json's hardcoded
-- "Email" section and into a single-row DB table, editable from the new
-- Email Settings page instead of requiring a redeploy to rotate them.
--
-- Idempotent — safe to run more than once. No embedded USE: run with
-- `sqlcmd -d <database>` against each target (local dev DB and live DB).

-- 1) EmailSettings table — single row (Id is always 1).
IF OBJECT_ID('dbo.EmailSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailSettings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Host NVARCHAR(255) NOT NULL,
        Port INT NOT NULL DEFAULT 587,
        Username NVARCHAR(255) NOT NULL DEFAULT '',
        Password NVARCHAR(255) NOT NULL DEFAULT '',
        FromAddress NVARCHAR(255) NOT NULL,
        FromName NVARCHAR(100) NOT NULL DEFAULT 'QTCMRS',
        UseSsl BIT NOT NULL DEFAULT 1,
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId INT NULL
    );
END
GO

-- 2) Seed the one row with the values that were previously hardcoded in
--    appsettings.json, so email delivery keeps working the moment this
--    deploys — only runs while the table is still empty.
IF NOT EXISTS (SELECT 1 FROM dbo.EmailSettings)
    INSERT INTO dbo.EmailSettings (Host, Port, Username, Password, FromAddress, FromName, UseSsl, UpdatedAt)
    VALUES ('mail.etoremailhosting.com', 587, 'webguardian@etoremailhosting.com', '!QAZxsw2#EDC', 'webguardian@etoremailhosting.com', 'QTCMRS', 1, SYSUTCDATETIME());
GO

-- 3) "System Settings" module + "Manage Email Settings" permission — a
--    standalone permission (no parent) so it can't get tangled in the
--    parent/child checkbox-sync bug in AssignPermissionsModal.jsx.
IF NOT EXISTS (SELECT 1 FROM dbo.SystemModules WHERE [Name] = 'System Settings')
    INSERT INTO dbo.SystemModules ([Name], [Description], [DisplayOrder]) VALUES ('System Settings', 'Managing system-level settings such as outgoing email delivery', 11);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE [PermissionTag] = 'system_settings.manage_email')
    INSERT INTO dbo.Permissions ([Name], [ParentId], [PermissionTag], [SystemModuleId])
    SELECT 'Manage Email Settings', NULL, 'system_settings.manage_email', m.Id
    FROM dbo.SystemModules m WHERE m.[Name] = 'System Settings';
GO

-- 4) Admin-only by default — SMTP credentials, same posture as every other
--    credential-holding screen in this app.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE p.[PermissionTag] = 'system_settings.manage_email'
  AND r.[Name] = 'Admin'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);
GO
