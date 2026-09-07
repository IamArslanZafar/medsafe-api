using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNewModulesBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalPharmacyInterventions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mrn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dob = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisitType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Facility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportingFacility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Medication = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentDose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Indication = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastDose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrugClass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterventionType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterventionSubtype = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdentifiedProblem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedAction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedDose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedFrequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedMonitoring = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClinicalRationale = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Importance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeSpent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedSaving = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Merp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Adr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrescriberName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrescriberDepartment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactDateTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrescriberResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowupRequired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowupDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DraftStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalPharmacyInterventions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CpdActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subcategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Credits = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Reflection = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CpdActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualityProjectCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QualityProjectId = table.Column<int>(type: "int", nullable: false),
                    ChangeIdea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TesterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timeframe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParticipantsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LearningGoal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudyResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudyLearning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Decision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityProjectCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualityProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrgName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SponsorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AimStatement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Problem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Outcomes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutcomeMeasures = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessMeasures = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitialActivities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barriers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stakeholders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Achievements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProgressNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearcherProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Orcid = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearcherProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchPublications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaLine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicationDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Journal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Authors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Identifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchPublications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallQuality = table.Column<int>(type: "int", nullable: false),
                    InstructorClarity = table.Column<int>(type: "int", nullable: false),
                    InstructorRatingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstructorComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialsSufficient = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaterialsComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnvironmentRating = table.Column<int>(type: "int", nullable: false),
                    SupportServicesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeedbacks", x => x.Id);
                });

            // NOTE: `dotnet ef migrations add` diffs the *entire* model, not just
            // this migration's own entities. It also picked up a pre-existing,
            // already-pending Permissions/SystemModules/RolePermissions seed
            // drift (Notifications/System Settings/Email Settings permissions)
            // that has nothing to do with these 5 new modules — deliberately
            // left OUT of this migration (and its Down()) so this migration only
            // does what it says: create the 5 new modules' tables. See the
            // handoff notes for the separate, pre-existing issue that needs its
            // own migration (the live DB already has a conflicting Permissions
            // row at Id 28, so that data can't just be re-inserted as-is either).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalPharmacyInterventions");

            migrationBuilder.DropTable(
                name: "CpdActivities");

            migrationBuilder.DropTable(
                name: "QualityProjectCycles");

            migrationBuilder.DropTable(
                name: "QualityProjects");

            migrationBuilder.DropTable(
                name: "ResearcherProfiles");

            migrationBuilder.DropTable(
                name: "ResearchPublications");

            migrationBuilder.DropTable(
                name: "StudentFeedbacks");
        }
    }
}
