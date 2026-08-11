using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationRecipientType' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.NotificationRecipientType
    (
        Id INT IDENTITY(1,1) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_NotificationRecipientType_IsActive DEFAULT (1),
        DisplayOrder INT NULL,
        CreatedBy INT NOT NULL CONSTRAINT DF_NotificationRecipientType_CreatedBy DEFAULT (1),
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_NotificationRecipientType_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedAt DATETIME2(7) NULL,
        CONSTRAINT PK_NotificationRecipientType PRIMARY KEY (Id),
        CONSTRAINT UQ_NotificationRecipientType_Code UNIQUE (Code),
        CONSTRAINT UQ_NotificationRecipientType_Name UNIQUE (Name)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.NotificationRecipientType)
BEGIN
    INSERT INTO dbo.NotificationRecipientType (Code, Name, Description, DisplayOrder) VALUES
    ('PHYSICIAN', 'Physician', 'Physician responsible for reviewing or treating the patient', 1),
    ('SAFETY_OFFICER', 'Safety Officer', 'Patient safety or medication safety officer', 2),
    ('ADMINISTRATOR', 'Administrator', 'System or organizational administrator', 3),
    ('PRESCRIBER', 'Prescriber', 'Healthcare professional who prescribed the medication', 4),
    ('PHARMACY_LEAD', 'Pharmacy Lead', 'Responsible pharmacy lead or pharmacy manager', 5),
    ('ASSIGNED_REVIEWER', 'Assigned Reviewer', 'Reviewer assigned to the incident report', 6),
    ('NURSE_MANAGER', 'Nurse Manager', 'Nurse manager or nursing supervisor', 7),
    ('OTHER', 'Other', 'Other notification recipient type', 99);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationMethod' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.NotificationMethod
    (
        Id INT IDENTITY(1,1) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_NotificationMethod_IsActive DEFAULT (1),
        DisplayOrder INT NULL,
        CreatedBy INT NOT NULL CONSTRAINT DF_NotificationMethod_CreatedBy DEFAULT (1),
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_NotificationMethod_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedAt DATETIME2(7) NULL,
        CONSTRAINT PK_NotificationMethod PRIMARY KEY (Id),
        CONSTRAINT UQ_NotificationMethod_Code UNIQUE (Code),
        CONSTRAINT UQ_NotificationMethod_Name UNIQUE (Name)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.NotificationMethod)
BEGIN
    INSERT INTO dbo.NotificationMethod (Code, Name, Description, DisplayOrder) VALUES
    ('SYSTEM', 'System Notification', 'Notification shown inside the application', 1),
    ('EMAIL', 'Email', 'Notification sent through email', 2),
    ('SMS', 'SMS', 'Notification sent through SMS', 3),
    ('WEBHOOK', 'Webhook', 'Notification sent to an external system through webhook', 4),
    ('PHONE', 'Phone', 'Recipient was manually informed by phone', 5),
    ('MANUAL', 'Manual', 'Notification was recorded manually without automated delivery', 6);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationStatus' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.NotificationStatus
    (
        Id INT IDENTITY(1,1) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_NotificationStatus_IsActive DEFAULT (1),
        DisplayOrder INT NULL,
        CreatedBy INT NOT NULL CONSTRAINT DF_NotificationStatus_CreatedBy DEFAULT (1),
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_NotificationStatus_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedAt DATETIME2(7) NULL,
        CONSTRAINT PK_NotificationStatus PRIMARY KEY (Id),
        CONSTRAINT UQ_NotificationStatus_Code UNIQUE (Code),
        CONSTRAINT UQ_NotificationStatus_Name UNIQUE (Name)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.NotificationStatus)
BEGIN
    INSERT INTO dbo.NotificationStatus (Code, Name, Description, DisplayOrder) VALUES
    ('PENDING', 'Pending', 'Notification has been created but not sent yet', 1),
    ('QUEUED', 'Queued', 'Notification is waiting for delivery processing', 2),
    ('SENT', 'Sent', 'Notification was successfully sent', 3),
    ('DELIVERED', 'Delivered', 'Delivery provider confirmed delivery where supported', 4),
    ('FAILED', 'Failed', 'Notification delivery failed', 5),
    ('READ', 'Read', 'Recipient has viewed the notification', 6),
    ('ACKNOWLEDGED', 'Acknowledged', 'Recipient acknowledged the notification', 7),
    ('CANCELLED', 'Cancelled', 'Notification was cancelled before delivery', 8);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationUrgency' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.NotificationUrgency
    (
        Id INT IDENTITY(1,1) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_NotificationUrgency_IsActive DEFAULT (1),
        DisplayOrder INT NULL,
        CreatedBy INT NOT NULL CONSTRAINT DF_NotificationUrgency_CreatedBy DEFAULT (1),
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_NotificationUrgency_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedAt DATETIME2(7) NULL,
        CONSTRAINT PK_NotificationUrgency PRIMARY KEY (Id),
        CONSTRAINT UQ_NotificationUrgency_Code UNIQUE (Code),
        CONSTRAINT UQ_NotificationUrgency_Name UNIQUE (Name)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.NotificationUrgency)
BEGIN
    INSERT INTO dbo.NotificationUrgency (Code, Name, Description, DisplayOrder) VALUES
    ('IMMEDIATE', 'Immediate', 'Notification should be sent immediately', 1),
    ('ESCALATED', 'Immediate / Escalated', 'Critical notification requiring immediate escalation', 2),
    ('REMINDER', 'Reminder', 'Follow-up reminder notification', 3),
    ('DAILY_DIGEST', 'Daily Digest', 'Notification grouped into a daily summary', 4),
    ('NORMAL', 'Normal', 'Standard notification without special urgency', 5);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentNotifications' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.IncidentNotifications
    (
        Id INT IDENTITY(1,1) NOT NULL,
        IncidentReportId INT NOT NULL,
        NotificationTypeId INT NULL,
        RecipientUserId INT NULL,
        NotificationMethodId INT NULL,
        IsAutomatic BIT NOT NULL DEFAULT (1),
        SentAt DATETIME2 NULL,
        Status NVARCHAR(50) NULL,
        Notes NVARCHAR(1000) NULL,
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_IncidentNotifications PRIMARY KEY (Id),
        CONSTRAINT FK_IncidentNotifications_IncidentReport FOREIGN KEY (IncidentReportId) REFERENCES dbo.IncidentReports (Id),
        CONSTRAINT FK_IncidentNotifications_NotificationRecipientType FOREIGN KEY (NotificationTypeId) REFERENCES dbo.NotificationRecipientType (Id),
        CONSTRAINT FK_IncidentNotifications_NotificationMethod FOREIGN KEY (NotificationMethodId) REFERENCES dbo.NotificationMethod (Id)
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.IncidentNotifications') IS NOT NULL DROP TABLE dbo.IncidentNotifications;
IF OBJECT_ID('dbo.NotificationUrgency') IS NOT NULL DROP TABLE dbo.NotificationUrgency;
IF OBJECT_ID('dbo.NotificationStatus') IS NOT NULL DROP TABLE dbo.NotificationStatus;
IF OBJECT_ID('dbo.NotificationMethod') IS NOT NULL DROP TABLE dbo.NotificationMethod;
IF OBJECT_ID('dbo.NotificationRecipientType') IS NOT NULL DROP TABLE dbo.NotificationRecipientType;
");
        }
    }
}
