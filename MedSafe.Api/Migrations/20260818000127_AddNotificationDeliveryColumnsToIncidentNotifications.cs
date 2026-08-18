using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveryColumnsToIncidentNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.IncidentNotifications', 'NotificationMethodId') IS NULL
    ALTER TABLE dbo.IncidentNotifications ADD NotificationMethodId INT NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.IncidentNotifications', 'Status') IS NULL
    ALTER TABLE dbo.IncidentNotifications ADD Status NVARCHAR(50) NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.IncidentNotifications', 'SentAt') IS NULL
    ALTER TABLE dbo.IncidentNotifications ADD SentAt DATETIME2 NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.IncidentNotifications', 'Notes') IS NULL
    ALTER TABLE dbo.IncidentNotifications ADD Notes NVARCHAR(1000) NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentNotifications_NotificationMethod')
    ALTER TABLE dbo.IncidentNotifications ADD CONSTRAINT FK_IncidentNotifications_NotificationMethod FOREIGN KEY (NotificationMethodId) REFERENCES dbo.NotificationMethod(Id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentNotifications_NotificationMethod')
    ALTER TABLE dbo.IncidentNotifications DROP CONSTRAINT FK_IncidentNotifications_NotificationMethod;
IF COL_LENGTH('dbo.IncidentNotifications', 'NotificationMethodId') IS NOT NULL
    ALTER TABLE dbo.IncidentNotifications DROP COLUMN NotificationMethodId;
IF COL_LENGTH('dbo.IncidentNotifications', 'Status') IS NOT NULL
    ALTER TABLE dbo.IncidentNotifications DROP COLUMN Status;
IF COL_LENGTH('dbo.IncidentNotifications', 'SentAt') IS NOT NULL
    ALTER TABLE dbo.IncidentNotifications DROP COLUMN SentAt;
IF COL_LENGTH('dbo.IncidentNotifications', 'Notes') IS NOT NULL
    ALTER TABLE dbo.IncidentNotifications DROP COLUMN Notes;
");
        }
    }
}
