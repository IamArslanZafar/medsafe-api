USE [master]
GO

CREATE DATABASE [MedSafeHub]
GO

USE [MedSafeHub]
GO

-- Tables
CREATE TABLE [dbo].[Users](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](max) NOT NULL,
    [Email] [nvarchar](450) NOT NULL,
    [PasswordHash] [nvarchar](max) NOT NULL,
    [Role] [nvarchar](max) NOT NULL,
    [Unit] [nvarchar](max) NULL,
    [Title] [nvarchar](max) NULL,
    [Status] [nvarchar](max) NOT NULL,
    [FailedAttempts] [int] NOT NULL,
    [LockedUntil] [datetime2](7) NULL,
    [LastLogin] [datetime2](7) NULL,
    [CreatedAt] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[RefreshTokens](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [UserId] [int] NOT NULL,
    [Token] [nvarchar](max) NOT NULL,
    [ExpiresAt] [datetime2](7) NOT NULL,
    [IsRevoked] [bit] NOT NULL,
    [CreatedAt] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[AuditLogs](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [UserId] [int] NULL,
    [UserName] [nvarchar](max) NOT NULL,
    [Action] [nvarchar](max) NOT NULL,
    [Details] [nvarchar](max) NOT NULL,
    [IpAddress] [nvarchar](max) NULL,
    [Timestamp] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[AlertRules](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [RuleId] [nvarchar](max) NOT NULL,
    [Name] [nvarchar](max) NOT NULL,
    [TriggerCondition] [nvarchar](max) NOT NULL,
    [TargetRoles] [nvarchar](max) NOT NULL,
    [Urgency] [nvarchar](max) NOT NULL,
    [Enabled] [bit] NOT NULL,
    [Description] [nvarchar](max) NOT NULL,
    [LastTriggered] [datetime2](7) NULL,
    [DeliveryConfig] [nvarchar](max) NULL,
    [CreatedAt] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_AlertRules] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[Feedbacks](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Rating] [int] NOT NULL,
    [Category] [nvarchar](max) NOT NULL,
    [Comments] [nvarchar](max) NOT NULL,
    [SubmittedBy] [nvarchar](max) NOT NULL,
    [Status] [nvarchar](max) NOT NULL,
    [CreatedAt] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_Feedbacks] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[IncidentReports](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [ReportId] [nvarchar](max) NOT NULL,
    [SubmittedAt] [datetime2](7) NOT NULL,
    [SubmittedBy] [nvarchar](max) NOT NULL,
    [SubmitterRole] [nvarchar](max) NOT NULL,
    [FacilityUnit] [nvarchar](max) NOT NULL,
    [Status] [nvarchar](max) NOT NULL,
    [PatientRef] [nvarchar](max) NOT NULL,
    [AgeBand] [nvarchar](max) NOT NULL,
    [Sex] [nvarchar](max) NOT NULL,
    [Weight] [decimal](18, 2) NULL,
    [MedicalHistory] [nvarchar](max) NULL,
    [KnownAllergies] [nvarchar](max) NULL,
    [CurrentMedications] [nvarchar](max) NULL,
    [MedicationName] [nvarchar](max) NOT NULL,
    [GenericName] [nvarchar](max) NULL,
    [Dose] [decimal](18, 4) NOT NULL,
    [DoseUnit] [nvarchar](max) NOT NULL,
    [Route] [nvarchar](max) NOT NULL,
    [Frequency] [nvarchar](max) NULL,
    [TimingGiven] [datetime2](7) NULL,
    [Formulation] [nvarchar](max) NULL,
    [BatchNumber] [nvarchar](max) NULL,
    [ReportType] [nvarchar](max) NOT NULL,
    [ErrorCategory] [nvarchar](max) NULL,
    [StageOfProcess] [nvarchar](max) NULL,
    [ReactionDescription] [nvarchar](max) NULL,
    [SuspectedCausality] [nvarchar](max) NULL,
    [SeverityCategory] [nvarchar](max) NOT NULL,
    [SeriousnessCriteria] [nvarchar](max) NULL,
    [IncidentNarrative] [nvarchar](max) NOT NULL,
    [IncidentDate] [datetime2](7) NULL,
    [Location] [nvarchar](max) NOT NULL,
    [ContributingFactors] [nvarchar](max) NULL,
    [ImmediateAction] [nvarchar](max) NULL,
    [PatientOutcome] [nvarchar](max) NOT NULL,
    [PatientOutcomeDetails] [nvarchar](max) NULL,
    [Reviewer] [nvarchar](max) NULL,
    [ReviewerRole] [nvarchar](max) NULL,
    [ClinicalAssessment] [nvarchar](max) NULL,
    [FollowUpActions] [nvarchar](max) NULL,
    [ActionOwner] [nvarchar](max) NULL,
    [ResolutionStatus] [nvarchar](max) NOT NULL,
    CONSTRAINT [PK_IncidentReports] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

-- Indexes
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users] ([Email] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_AuditLogs_UserId] ON [dbo].[AuditLogs] ([UserId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId] ASC)
GO

-- Foreign Keys
ALTER TABLE [dbo].[RefreshTokens] ADD CONSTRAINT [FK_RefreshTokens_Users_UserId]
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users_UserId]
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([Id])
GO

-- EF Migrations History
CREATE TABLE [dbo].[__EFMigrationsHistory](
    [MigrationId] [nvarchar](150) NOT NULL,
    [ProductVersion] [nvarchar](32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED ([MigrationId] ASC)
)
GO

INSERT INTO [__EFMigrationsHistory] VALUES ('20260808144144_InitialCreate', '9.0.0')
INSERT INTO [__EFMigrationsHistory] VALUES ('20260808182332_UpdateModelSync', '9.0.0')
GO

-- Seed Admin User (Password: Admin@1234)
INSERT INTO [dbo].[Users] (Name, Email, PasswordHash, Role, Unit, Title, Status, FailedAttempts, CreatedAt)
VALUES ('Super Admin', 'admin@medsafe.com', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'Admin', NULL, NULL, 'active', 0, GETUTCDATE())
GO
