-- Adds the granular Role/Permission system (Roles & Permissions screen):
--   1) Roles, SystemModules, Permissions, RolePermissions tables
--   2) Users.RoleId (FK -> Roles.Id) — the legacy Users.Role string column stays
--      as the source of truth for [Authorize(Roles=...)] everywhere; RoleId only
--      drives the granular permission tags returned at login (see AuthController).
--   3) Seeds 4 Roles, 9 SystemModules, 23 Permissions, and each Role's default
--      RolePermission grants (mirrors MedSafe.Infrastructure/Data/AppDbContext.cs)
--   4) Marks the matching EF migrations as applied
--
-- Idempotent — safe to run more than once, and safe to run directly against the
-- live database (every step checks before acting).

USE [db_acd20d_medsafe001]
GO

-- 1) Tables ---------------------------------------------------------------

IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [CreatedAt] [datetime2] NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
END
GO

IF OBJECT_ID('dbo.SystemModules', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SystemModules](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [DisplayOrder] [int] NOT NULL,
        CONSTRAINT [PK_SystemModules] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
END
GO

IF OBJECT_ID('dbo.Permissions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Permissions](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [PermissionTag] [nvarchar](150) NOT NULL,
        [ParentId] [int] NULL,
        [SystemModuleId] [int] NOT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Permissions_Parent] FOREIGN KEY([ParentId]) REFERENCES [dbo].[Permissions] ([Id]),
        CONSTRAINT [FK_Permissions_SystemModule] FOREIGN KEY([SystemModuleId]) REFERENCES [dbo].[SystemModules] ([Id])
    )
END
GO

IF OBJECT_ID('dbo.RolePermissions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RolePermissions](
        [RoleId] [int] NOT NULL,
        [PermissionId] [int] NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RoleId] ASC, [PermissionId] ASC),
        CONSTRAINT [FK_RolePermissions_Permission] FOREIGN KEY([PermissionId]) REFERENCES [dbo].[Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE
    )
END
GO

IF COL_LENGTH('dbo.Users', 'RoleId') IS NULL
    ALTER TABLE [dbo].[Users] ADD [RoleId] [int] NULL
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_RoleId')
    CREATE NONCLUSTERED INDEX [IX_Users_RoleId] ON [dbo].[Users] ([RoleId] ASC)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_ParentId')
    CREATE NONCLUSTERED INDEX [IX_Permissions_ParentId] ON [dbo].[Permissions] ([ParentId] ASC)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_PermissionTag')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Permissions_PermissionTag] ON [dbo].[Permissions] ([PermissionTag] ASC)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_SystemModuleId')
    CREATE NONCLUSTERED INDEX [IX_Permissions_SystemModuleId] ON [dbo].[Permissions] ([SystemModuleId] ASC)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolePermissions_PermissionId')
    CREATE NONCLUSTERED INDEX [IX_RolePermissions_PermissionId] ON [dbo].[RolePermissions] ([PermissionId] ASC)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Roles_Name] ON [dbo].[Roles] ([Name] ASC)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SystemModules_Name')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_SystemModules_Name] ON [dbo].[SystemModules] ([Name] ASC)
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Role')
    ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_Users_Role] FOREIGN KEY([RoleId]) REFERENCES [dbo].[Roles] ([Id])
GO

-- 2) Seed data --------------------------------------------------------------

SET IDENTITY_INSERT [dbo].[Roles] ON
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 1)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [CreatedAt]) VALUES (1, 'Nurse', 'Frontline clinical staff who submit incident reports', '2026-01-01')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 2)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [CreatedAt]) VALUES (2, 'Physician', 'Clinical reviewer who signs off on incident reports', '2026-01-01')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 3)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [CreatedAt]) VALUES (3, 'Admin', 'Full administrative access', '2026-01-01')
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 4)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [CreatedAt]) VALUES (4, 'Pharmacist', 'Pharmacy staff', '2026-01-01')
SET IDENTITY_INSERT [dbo].[Roles] OFF
GO

SET IDENTITY_INSERT [dbo].[SystemModules] ON
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 1) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (1, 'Incident Reports', 'Submitting and viewing incident reports', 1)
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 2) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (2, 'Clinical Review', 'Reviewing and signing off on reports', 2)
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 3) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (3, 'Alert Rules', 'Configuring notification/alert rules', 3)
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 4) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (4, 'Configurations', 'Managing dropdown/lookup configuration data', 4)
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 5) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (5, 'User Management', 'Managing user accounts and roles', 5)
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 6) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (6, 'Feedback', 'Submitting and reviewing app feedback', 6)
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 7) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (7, 'Audit Log', 'Viewing the HIPAA audit trail', 7)
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 8) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (8, 'Dashboard', 'Viewing analytics dashboard', 8)
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModules] WHERE [Id] = 9) INSERT INTO [dbo].[SystemModules] ([Id],[Name],[Description],[DisplayOrder]) VALUES (9, 'Training & Support', 'Viewing training/reference and support resources', 9)
SET IDENTITY_INSERT [dbo].[SystemModules] OFF
GO

SET IDENTITY_INSERT [dbo].[Permissions] ON
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 1)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (1, 'Incident Reports', NULL, 'incident_reports', 1)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 2)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (2, 'Submit Report', 1, 'incident_reports.submit', 1)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 3)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (3, 'View All Reports', 1, 'incident_reports.view_all', 1)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 4)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (4, 'Export Reports', 1, 'incident_reports.export', 1)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 5)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (5, 'Clinical Review', NULL, 'clinical_review', 2)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 6)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (6, 'Start Review', 5, 'clinical_review.start', 2)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 7)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (7, 'Sign Off Review', 5, 'clinical_review.sign_off', 2)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 8)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (8, 'Alert Rules', NULL, 'alert_rules', 3)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 9)  INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (9, 'View Alert Rules', 8, 'alert_rules.view', 3)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 10) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (10, 'Manage Alert Rules', 8, 'alert_rules.manage', 3)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 11) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (11, 'Configurations', NULL, 'configurations', 4)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 12) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (12, 'Manage Configurations', 11, 'configurations.manage', 4)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 13) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (13, 'User Management', NULL, 'user_management', 5)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 14) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (14, 'Manage Users', 13, 'user_management.manage', 5)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 15) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (15, 'Feedback', NULL, 'feedback', 6)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 16) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (16, 'Submit Feedback', 15, 'feedback.submit', 6)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 17) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (17, 'Review Feedback', 15, 'feedback.review', 6)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 18) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (18, 'Audit Log', NULL, 'audit_log', 7)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 19) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (19, 'View Audit Log', 18, 'audit_log.view', 7)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 20) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (20, 'Dashboard', NULL, 'dashboard', 8)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 21) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (21, 'View Dashboard', 20, 'dashboard.view', 8)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 22) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (22, 'Training & Support', NULL, 'training', 9)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Id] = 23) INSERT INTO [dbo].[Permissions] ([Id],[Name],[ParentId],[PermissionTag],[SystemModuleId]) VALUES (23, 'View Training & Support', 22, 'training.view', 9)
SET IDENTITY_INSERT [dbo].[Permissions] OFF
GO

-- Default RolePermission grants — Admin = every permission (1-23). Physician =
-- clinical review + report basics. Nurse = submit report + feedback + dashboard.
-- Pharmacist = baseline only. Training & Support (22/23) goes to every role.
;WITH DefaultGrants AS (
    SELECT * FROM (VALUES
        -- Admin: everything
        (3,1),(3,2),(3,3),(3,4),(3,5),(3,6),(3,7),(3,8),(3,9),(3,10),(3,11),(3,12),
        (3,13),(3,14),(3,15),(3,16),(3,17),(3,18),(3,19),(3,20),(3,21),(3,22),(3,23),
        -- Physician
        (2,1),(2,3),(2,5),(2,6),(2,7),(2,20),(2,21),(2,22),(2,23),
        -- Nurse
        (1,1),(1,2),(1,15),(1,16),(1,20),(1,21),(1,22),(1,23),
        -- Pharmacist
        (4,1),(4,2),(4,20),(4,21),(4,22),(4,23)
    ) AS g(RoleId, PermissionId)
)
INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId])
SELECT g.RoleId, g.PermissionId
FROM DefaultGrants g
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[RolePermissions] rp
    WHERE rp.RoleId = g.RoleId AND rp.PermissionId = g.PermissionId
)
GO

-- 3) EF migration history (so `dotnet ef database update` recognizes these as applied) --

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260813201941_AddRolePermissionSystem')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES ('20260813201941_AddRolePermissionSystem', '9.0.0')
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260813215056_AddTrainingSupportPermission')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES ('20260813215056_AddTrainingSupportPermission', '9.0.0')
GO
