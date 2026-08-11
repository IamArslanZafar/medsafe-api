using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateLocalLookupTables : Migration
    {
        // These lookup tables already exist on the live DB (created directly via SSMS).
        // Guarded so this migration is a no-op there and only creates them where missing
        // (i.e. local dev SQLEXPRESS), keeping both environments in schema parity.
        private static readonly string[] StandardLookupTables =
        {
            "Route", "PatientOutcome", "StageOfProcess", "SeriousnessCriterion",
            "Formulation", "Frequency", "ErrorCategory", "DoseUnit"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContributingFactor' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ContributingFactor
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContributingFactor PRIMARY KEY,
        Code NVARCHAR(100) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ContributingFactor_IsActive DEFAULT (1),
        DisplayOrder INT NOT NULL CONSTRAINT DF_ContributingFactor_DisplayOrder DEFAULT (0),
        CreatedBy INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_ContributingFactor_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME2 NULL
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Profession' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Profession
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Profession PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Profession_IsActive DEFAULT (1),
        DisplayOrder INT NOT NULL CONSTRAINT DF_Profession_DisplayOrder DEFAULT (0),
        CreatedBy INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Profession_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME2 NULL
    );
END
");

            foreach (var table in StandardLookupTables)
            {
                migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{table}' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.{table}
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_{table} PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_{table}_IsActive DEFAULT (1),
        DisplayOrder INT NOT NULL CONSTRAINT DF_{table}_DisplayOrder DEFAULT (0),
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_{table}_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME2 NULL
    );
END
");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "ContributingFactor", "Profession" }.Concat(StandardLookupTables))
            {
                migrationBuilder.Sql($@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = '{table}' AND schema_id = SCHEMA_ID('dbo'))
    DROP TABLE dbo.{table};
");
            }
        }
    }
}
