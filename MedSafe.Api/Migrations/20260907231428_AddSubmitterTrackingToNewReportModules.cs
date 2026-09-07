using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmitterTrackingToNewReportModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmittedByRole",
                table: "StudentFeedbacks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: "StudentFeedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByRole",
                table: "QualityProjects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByRole",
                table: "CpdActivities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByRole",
                table: "ClinicalPharmacyInterventions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedByRole",
                table: "StudentFeedbacks");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "StudentFeedbacks");

            migrationBuilder.DropColumn(
                name: "CreatedByRole",
                table: "QualityProjects");

            migrationBuilder.DropColumn(
                name: "SubmittedByRole",
                table: "CpdActivities");

            migrationBuilder.DropColumn(
                name: "SubmittedByRole",
                table: "ClinicalPharmacyInterventions");
        }
    }
}
