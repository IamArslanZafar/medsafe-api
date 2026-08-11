using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Position' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Position
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Position PRIMARY KEY,
        ProfessionId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Position_IsActive DEFAULT (1),
        DisplayOrder INT NOT NULL CONSTRAINT DF_Position_DisplayOrder DEFAULT (0),
        CreatedBy INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Position_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME2 NULL,
        CONSTRAINT FK_Position_Profession FOREIGN KEY (ProfessionId) REFERENCES dbo.Profession (Id),
        CONSTRAINT UQ_Position_Profession_Name UNIQUE (ProfessionId, Name)
    );
    CREATE INDEX IX_Position_ProfessionId ON dbo.Position (ProfessionId);
END
");

            // Resolved by Profession.Name rather than hardcoded ids — local dev DBs may not
            // have Profession seeded (or seeded with different ids) yet, so this silently
            // seeds nothing there instead of failing the migration on a missing FK target.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Position)
BEGIN
    INSERT INTO dbo.Position (ProfessionId, Name, DisplayOrder, CreatedBy)
    SELECT pr.Id, v.Name, v.DisplayOrder, 1
    FROM (VALUES
        ('Nurse', 'Staff Nurse', 1),
        ('Nurse', 'Charge Nurse', 2),
        ('Nurse', 'Head Nurse', 3),
        ('Nurse', 'Nurse Manager', 4),
        ('Physician', 'Resident', 1),
        ('Physician', 'Specialist', 2),
        ('Physician', 'Consultant', 3),
        ('Pharmacist', 'Staff Pharmacist', 1),
        ('Pharmacist', 'Clinical Pharmacist', 2),
        ('Pharmacist', 'Pharmacy Manager', 3)
    ) AS v(ProfessionName, Name, DisplayOrder)
    JOIN dbo.Profession pr ON pr.Name = v.ProfessionName;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Position') IS NOT NULL DROP TABLE dbo.Position;
");
        }
    }
}
