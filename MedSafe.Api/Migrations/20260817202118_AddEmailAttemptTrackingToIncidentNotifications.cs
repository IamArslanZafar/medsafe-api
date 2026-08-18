using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAttemptTrackingToIncidentNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentNotifications', 'EmailAttemptCount') IS NULL
BEGIN
    ALTER TABLE dbo.IncidentNotifications ADD EmailAttemptCount INT NOT NULL CONSTRAINT DF_IncidentNotifications_EmailAttemptCount DEFAULT (0);
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentNotifications', 'LastEmailAttemptAt') IS NULL
BEGIN
    ALTER TABLE dbo.IncidentNotifications ADD LastEmailAttemptAt DATETIME2 NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentNotifications', 'LastEmailAttemptAt') IS NOT NULL
BEGIN
    ALTER TABLE dbo.IncidentNotifications DROP COLUMN LastEmailAttemptAt;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentNotifications', 'EmailAttemptCount') IS NOT NULL
BEGIN
    ALTER TABLE dbo.IncidentNotifications DROP COLUMN EmailAttemptCount;
END
");
        }
    }
}
