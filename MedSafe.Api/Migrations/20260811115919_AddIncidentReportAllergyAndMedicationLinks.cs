using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentReportAllergyAndMedicationLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentReportAllergy' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.IncidentReportAllergy
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IncidentReportAllergy PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        AllergyId INT NOT NULL,
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_IncidentReportAllergy_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_IRAllergy_Incident_Allergy UNIQUE (IncidentReportId, AllergyId),
        CONSTRAINT FK_IRAllergy_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports (Id),
        CONSTRAINT FK_IRAllergy_Allergy FOREIGN KEY (AllergyId) REFERENCES dbo.Allergy (Id)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentReportCurrentMedication' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.IncidentReportCurrentMedication
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IncidentReportCurrentMedication PRIMARY KEY,
        IncidentReportId INT NOT NULL,
        CurrentMedicationId INT NOT NULL,
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_IncidentReportCurrentMedication_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_IRCurrentMedication_Incident_Medication UNIQUE (IncidentReportId, CurrentMedicationId),
        CONSTRAINT FK_IRCurrentMedication_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports (Id),
        CONSTRAINT FK_IRCurrentMedication_CurrentMedication FOREIGN KEY (CurrentMedicationId) REFERENCES dbo.CurrentMedication (Id)
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.IncidentReportCurrentMedication') IS NOT NULL DROP TABLE dbo.IncidentReportCurrentMedication;
IF OBJECT_ID('dbo.IncidentReportAllergy') IS NOT NULL DROP TABLE dbo.IncidentReportAllergy;
");
        }
    }
}
