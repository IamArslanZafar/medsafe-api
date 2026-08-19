using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertTriggerHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AlertTriggerHistory' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AlertTriggerHistory
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        AlertTriggerNumber NVARCHAR(40) NOT NULL,
        AlertRuleId INT NOT NULL,
        IncidentReportId INT NOT NULL,
        UrgencyId INT NULL,
        TriggerSource NVARCHAR(50) NOT NULL,
        ConditionSummary NVARCHAR(1000) NULL,
        MatchedConditionSnapshot NVARCHAR(MAX) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_AlertTriggerHistory_Status DEFAULT ('OPEN'),
        DedupeKey NVARCHAR(250) NOT NULL,
        TriggeredAt DATETIME2 NOT NULL,
        AcknowledgedByUserId INT NULL,
        AcknowledgedAt DATETIME2 NULL,
        ResolvedByUserId INT NULL,
        ResolvedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AlertTriggerHistory_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AlertTriggerHistory PRIMARY KEY (Id),
        CONSTRAINT FK_AlertTriggerHistory_AlertRule FOREIGN KEY (AlertRuleId) REFERENCES dbo.AlertRules (Id),
        CONSTRAINT FK_AlertTriggerHistory_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports (Id),
        CONSTRAINT FK_AlertTriggerHistory_Urgency FOREIGN KEY (UrgencyId) REFERENCES dbo.NotificationUrgency (Id),
        CONSTRAINT FK_AlertTriggerHistory_AcknowledgedBy FOREIGN KEY (AcknowledgedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_AlertTriggerHistory_ResolvedBy FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users (Id)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AlertTriggerHistory_AlertTriggerNumber' AND object_id = OBJECT_ID('dbo.AlertTriggerHistory'))
    CREATE UNIQUE INDEX UX_AlertTriggerHistory_AlertTriggerNumber ON dbo.AlertTriggerHistory (AlertTriggerNumber);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AlertTriggerHistory_DedupeKey' AND object_id = OBJECT_ID('dbo.AlertTriggerHistory'))
    CREATE UNIQUE INDEX UX_AlertTriggerHistory_DedupeKey ON dbo.AlertTriggerHistory (DedupeKey);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AlertTriggerHistory_TriggeredAt' AND object_id = OBJECT_ID('dbo.AlertTriggerHistory'))
    CREATE INDEX IX_AlertTriggerHistory_TriggeredAt ON dbo.AlertTriggerHistory (TriggeredAt);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AlertTriggerHistory_Status_TriggeredAt' AND object_id = OBJECT_ID('dbo.AlertTriggerHistory'))
    CREATE INDEX IX_AlertTriggerHistory_Status_TriggeredAt ON dbo.AlertTriggerHistory (Status, TriggeredAt);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AlertTriggerHistory_AlertRuleId_TriggeredAt' AND object_id = OBJECT_ID('dbo.AlertTriggerHistory'))
    CREATE INDEX IX_AlertTriggerHistory_AlertRuleId_TriggeredAt ON dbo.AlertTriggerHistory (AlertRuleId, TriggeredAt);
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.IncidentNotifications', 'AlertTriggerId') IS NULL
    ALTER TABLE dbo.IncidentNotifications ADD AlertTriggerId BIGINT NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentNotifications_AlertTriggerHistory')
    ALTER TABLE dbo.IncidentNotifications
    ADD CONSTRAINT FK_IncidentNotifications_AlertTriggerHistory
    FOREIGN KEY (AlertTriggerId) REFERENCES dbo.AlertTriggerHistory (Id);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentNotifications_AlertTriggerId' AND object_id = OBJECT_ID('dbo.IncidentNotifications'))
    CREATE INDEX IX_IncidentNotifications_AlertTriggerId ON dbo.IncidentNotifications (AlertTriggerId);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentNotifications_AlertTriggerHistory')
    ALTER TABLE dbo.IncidentNotifications DROP CONSTRAINT FK_IncidentNotifications_AlertTriggerHistory;
IF COL_LENGTH('dbo.IncidentNotifications', 'AlertTriggerId') IS NOT NULL
    ALTER TABLE dbo.IncidentNotifications DROP COLUMN AlertTriggerId;
IF OBJECT_ID('dbo.AlertTriggerHistory') IS NOT NULL DROP TABLE dbo.AlertTriggerHistory;
");
        }
    }
}
