using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class RebuildIncidentReportsSchema : Migration
    {
        // The live IncidentReports table was rebuilt directly on the DB (outside this
        // project's migrations) to the clean schema the app now targets. Locally, the
        // table still has the old columns from earlier migrations, so this drops and
        // recreates it — safe because local IncidentReports is dev/test data only.
        // Guarded on IncidentReportNumber so this is a no-op wherever the new schema
        // already exists (i.e. live).
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.IncidentReports') IS NOT NULL AND COL_LENGTH('dbo.IncidentReports', 'IncidentReportNumber') IS NULL
BEGIN
    -- Drop any FK constraints (on this or other tables) referencing the old table first,
    -- since we don't know locally which earlier migrations left pointing at it.
    DECLARE @dropFkSql NVARCHAR(MAX) = N'';
    SELECT @dropFkSql = @dropFkSql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(fk.schema_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM sys.foreign_keys fk
    WHERE fk.referenced_object_id = OBJECT_ID('dbo.IncidentReports');
    IF LEN(@dropFkSql) > 0 EXEC sp_executesql @dropFkSql;

    DROP TABLE dbo.IncidentReports;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.IncidentReports') IS NULL
BEGIN
    CREATE TABLE dbo.IncidentReports
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IncidentReports PRIMARY KEY,
        IncidentReportNumber NVARCHAR(30) NOT NULL,
        SubmittedAt DATETIME2 NOT NULL CONSTRAINT DF_IncidentReports_SubmittedAt DEFAULT (SYSUTCDATETIME()),
        SubmittedByUserId INT NOT NULL,
        SubmittedByRole NVARCHAR(50) NOT NULL,
        ReportingFacilityUnit NVARCHAR(150) NOT NULL,
        ReportStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_IncidentReports_ReportStatus DEFAULT ('Submitted'),

        PatientReferenceToken VARCHAR(35) NOT NULL,
        PatientAge SMALLINT NOT NULL,
        PatientSex NVARCHAR(20) NOT NULL,
        PatientWeightKg DECIMAL(6, 2) NULL,
        PatientMedicalHistory NVARCHAR(MAX) NULL,
        KnownPatientAllergies NVARCHAR(MAX) NULL,
        CurrentPatientMedications NVARCHAR(MAX) NULL,

        MedicationName NVARCHAR(250) NOT NULL,
        GenericActiveIngredient NVARCHAR(250) NULL,
        DoseValue DECIMAL(18, 4) NOT NULL,
        DoseUnitId INT NOT NULL,
        RouteId INT NOT NULL,
        FrequencyId INT NULL,
        FormulationId INT NULL,
        MedicationGivenAt DATETIME2 NOT NULL,
        BatchLotNumber NVARCHAR(100) NULL,

        ReportType NVARCHAR(30) NOT NULL,
        ErrorCategoryId INT NULL,
        StageOfProcessId INT NULL,
        AdrReactionDescription NVARCHAR(MAX) NULL,
        SuspectedCausality NVARCHAR(100) NULL,
        HarmLevelCode CHAR(1) NOT NULL,
        IncidentOccurredAt DATETIME2 NOT NULL,
        IncidentLocation NVARCHAR(200) NOT NULL,
        IncidentNarrative NVARCHAR(MAX) NOT NULL,

        ImmediateActionTaken NVARCHAR(MAX) NULL,
        PatientOutcomeId INT NOT NULL,
        PatientOutcomeDetails NVARCHAR(MAX) NULL,

        CONSTRAINT UQ_IncidentReports_IncidentReportNumber UNIQUE (IncidentReportNumber),
        CONSTRAINT FK_IncidentReports_Route FOREIGN KEY (RouteId) REFERENCES dbo.Route (Id),
        CONSTRAINT FK_IncidentReports_PatientOutcome FOREIGN KEY (PatientOutcomeId) REFERENCES dbo.PatientOutcome (Id),
        CONSTRAINT FK_IncidentReports_StageOfProcess FOREIGN KEY (StageOfProcessId) REFERENCES dbo.StageOfProcess (Id),
        CONSTRAINT FK_IncidentReports_Formulation FOREIGN KEY (FormulationId) REFERENCES dbo.Formulation (Id),
        CONSTRAINT FK_IncidentReports_Frequency FOREIGN KEY (FrequencyId) REFERENCES dbo.Frequency (Id),
        CONSTRAINT FK_IncidentReports_ErrorCategory FOREIGN KEY (ErrorCategoryId) REFERENCES dbo.ErrorCategory (Id),
        CONSTRAINT FK_IncidentReports_DoseUnit FOREIGN KEY (DoseUnitId) REFERENCES dbo.DoseUnit (Id)
    );

    CREATE INDEX IX_IncidentReports_DoseUnitId ON dbo.IncidentReports (DoseUnitId);
    CREATE INDEX IX_IncidentReports_ErrorCategoryId ON dbo.IncidentReports (ErrorCategoryId);
    CREATE INDEX IX_IncidentReports_FormulationId ON dbo.IncidentReports (FormulationId);
    CREATE INDEX IX_IncidentReports_FrequencyId ON dbo.IncidentReports (FrequencyId);
    CREATE INDEX IX_IncidentReports_PatientOutcomeId ON dbo.IncidentReports (PatientOutcomeId);
    CREATE INDEX IX_IncidentReports_StageOfProcessId ON dbo.IncidentReports (StageOfProcessId);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentReportContributingFactor' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.IncidentReportContributingFactor
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IncidentReportContributingFactor PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        ContributingFactorId INT NOT NULL,
        CreatedBy INT NOT NULL CONSTRAINT DF_IRCF_CreatedBy DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_IRCF_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_IRCF_Incident_ContributingFactor UNIQUE (IncidentReportId, ContributingFactorId),
        CONSTRAINT FK_IRCF_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports (Id),
        CONSTRAINT FK_IRCF_ContributingFactor FOREIGN KEY (ContributingFactorId) REFERENCES dbo.ContributingFactor (Id)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentReportSeriousnessCriterion' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.IncidentReportSeriousnessCriterion
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IncidentReportSeriousnessCriterion PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        SeriousnessCriterionId INT NOT NULL,
        CreatedBy INT NOT NULL CONSTRAINT DF_IRSC_CreatedBy DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_IRSC_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_IRSC_Incident_Criterion UNIQUE (IncidentReportId, SeriousnessCriterionId),
        CONSTRAINT FK_IRSC_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports (Id),
        CONSTRAINT FK_IRSC_SeriousnessCriterion FOREIGN KEY (SeriousnessCriterionId) REFERENCES dbo.SeriousnessCriterion (Id)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentReportAttachment' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.IncidentReportAttachment
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IncidentReportAttachment PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        OriginalFileName NVARCHAR(255) NOT NULL,
        StorageKey NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(150) NOT NULL,
        FileSizeBytes BIGINT NOT NULL,
        Sha256Hash CHAR(64) NULL,
        UploadedByUserId INT NOT NULL,
        UploadedAt DATETIME2 NOT NULL CONSTRAINT DF_IncidentReportAttachment_UploadedAt DEFAULT (SYSUTCDATETIME()),
        IsDeleted BIT NOT NULL CONSTRAINT DF_IncidentReportAttachment_IsDeleted DEFAULT (0),
        DeletedByUserId INT NULL,
        DeletedAt DATETIME2 NULL,
        CONSTRAINT FK_IncidentReportAttachment_Incident FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports (Id)
    );

    CREATE INDEX IX_IncidentReportAttachment_IncidentReportId ON dbo.IncidentReportAttachment (IncidentReportId);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.IncidentReportAttachment') IS NOT NULL DROP TABLE dbo.IncidentReportAttachment;
IF OBJECT_ID('dbo.IncidentReportSeriousnessCriterion') IS NOT NULL DROP TABLE dbo.IncidentReportSeriousnessCriterion;
IF OBJECT_ID('dbo.IncidentReportContributingFactor') IS NOT NULL DROP TABLE dbo.IncidentReportContributingFactor;
");
        }
    }
}
