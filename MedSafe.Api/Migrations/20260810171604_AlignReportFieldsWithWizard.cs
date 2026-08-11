using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AlignReportFieldsWithWizard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContributingFactors",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "FacilityUnit",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "MedicalHistory",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ReactionDescription",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "SuspectedCausality",
                table: "IncidentReports");

            migrationBuilder.RenameColumn(
                name: "SeverityCategory",
                table: "IncidentReports",
                newName: "PatientName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PatientName",
                table: "IncidentReports",
                newName: "SeverityCategory");

            migrationBuilder.AddColumn<string>(
                name: "ContributingFactors",
                table: "IncidentReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacilityUnit",
                table: "IncidentReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "IncidentReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedicalHistory",
                table: "IncidentReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReactionDescription",
                table: "IncidentReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportType",
                table: "IncidentReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuspectedCausality",
                table: "IncidentReports",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
