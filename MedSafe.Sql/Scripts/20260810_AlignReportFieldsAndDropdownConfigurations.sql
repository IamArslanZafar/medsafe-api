-- Brings a MedSafeHub.sql-era database up to the current schema:
--   1) IncidentReports: drop fields the wizard never collects, add PatientName
--   2) DropdownDefinitions / DropdownValues: Configurations page (dropdown value management)
--   3) Seeds the 9 dropdown definitions + default values (mirrors
--      MedSafeFrontend/src/data/dropdownDefaults.js)
--   4) Marks the matching EF migrations as applied
--
-- Idempotent — safe to run more than once, and safe to run directly against the
-- live database (every step checks before acting).

USE [db_acd20d_medsafe001]
GO

-- 1) IncidentReports -----------------------------------------------------

IF COL_LENGTH('dbo.IncidentReports', 'ContributingFactors') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [ContributingFactors]
GO
IF COL_LENGTH('dbo.IncidentReports', 'FacilityUnit') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [FacilityUnit]
GO
IF COL_LENGTH('dbo.IncidentReports', 'Location') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [Location]
GO
IF COL_LENGTH('dbo.IncidentReports', 'MedicalHistory') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [MedicalHistory]
GO
IF COL_LENGTH('dbo.IncidentReports', 'ReactionDescription') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [ReactionDescription]
GO
IF COL_LENGTH('dbo.IncidentReports', 'ReportType') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [ReportType]
GO
IF COL_LENGTH('dbo.IncidentReports', 'SuspectedCausality') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [SuspectedCausality]
GO

-- SeverityCategory (unused NCC-MERP field) becomes PatientName (new Demographics field)
IF COL_LENGTH('dbo.IncidentReports', 'SeverityCategory') IS NOT NULL
   AND COL_LENGTH('dbo.IncidentReports', 'PatientName') IS NULL
    EXEC sp_rename 'dbo.IncidentReports.SeverityCategory', 'PatientName', 'COLUMN'
GO
IF COL_LENGTH('dbo.IncidentReports', 'PatientName') IS NULL
    ALTER TABLE [dbo].[IncidentReports] ADD [PatientName] [nvarchar](max) NOT NULL DEFAULT ''
GO

-- 2) DropdownDefinitions / DropdownValues --------------------------------

IF OBJECT_ID('dbo.DropdownDefinitions', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DropdownDefinitions](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](450) NOT NULL,
        [Label] [nvarchar](max) NOT NULL,
        [Description] [nvarchar](max) NULL,
        CONSTRAINT [PK_DropdownDefinitions] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
END
GO

IF OBJECT_ID('dbo.DropdownValues', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DropdownValues](
        [Id] [nvarchar](450) NOT NULL,
        [DropdownDefinitionId] [int] NOT NULL,
        [Value] [nvarchar](450) NOT NULL,
        [Description] [nvarchar](max) NULL,
        [SortOrder] [int] NOT NULL,
        CONSTRAINT [PK_DropdownValues] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_DropdownValues_DropdownDefinitions_DropdownDefinitionId]
            FOREIGN KEY([DropdownDefinitionId]) REFERENCES [dbo].[DropdownDefinitions] ([Id]) ON DELETE CASCADE
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DropdownDefinitions_Key')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DropdownDefinitions_Key] ON [dbo].[DropdownDefinitions] ([Key] ASC)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DropdownValues_DropdownDefinitionId_Value')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DropdownValues_DropdownDefinitionId_Value] ON [dbo].[DropdownValues] ([DropdownDefinitionId] ASC, [Value] ASC)
GO

-- 3) Seed data ------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [dbo].[DropdownDefinitions])
BEGIN
    INSERT INTO [dbo].[DropdownDefinitions] ([Key], [Label], [Description]) VALUES
        ('route', 'Route', 'Administration route options shown on the Medication step of the Incident Report form.'),
        ('doseUnit', 'Dose Unit', 'Dose unit options shown on the Medication step of the Incident Report form.'),
        ('frequency', 'Frequency', 'Dosing frequency options shown on the Medication step of the Incident Report form.'),
        ('formulation', 'Formulation', 'Drug formulation options shown on the Medication step, alongside formulations specific to the selected medication.'),
        ('errorCategory', 'Error Category', 'NCC MERP error-nature categories shown on the Classification step.'),
        ('stageOfProcess', 'Stage of Process', 'Medication-use process stage where the error occurred.'),
        ('patientOutcome', 'Patient Outcome', 'Outcome classification recorded for the patient.'),
        ('seriousnessCriteria', 'Seriousness Criteria', 'Multi-select seriousness flags recorded on the Classification step.'),
        ('contributingFactors', 'Contributing Factors', 'Multi-select contributing factors recorded on the Incident & Harm step.')

    DECLARE @route INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'route')
    DECLARE @doseUnit INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'doseUnit')
    DECLARE @frequency INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'frequency')
    DECLARE @formulation INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'formulation')
    DECLARE @errorCategory INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'errorCategory')
    DECLARE @stageOfProcess INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'stageOfProcess')
    DECLARE @patientOutcome INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'patientOutcome')
    DECLARE @seriousnessCriteria INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'seriousnessCriteria')
    DECLARE @contributingFactors INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'contributingFactors')

    INSERT INTO [dbo].[DropdownValues] ([Id], [DropdownDefinitionId], [Value], [SortOrder]) VALUES
        ('route-oral', @route, 'Oral', 0),
        ('route-iv-infusion', @route, 'IV Infusion', 1),
        ('route-iv-direct', @route, 'IV Direct', 2),
        ('route-subcutaneous', @route, 'Subcutaneous', 3),
        ('route-im-injection', @route, 'IM Injection', 4),

        ('unit-mg', @doseUnit, 'mg', 0),
        ('unit-g', @doseUnit, 'g', 1),
        ('unit-ml', @doseUnit, 'mL', 2),
        ('unit-units', @doseUnit, 'units', 3),
        ('unit-mcg', @doseUnit, 'mcg', 4),
        ('unit-puffs', @doseUnit, 'puffs', 5),

        ('freq-once-only', @frequency, 'Once only', 0),
        ('freq-once-daily', @frequency, 'Once daily', 1),
        ('freq-bd', @frequency, 'BD (twice daily)', 2),
        ('freq-tds', @frequency, 'TDS (three times daily)', 3),
        ('freq-qds', @frequency, 'QDS (four times daily)', 4),
        ('freq-q4h', @frequency, 'Q4H', 5),
        ('freq-q6h', @frequency, 'Q6H', 6),
        ('freq-q8h', @frequency, 'Q8H', 7),
        ('freq-q12h', @frequency, 'Q12H', 8),
        ('freq-prn', @frequency, 'PRN (as needed)', 9),
        ('freq-weekly', @frequency, 'Weekly', 10),
        ('freq-other', @frequency, 'Other', 11),

        ('form-tablet', @formulation, 'Tablet', 0),
        ('form-capsule', @formulation, 'Capsule', 1),
        ('form-syrup', @formulation, 'Syrup', 2),
        ('form-suspension', @formulation, 'Suspension', 3),
        ('form-oral-solution', @formulation, 'Oral Solution', 4),
        ('form-powder-for-reconstitution', @formulation, 'Powder for Reconstitution', 5),
        ('form-injection', @formulation, 'Injection', 6),
        ('form-iv-infusion', @formulation, 'IV Infusion', 7),
        ('form-im-injection', @formulation, 'IM Injection', 8),
        ('form-subcutaneous-injection', @formulation, 'Subcutaneous Injection', 9),
        ('form-pen-auto-injector', @formulation, 'Pen/Auto-Injector', 10),
        ('form-cream', @formulation, 'Cream', 11),
        ('form-ointment', @formulation, 'Ointment', 12),
        ('form-gel', @formulation, 'Gel', 13),
        ('form-lotion', @formulation, 'Lotion', 14),
        ('form-patch', @formulation, 'Patch', 15),
        ('form-drops', @formulation, 'Drops', 16),
        ('form-inhaler', @formulation, 'Inhaler', 17),
        ('form-nebule', @formulation, 'Nebule', 18),
        ('form-suppository', @formulation, 'Suppository', 19),

        ('err-1', @errorCategory, 'Wrong drug', 0),
        ('err-2', @errorCategory, 'Wrong dose / strength', 1),
        ('err-3', @errorCategory, 'Wrong route', 2),
        ('err-4', @errorCategory, 'Wrong time', 3),
        ('err-5', @errorCategory, 'Wrong frequency', 4),
        ('err-6', @errorCategory, 'Wrong patient', 5),
        ('err-7', @errorCategory, 'Omission / missed dose', 6),
        ('err-8', @errorCategory, 'Extra / duplicate dose', 7),
        ('err-9', @errorCategory, 'Wrong formulation', 8),
        ('err-10', @errorCategory, 'Wrong preparation', 9),
        ('err-11', @errorCategory, 'Wrong rate (infusion)', 10),
        ('err-12', @errorCategory, 'Wrong duration', 11),
        ('err-13', @errorCategory, 'Allergy / contraindication', 12),
        ('err-14', @errorCategory, N'Drug–drug interaction', 13),
        ('err-15', @errorCategory, 'Expired / deteriorated drug', 14),
        ('err-16', @errorCategory, 'Documentation error', 15),

        ('prescribing', @stageOfProcess, 'Prescribing', 0),
        ('transcribing', @stageOfProcess, 'Transcribing', 1),
        ('dispensing', @stageOfProcess, 'Dispensing', 2),
        ('administration', @stageOfProcess, 'Administration', 3),
        ('monitoring', @stageOfProcess, 'Monitoring', 4),

        ('no_harm', @patientOutcome, 'No Harm', 0),
        ('harm', @patientOutcome, 'Harm Occurred', 1),
        ('recovered', @patientOutcome, 'Recovered', 2),
        ('ongoing', @patientOutcome, 'Ongoing / Monitoring', 3),

        ('death', @seriousnessCriteria, 'Results in death', 0),
        ('life_threatening', @seriousnessCriteria, 'Life-threatening', 1),
        ('hospitalisation', @seriousnessCriteria, 'Requires or prolongs hospitalisation', 2),
        ('disability', @seriousnessCriteria, 'Causes persistent or significant disability/incapacity', 3),
        ('congenital', @seriousnessCriteria, 'Congenital anomaly / birth defect', 4),
        ('medically_important', @seriousnessCriteria, 'Other medically important condition', 5),

        ('cf-adcs-issues', @contributingFactors, N'ADCs (e.g., Pyxis or Omnicell) Issues', 0),
        ('cf-communication-failure', @contributingFactors, 'Communication Failure', 1),
        ('cf-competency-deficit', @contributingFactors, 'Competency Deficit', 2),
        ('cf-computer-error', @contributingFactors, 'Computer Error', 3),
        ('cf-failure-in-performing-double-check', @contributingFactors, 'Failure in Performing Double Check', 4),
        ('cf-failure-to-adhere-to-work-procedures', @contributingFactors, 'Failure to Adhere to Work Procedures', 5),
        ('cf-fatigue-lack-of-sleep', @contributingFactors, 'Fatigue / Lack of Sleep', 6),
        ('cf-frequent-interruption-and-distractions', @contributingFactors, 'Frequent Interruption and Distractions', 7),
        ('cf-illegible-handwriting', @contributingFactors, 'Illegible Handwriting', 8),
        ('cf-incorrect-missing-patient-information', @contributingFactors, 'Incorrect / Missing Patient Information', 9),
        ('cf-inexperienced-personnel', @contributingFactors, 'Inexperienced Personnel', 10),
        ('cf-lighting-issues', @contributingFactors, 'Lighting Issues', 11),
        ('cf-look-alike-sound-alike-medication', @contributingFactors, 'Look Alike - Sound Alike Medication', 12),
        ('cf-missing-incomplete-instructions', @contributingFactors, 'Missing / Incomplete Instructions', 13),
        ('cf-na', @contributingFactors, 'NA', 14),
        ('cf-noise-level', @contributingFactors, 'Noise Level', 15),
        ('cf-patient-family-education', @contributingFactors, 'Patient / Family Education', 16),
        ('cf-peak-hours', @contributingFactors, 'Peak Hours', 17),
        ('cf-performance-deficit', @contributingFactors, 'Performance Deficit', 18),
        ('cf-policy-procedure-issue', @contributingFactors, 'Policy/Procedure Issue', 19),
        ('cf-pump-infusion-issues', @contributingFactors, 'Pump/Infusion Issues', 20),
        ('cf-reconciliation-failure', @contributingFactors, 'Reconciliation Failure', 21),
        ('cf-refusal-of-medication', @contributingFactors, 'Refusal of Medication', 22),
        ('cf-self-administration', @contributingFactors, 'Self Administration', 23),
        ('cf-staffing', @contributingFactors, 'Staffing', 24),
        ('cf-stress-high-workload', @contributingFactors, 'Stress (High Volume Workload, etc.)', 25),
        ('cf-unapproved-abbreviation-use', @contributingFactors, 'Unapproved Abbreviation Use', 26)
END
GO

-- 3b) Top up Contributing Factors on a DB that was already seeded before this
--     expanded list existed (the block above only fires on an empty table).
IF EXISTS (SELECT 1 FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'contributingFactors')
BEGIN
    DECLARE @cf INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'contributingFactors')

    MERGE [dbo].[DropdownValues] AS target
    USING (VALUES
        ('cf-adcs-issues', N'ADCs (e.g., Pyxis or Omnicell) Issues', 0),
        ('cf-communication-failure', 'Communication Failure', 1),
        ('cf-competency-deficit', 'Competency Deficit', 2),
        ('cf-computer-error', 'Computer Error', 3),
        ('cf-failure-in-performing-double-check', 'Failure in Performing Double Check', 4),
        ('cf-failure-to-adhere-to-work-procedures', 'Failure to Adhere to Work Procedures', 5),
        ('cf-fatigue-lack-of-sleep', 'Fatigue / Lack of Sleep', 6),
        ('cf-frequent-interruption-and-distractions', 'Frequent Interruption and Distractions', 7),
        ('cf-illegible-handwriting', 'Illegible Handwriting', 8),
        ('cf-incorrect-missing-patient-information', 'Incorrect / Missing Patient Information', 9),
        ('cf-inexperienced-personnel', 'Inexperienced Personnel', 10),
        ('cf-lighting-issues', 'Lighting Issues', 11),
        ('cf-look-alike-sound-alike-medication', 'Look Alike - Sound Alike Medication', 12),
        ('cf-missing-incomplete-instructions', 'Missing / Incomplete Instructions', 13),
        ('cf-na', 'NA', 14),
        ('cf-noise-level', 'Noise Level', 15),
        ('cf-patient-family-education', 'Patient / Family Education', 16),
        ('cf-peak-hours', 'Peak Hours', 17),
        ('cf-performance-deficit', 'Performance Deficit', 18),
        ('cf-policy-procedure-issue', 'Policy/Procedure Issue', 19),
        ('cf-pump-infusion-issues', 'Pump/Infusion Issues', 20),
        ('cf-reconciliation-failure', 'Reconciliation Failure', 21),
        ('cf-refusal-of-medication', 'Refusal of Medication', 22),
        ('cf-self-administration', 'Self Administration', 23),
        ('cf-staffing', 'Staffing', 24),
        ('cf-stress-high-workload', 'Stress (High Volume Workload, etc.)', 25),
        ('cf-unapproved-abbreviation-use', 'Unapproved Abbreviation Use', 26)
    ) AS src ([Id], [Value], [SortOrder])
    ON target.[Id] = src.[Id] AND target.[DropdownDefinitionId] = @cf
    WHEN MATCHED THEN
        UPDATE SET target.[SortOrder] = src.[SortOrder]
    WHEN NOT MATCHED THEN
        INSERT ([Id], [DropdownDefinitionId], [Value], [SortOrder])
        VALUES (src.[Id], @cf, src.[Value], src.[SortOrder]);
END
GO

-- 4) EF migration history (so `dotnet ef database update` recognizes these as applied) --

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260810171604_AlignReportFieldsWithWizard')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES ('20260810171604_AlignReportFieldsWithWizard', '9.0.0')
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260810171929_SyncReportFieldsWithWizard')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES ('20260810171929_SyncReportFieldsWithWizard', '9.0.0')
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260810180622_AddDropdownConfigurations')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES ('20260810180622_AddDropdownConfigurations', '9.0.0')
GO
