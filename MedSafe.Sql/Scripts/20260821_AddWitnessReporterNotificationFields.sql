-- Adds the second round of gap-analysis fields on top of yesterday's sync:
--   1) IncidentReports: PatientDateOfBirth, EnteredByTitle, ReporterPhoneNumber
--   2) IncidentReportAttachment: Category, Description
--   3) IncidentReportWitness, IncidentReportOtherDepartment, IncidentReportReporter,
--      IncidentReportManualNotification — new child tables
--   4) VisitType: "Location/Person Not Applicable"
--   5) ErrorCategory: "Wrong/Missing Label" (Dispensing), "Wrong/Missing Indication" (Monitoring)
--
-- Purely additive — no columns dropped or renamed.
-- Idempotent — safe to run more than once, and safe to run directly against the
-- live database (every step checks before acting).

USE [db_acd20d_medsafe001]
GO

-- 1) IncidentReports: new simple fields ---------------------------------------
IF COL_LENGTH('dbo.IncidentReports', 'PatientDateOfBirth') IS NULL
    ALTER TABLE dbo.IncidentReports ADD PatientDateOfBirth DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.IncidentReports', 'EnteredByTitle') IS NULL
    ALTER TABLE dbo.IncidentReports ADD EnteredByTitle NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.IncidentReports', 'ReporterPhoneNumber') IS NULL
    ALTER TABLE dbo.IncidentReports ADD ReporterPhoneNumber NVARCHAR(50) NULL;
GO

-- 2) IncidentReportAttachment: Category + Description --------------------------
IF COL_LENGTH('dbo.IncidentReportAttachment', 'Category') IS NULL
    ALTER TABLE dbo.IncidentReportAttachment ADD Category NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.IncidentReportAttachment', 'Description') IS NULL
    ALTER TABLE dbo.IncidentReportAttachment ADD Description NVARCHAR(500) NULL;
GO

-- 3) IncidentReportWitness ------------------------------------------------------
IF OBJECT_ID('dbo.IncidentReportWitness', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncidentReportWitness (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Address NVARCHAR(500) NULL,
        PhoneNumber NVARCHAR(50) NULL,
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_IRWitness_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports(Id)
    );
    CREATE NONCLUSTERED INDEX IX_IRWitness_IncidentReportId ON dbo.IncidentReportWitness(IncidentReportId);
END
GO

-- 4) IncidentReportOtherDepartment ----------------------------------------------
IF OBJECT_ID('dbo.IncidentReportOtherDepartment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncidentReportOtherDepartment (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        UnitDepartmentId INT NOT NULL,
        CONSTRAINT FK_IROtherDepartment_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports(Id),
        CONSTRAINT FK_IROtherDepartment_UnitDepartment FOREIGN KEY (UnitDepartmentId) REFERENCES dbo.UnitDepartment(Id)
    );
    CREATE NONCLUSTERED INDEX IX_IROtherDepartment_IncidentReportId ON dbo.IncidentReportOtherDepartment(IncidentReportId);
END
GO

-- 5) IncidentReportReporter ------------------------------------------------------
IF OBJECT_ID('dbo.IncidentReportReporter', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncidentReportReporter (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        ReportedDate DATETIME2 NOT NULL,
        ProfessionId INT NULL,
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_IRReporter_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports(Id),
        CONSTRAINT FK_IRReporter_Profession FOREIGN KEY (ProfessionId) REFERENCES dbo.Profession(Id)
    );
    CREATE NONCLUSTERED INDEX IX_IRReporter_IncidentReportId ON dbo.IncidentReportReporter(IncidentReportId);
END
GO

-- 6) IncidentReportManualNotification ---------------------------------------------
IF OBJECT_ID('dbo.IncidentReportManualNotification', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncidentReportManualNotification (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        TypeOfPersonNotified NVARCHAR(200) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        NotifiedAt DATETIME2 NOT NULL,
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_IRManualNotification_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports(Id)
    );
    CREATE NONCLUSTERED INDEX IX_IRManualNotification_IncidentReportId ON dbo.IncidentReportManualNotification(IncidentReportId);
END
GO

-- 7) VisitType: "Location/Person Not Applicable" -------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.VisitType WHERE Code = 'NOT_APPLICABLE')
BEGIN
    DECLARE @NextVisitOrder INT = (SELECT ISNULL(MAX(DisplayOrder), 0) + 1 FROM dbo.VisitType);
    INSERT INTO dbo.VisitType (Code, Name, Description, IsActive, DisplayOrder, CreatedBy, CreatedDate) VALUES
    ('NOT_APPLICABLE', 'Location/Person Not Applicable', 'Neither an inpatient/outpatient visit nor a specific location applies.', 1, @NextVisitOrder, 1, SYSUTCDATETIME());
END
GO

-- 8) Two new stage-specific ErrorCategory values --------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.ErrorCategory WHERE Name = 'Wrong/Missing Label')
BEGIN
    DECLARE @Dispensing INT = (SELECT Id FROM dbo.StageOfProcess WHERE Name = 'Dispensing');
    DECLARE @NextOrder1 INT = (SELECT ISNULL(MAX(DisplayOrder), 0) + 1 FROM dbo.ErrorCategory);
    INSERT INTO dbo.ErrorCategory (Name, IsActive, DisplayOrder, StageOfProcessId) VALUES
    ('Wrong/Missing Label', 1, @NextOrder1, @Dispensing);
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.ErrorCategory WHERE Name = 'Wrong/Missing Indication')
BEGIN
    DECLARE @Monitoring INT = (SELECT Id FROM dbo.StageOfProcess WHERE Name = 'Monitoring');
    DECLARE @NextOrder2 INT = (SELECT ISNULL(MAX(DisplayOrder), 0) + 1 FROM dbo.ErrorCategory);
    INSERT INTO dbo.ErrorCategory (Name, IsActive, DisplayOrder, StageOfProcessId) VALUES
    ('Wrong/Missing Indication', 1, @NextOrder2, @Monitoring);
END
GO

-- 9) Verify -----------------------------------------------------------------------
SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IncidentReports') AND name IN ('PatientDateOfBirth','EnteredByTitle','ReporterPhoneNumber');
SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IncidentReportAttachment') AND name IN ('Category','Description');
SELECT 'IncidentReportWitness' AS TableName, COUNT(*) AS Rows FROM dbo.IncidentReportWitness
UNION ALL SELECT 'IncidentReportOtherDepartment', COUNT(*) FROM dbo.IncidentReportOtherDepartment
UNION ALL SELECT 'IncidentReportReporter', COUNT(*) FROM dbo.IncidentReportReporter
UNION ALL SELECT 'IncidentReportManualNotification', COUNT(*) FROM dbo.IncidentReportManualNotification;
SELECT Code, Name FROM dbo.VisitType ORDER BY DisplayOrder;
SELECT Name FROM dbo.ErrorCategory WHERE Name IN ('Wrong/Missing Label', 'Wrong/Missing Indication');
GO
