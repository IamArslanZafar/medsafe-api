using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyAllergyMedicationColumns : Migration
    {
        // KnownPatientAllergies/CurrentPatientMedications on IncidentReports were the
        // pre-normalization free-text columns. Nothing reads or writes them anymore now
        // that IncidentReportAllergy/IncidentReportCurrentMedication junction tables are
        // the source of truth, so they're dead weight — drop them.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentReports', 'KnownPatientAllergies') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [KnownPatientAllergies];
");
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentReports', 'CurrentPatientMedications') IS NOT NULL
    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [CurrentPatientMedications];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentReports', 'KnownPatientAllergies') IS NULL
    ALTER TABLE [dbo].[IncidentReports] ADD [KnownPatientAllergies] NVARCHAR(MAX) NULL;
");
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentReports', 'CurrentPatientMedications') IS NULL
    ALTER TABLE [dbo].[IncidentReports] ADD [CurrentPatientMedications] NVARCHAR(MAX) NULL;
");
        }
    }
}
