using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceCodesToReportModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeedbackCode",
                table: "StudentFeedbacks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectCode",
                table: "QualityProjects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterventionCode",
                table: "ClinicalPharmacyInterventions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedbackCode",
                table: "StudentFeedbacks");

            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "QualityProjects");

            migrationBuilder.DropColumn(
                name: "InterventionCode",
                table: "ClinicalPharmacyInterventions");
        }
    }
}
