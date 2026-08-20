-- Brings the live DB up to parity with local for the lookup tables and
-- IncidentReports FK columns that were introduced during the local-only
-- development stretch and never pushed live: ReportType, VisitType,
-- HarmLevel, AdrSeverity, SuspectedCausality, ReportingSource,
-- UnitDepartment, Section, IncidentReportHealthcareProfessional,
-- IncidentReportConcomitantMedication, plus the matching FK id columns on
-- IncidentReports (ReportTypeId, HarmLevelId, SuspectedCausalityId,
-- AdrSeverityId, IncidentUnitId, VisitTypeId, ReportingSourceId,
-- RelevantMedicalHistory, AdrAdditionalInformation).
--
-- Purely additive — no columns are dropped or renamed, so the legacy
-- ReportType/SuspectedCausality string columns and the DropdownDefinitions/
-- DropdownValues tables are left exactly as they are on live.
--
-- Idempotent — safe to run more than once, and safe to run directly against
-- the live database (every step checks before acting).

USE [db_acd20d_medsafe001]
GO

-- 1) Simple lookup tables ----------------------------------------------------

IF OBJECT_ID('dbo.ReportType', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportType](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] [nvarchar](50) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [DisplayOrder] [int] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedAt] [datetime2] NULL
    )
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.ReportType)
BEGIN
    SET IDENTITY_INSERT dbo.ReportType ON
    INSERT INTO dbo.ReportType (Id, Code, Name, Description, IsActive, DisplayOrder) VALUES
    (1, 'MEDICATION_ERROR', 'Medication Error', 'Medication error report. Errors intercepted before reaching the patient are handled within this report type.', 1, 1),
    (2, 'ADR', 'ADR Reaction', 'Adverse Drug Reaction report.', 1, 2)
    SET IDENTITY_INSERT dbo.ReportType OFF
END
GO

IF OBJECT_ID('dbo.VisitType', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[VisitType](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] [nvarchar](50) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [DisplayOrder] [int] NOT NULL DEFAULT 0,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime2] NULL
    )
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.VisitType)
BEGIN
    SET IDENTITY_INSERT dbo.VisitType ON
    INSERT INTO dbo.VisitType (Id, Code, Name, Description, IsActive, DisplayOrder, CreatedBy, CreatedDate) VALUES
    (1, 'INPATIENT', 'Inpatient', 'Patient was admitted as an inpatient.', 1, 1, 1, SYSUTCDATETIME()),
    (2, 'OUTPATIENT', 'Outpatient', 'Patient was receiving outpatient care.', 1, 2, 1, SYSUTCDATETIME()),
    (3, 'DISCHARGE', 'Discharge', 'Report relates to the discharge process.', 1, 3, 1, SYSUTCDATETIME()),
    (4, 'HOME_VISIT', 'Home Visit', 'Patient was seen during a home visit.', 1, 4, 1, SYSUTCDATETIME())
    SET IDENTITY_INSERT dbo.VisitType OFF
END
GO

IF OBJECT_ID('dbo.HarmLevel', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[HarmLevel](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] [char](1) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [Description] [nvarchar](500) NOT NULL,
        [SeverityRank] [tinyint] NOT NULL,
        [GroupName] [nvarchar](50) NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [DisplayOrder] [int] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedAt] [datetime2] NULL
    )
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.HarmLevel)
BEGIN
    SET IDENTITY_INSERT dbo.HarmLevel ON
    INSERT INTO dbo.HarmLevel (Id, Code, Name, Description, SeverityRank, GroupName, IsActive, DisplayOrder) VALUES
    (1, 'A', 'Category A', 'Circumstances capable of causing error; no error occurred.', 1, 'No error', 1, 1),
    (2, 'B', 'Category B', 'Error occurred but did not reach the patient.', 2, 'No harm', 1, 2),
    (3, 'C', 'Category C', 'Error reached the patient but caused no harm.', 3, 'No harm', 1, 3),
    (4, 'D', 'Category D', 'Error reached the patient and required monitoring to confirm no harm.', 4, 'No harm', 1, 4),
    (5, 'E', 'Category E', 'Error caused temporary harm requiring intervention.', 5, 'Harm', 1, 5),
    (6, 'F', 'Category F', 'Error caused temporary harm requiring hospitalisation.', 6, 'Harm', 1, 6),
    (7, 'G', 'Category G', 'Error caused permanent harm.', 7, 'Harm', 1, 7),
    (8, 'H', 'Category H', 'Error required intervention to sustain life.', 8, 'Harm', 1, 8),
    (9, 'I', 'Category I', 'Error may have contributed to or resulted in death.', 9, 'Death', 1, 9)
    SET IDENTITY_INSERT dbo.HarmLevel OFF
END
GO

IF OBJECT_ID('dbo.AdrSeverity', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AdrSeverity](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] [nvarchar](50) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [DisplayOrder] [int] NOT NULL DEFAULT 0,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime2] NULL
    )
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.AdrSeverity)
BEGIN
    SET IDENTITY_INSERT dbo.AdrSeverity ON
    INSERT INTO dbo.AdrSeverity (Id, Code, Name, Description, IsActive, DisplayOrder, CreatedBy, CreatedDate) VALUES
    (1, 'MILD', 'Mild ADR', 'Mild intensity adverse drug reaction.', 1, 1, 1, SYSUTCDATETIME()),
    (2, 'MODERATE', 'Moderate ADR', 'Moderate intensity adverse drug reaction.', 1, 2, 1, SYSUTCDATETIME()),
    (3, 'SEVERE', 'Severe ADR', 'Severe intensity adverse drug reaction.', 1, 3, 1, SYSUTCDATETIME())
    SET IDENTITY_INSERT dbo.AdrSeverity OFF
END
GO

IF OBJECT_ID('dbo.SuspectedCausality', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SuspectedCausality](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] [nvarchar](50) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [DisplayOrder] [int] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedAt] [datetime2] NULL
    )
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.SuspectedCausality)
BEGIN
    SET IDENTITY_INSERT dbo.SuspectedCausality ON
    INSERT INTO dbo.SuspectedCausality (Id, Code, Name, IsActive, DisplayOrder) VALUES
    (1, 'CERTAIN', 'Certain', 1, 1),
    (2, 'PROBABLE_LIKELY', 'Probable / Likely', 1, 2),
    (3, 'POSSIBLE', 'Possible', 1, 3),
    (4, 'UNLIKELY', 'Unlikely', 1, 4),
    (5, 'CONDITIONAL_UNCLASSIFIED', 'Conditional / Unclassified', 1, 5),
    (6, 'UNASSESSABLE', 'Unassessable', 1, 6)
    SET IDENTITY_INSERT dbo.SuspectedCausality OFF
END
GO

IF OBJECT_ID('dbo.ReportingSource', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportingSource](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] [nvarchar](50) NOT NULL,
        [Name] [nvarchar](150) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [DisplayOrder] [int] NOT NULL DEFAULT 0,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime2] NULL
    )
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.ReportingSource)
BEGIN
    SET IDENTITY_INSERT dbo.ReportingSource ON
    INSERT INTO dbo.ReportingSource (Id, Code, Name, Description, IsActive, DisplayOrder, CreatedBy, CreatedDate) VALUES
    (1, 'DIRECT_CARE_OBSERVED', 'Direct Care / Observed', 'Reaction or incident directly observed during patient care.', 1, 1, 1, SYSUTCDATETIME()),
    (2, 'INTERNAL_SAFETY_REPORTING', 'Internal Safety Reporting', 'Reported through internal safety reporting.', 1, 2, 1, SYSUTCDATETIME()),
    (3, 'PATIENT_CAREGIVER_REPORTED', 'Patient / Caregiver Reported', 'Reported by the patient or caregiver.', 1, 3, 1, SYSUTCDATETIME()),
    (4, 'OTHER', 'Other', 'Other reporting source.', 1, 99, 1, SYSUTCDATETIME())
    SET IDENTITY_INSERT dbo.ReportingSource OFF
END
GO

IF OBJECT_ID('dbo.UnitDepartment', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UnitDepartment](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] [nvarchar](50) NULL,
        [Name] [nvarchar](200) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [DisplayOrder] [int] NOT NULL DEFAULT 0,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] [int] NULL,
        [ModifiedDate] [datetime2] NULL
    )
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.UnitDepartment)
BEGIN
    SET IDENTITY_INSERT dbo.UnitDepartment ON
    INSERT INTO dbo.UnitDepartment (Id, Code, Name, Description, IsActive, DisplayOrder, CreatedBy, CreatedDate) VALUES
    (1, NULL, 'Emergency Department (ED)', NULL, 1, 0, 1, SYSUTCDATETIME()),
    (2, NULL, 'Intensive Care Unit (ICU)', NULL, 1, 0, 1, SYSUTCDATETIME()),
    (3, NULL, 'Ward A', NULL, 1, 0, 1, SYSUTCDATETIME()),
    (4, 'CARDIOLOGY_WARD', 'Cardiology Ward', 'Cardiology inpatient ward.', 1, 1, 1, SYSUTCDATETIME()),
    (5, 'MEDICAL_WARD', 'Medical Ward', 'General medical ward.', 1, 4, 1, SYSUTCDATETIME()),
    (6, 'SURGICAL_WARD', 'Surgical Ward', 'General surgical ward.', 1, 5, 1, SYSUTCDATETIME()),
    (7, 'PHARMACY', 'Pharmacy', 'Pharmacy department.', 1, 6, 1, SYSUTCDATETIME()),
    (8, 'OUTPATIENT_CLINIC', 'Outpatient Clinic', 'Outpatient clinical area.', 1, 7, 1, SYSUTCDATETIME()),
    (9, 'OTHER', 'Other', 'Other clinical unit or department.', 1, 99, 1, SYSUTCDATETIME())
    SET IDENTITY_INSERT dbo.UnitDepartment OFF
END
GO

-- 2) Section (child of UnitDepartment; couldn't be created earlier today
--    because UnitDepartment didn't exist on live yet) -----------------------

IF OBJECT_ID('dbo.Section', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Section (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        UnitDepartmentId INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        DisplayOrder INT NOT NULL DEFAULT 0,
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME2 NULL,
        CONSTRAINT FK_Section_UnitDepartment FOREIGN KEY (UnitDepartmentId) REFERENCES dbo.UnitDepartment(Id)
    );
    CREATE NONCLUSTERED INDEX IX_Section_UnitDepartmentId ON dbo.Section(UnitDepartmentId);
END
GO

-- 3) Incident report detail (child) tables -----------------------------------

IF OBJECT_ID('dbo.IncidentReportHealthcareProfessional', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IncidentReportHealthcareProfessional](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [IncidentReportId] [int] NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [ProfessionId] [int] NOT NULL,
        [PositionId] [int] NOT NULL,
        [ContactNumber] [nvarchar](50) NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_IRHealthcareProfessional_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports(Id),
        CONSTRAINT FK_IRHealthcareProfessional_Profession FOREIGN KEY (ProfessionId) REFERENCES dbo.Profession(Id),
        CONSTRAINT FK_IRHealthcareProfessional_Position FOREIGN KEY (PositionId) REFERENCES dbo.Position(Id),
        CONSTRAINT FK_IRHealthcareProfessional_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id)
    )
    CREATE NONCLUSTERED INDEX IX_IRHealthcareProfessional_IncidentReportId ON dbo.IncidentReportHealthcareProfessional(IncidentReportId)
END
GO

IF OBJECT_ID('dbo.IncidentReportConcomitantMedication', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IncidentReportConcomitantMedication](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [IncidentReportId] [int] NOT NULL,
        [CareSettingCode] [nvarchar](20) NOT NULL,
        [MedicationText] [nvarchar](max) NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime2] NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_IRConcomitantMedication_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports(Id),
        CONSTRAINT FK_IRConcomitantMedication_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id)
    )
    CREATE NONCLUSTERED INDEX IX_IRConcomitantMedication_IncidentReportId ON dbo.IncidentReportConcomitantMedication(IncidentReportId)
END
GO

-- 4) IncidentReports: missing FK id columns + free-text fields ---------------

IF COL_LENGTH('dbo.IncidentReports', 'ReportTypeId') IS NULL
    ALTER TABLE dbo.IncidentReports ADD ReportTypeId INT NULL
GO
IF COL_LENGTH('dbo.IncidentReports', 'HarmLevelId') IS NULL
    ALTER TABLE dbo.IncidentReports ADD HarmLevelId INT NULL
GO
IF COL_LENGTH('dbo.IncidentReports', 'SuspectedCausalityId') IS NULL
    ALTER TABLE dbo.IncidentReports ADD SuspectedCausalityId INT NULL
GO
IF COL_LENGTH('dbo.IncidentReports', 'AdrSeverityId') IS NULL
    ALTER TABLE dbo.IncidentReports ADD AdrSeverityId INT NULL
GO
IF COL_LENGTH('dbo.IncidentReports', 'IncidentUnitId') IS NULL
    ALTER TABLE dbo.IncidentReports ADD IncidentUnitId INT NULL
GO
IF COL_LENGTH('dbo.IncidentReports', 'VisitTypeId') IS NULL
    ALTER TABLE dbo.IncidentReports ADD VisitTypeId INT NULL
GO
IF COL_LENGTH('dbo.IncidentReports', 'ReportingSourceId') IS NULL
    ALTER TABLE dbo.IncidentReports ADD ReportingSourceId INT NULL
GO
IF COL_LENGTH('dbo.IncidentReports', 'RelevantMedicalHistory') IS NULL
    ALTER TABLE dbo.IncidentReports ADD RelevantMedicalHistory NVARCHAR(MAX) NULL
GO
IF COL_LENGTH('dbo.IncidentReports', 'AdrAdditionalInformation') IS NULL
    ALTER TABLE dbo.IncidentReports ADD AdrAdditionalInformation NVARCHAR(MAX) NULL
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_ReportType')
    ALTER TABLE dbo.IncidentReports ADD CONSTRAINT FK_IncidentReports_ReportType FOREIGN KEY (ReportTypeId) REFERENCES dbo.ReportType(Id)
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_HarmLevel')
    ALTER TABLE dbo.IncidentReports ADD CONSTRAINT FK_IncidentReports_HarmLevel FOREIGN KEY (HarmLevelId) REFERENCES dbo.HarmLevel(Id)
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_SuspectedCausality')
    ALTER TABLE dbo.IncidentReports ADD CONSTRAINT FK_IncidentReports_SuspectedCausality FOREIGN KEY (SuspectedCausalityId) REFERENCES dbo.SuspectedCausality(Id)
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_AdrSeverity')
    ALTER TABLE dbo.IncidentReports ADD CONSTRAINT FK_IncidentReports_AdrSeverity FOREIGN KEY (AdrSeverityId) REFERENCES dbo.AdrSeverity(Id)
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_IncidentUnit')
    ALTER TABLE dbo.IncidentReports ADD CONSTRAINT FK_IncidentReports_IncidentUnit FOREIGN KEY (IncidentUnitId) REFERENCES dbo.UnitDepartment(Id)
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_VisitType')
    ALTER TABLE dbo.IncidentReports ADD CONSTRAINT FK_IncidentReports_VisitType FOREIGN KEY (VisitTypeId) REFERENCES dbo.VisitType(Id)
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_ReportingSource')
    ALTER TABLE dbo.IncidentReports ADD CONSTRAINT FK_IncidentReports_ReportingSource FOREIGN KEY (ReportingSourceId) REFERENCES dbo.ReportingSource(Id)
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_Section')
    ALTER TABLE dbo.IncidentReports ADD CONSTRAINT FK_IncidentReports_Section FOREIGN KEY (SectionId) REFERENCES dbo.Section(Id)
GO

-- 5) Verify -------------------------------------------------------------------

SELECT 'ReportType' AS TableName, COUNT(*) AS Rows FROM dbo.ReportType
UNION ALL SELECT 'VisitType', COUNT(*) FROM dbo.VisitType
UNION ALL SELECT 'HarmLevel', COUNT(*) FROM dbo.HarmLevel
UNION ALL SELECT 'AdrSeverity', COUNT(*) FROM dbo.AdrSeverity
UNION ALL SELECT 'SuspectedCausality', COUNT(*) FROM dbo.SuspectedCausality
UNION ALL SELECT 'ReportingSource', COUNT(*) FROM dbo.ReportingSource
UNION ALL SELECT 'UnitDepartment', COUNT(*) FROM dbo.UnitDepartment
UNION ALL SELECT 'Section', COUNT(*) FROM dbo.Section
UNION ALL SELECT 'IncidentReportHealthcareProfessional', COUNT(*) FROM dbo.IncidentReportHealthcareProfessional
UNION ALL SELECT 'IncidentReportConcomitantMedication', COUNT(*) FROM dbo.IncidentReportConcomitantMedication;

SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IncidentReports')
AND name IN ('ReportTypeId','HarmLevelId','SuspectedCausalityId','AdrSeverityId','IncidentUnitId','VisitTypeId','ReportingSourceId','RelevantMedicalHistory','AdrAdditionalInformation');

SELECT name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.IncidentReports')
AND name LIKE 'FK_IncidentReports_%' ORDER BY name;
GO
