using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAllergyAndCurrentMedicationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Allergy' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Allergy
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Allergy PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Allergy_IsActive DEFAULT (1),
        DisplayOrder INT NOT NULL CONSTRAINT DF_Allergy_DisplayOrder DEFAULT (0),
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Allergy_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME2 NULL,
        CONSTRAINT UQ_Allergy_Name UNIQUE (Name)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Allergy)
BEGIN
    INSERT INTO dbo.Allergy (Name, DisplayOrder, CreatedBy) VALUES
    ('Penicillin', 1, 1),
    ('Aspirin', 2, 1),
    ('Sulfa Drugs', 3, 1),
    ('NSAIDs', 4, 1),
    ('Latex', 5, 1),
    ('Iodine / Contrast Dye', 6, 1),
    ('Peanuts', 7, 1),
    ('Shellfish', 8, 1),
    ('Eggs', 9, 1),
    ('Codeine', 10, 1);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CurrentMedication' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CurrentMedication
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CurrentMedication PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CurrentMedication_IsActive DEFAULT (1),
        DisplayOrder INT NOT NULL CONSTRAINT DF_CurrentMedication_DisplayOrder DEFAULT (0),
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_CurrentMedication_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME2 NULL,
        CONSTRAINT UQ_CurrentMedication_Name UNIQUE (Name)
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.CurrentMedication)
BEGIN
    INSERT INTO dbo.CurrentMedication (Name, DisplayOrder, CreatedBy) VALUES
    ('Warfarin 5mg', 1, 1),
    ('Furosemide 40mg', 2, 1),
    ('Metformin 500mg', 3, 1),
    ('Atorvastatin 20mg', 4, 1),
    ('Amlodipine 5mg', 5, 1),
    ('Aspirin 75mg', 6, 1),
    ('Insulin Glargine 10 units', 7, 1),
    ('Omeprazole 20mg', 8, 1),
    ('Levothyroxine 50mcg', 9, 1),
    ('Paracetamol 500mg', 10, 1);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.CurrentMedication') IS NOT NULL DROP TABLE dbo.CurrentMedication;
IF OBJECT_ID('dbo.Allergy') IS NOT NULL DROP TABLE dbo.Allergy;
");
        }
    }
}
