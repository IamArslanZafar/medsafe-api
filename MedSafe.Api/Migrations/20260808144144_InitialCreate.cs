using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetRoles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Urgency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastTriggered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmitterRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FacilityUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientRef = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgeBand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KnownAllergies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentMedications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedicationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dose = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DoseUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Route = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimingGiven = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Formulation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StageOfProcess = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReactionDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuspectedCausality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeverityCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeriousnessCriteria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncidentNarrative = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncidentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContributingFactors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImmediateAction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientOutcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientOutcomeDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reviewer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewerRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClinicalAssessment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpActions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionOwner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "IncidentReports");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
