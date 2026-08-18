using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDoseFieldsToCurrentMedication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.CurrentMedication', 'DoseValue') IS NULL
    ALTER TABLE dbo.CurrentMedication ADD DoseValue DECIMAL(18,4) NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.CurrentMedication', 'DoseUnitId') IS NULL
    ALTER TABLE dbo.CurrentMedication ADD DoseUnitId INT NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.CurrentMedication', 'RouteId') IS NULL
    ALTER TABLE dbo.CurrentMedication ADD RouteId INT NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.CurrentMedication', 'FrequencyId') IS NULL
    ALTER TABLE dbo.CurrentMedication ADD FrequencyId INT NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH('dbo.CurrentMedication', 'FormulationId') IS NULL
    ALTER TABLE dbo.CurrentMedication ADD FormulationId INT NULL;
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CurrentMedication_DoseUnit')
    ALTER TABLE dbo.CurrentMedication ADD CONSTRAINT FK_CurrentMedication_DoseUnit FOREIGN KEY (DoseUnitId) REFERENCES dbo.DoseUnit(Id);
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CurrentMedication_Route')
    ALTER TABLE dbo.CurrentMedication ADD CONSTRAINT FK_CurrentMedication_Route FOREIGN KEY (RouteId) REFERENCES dbo.Route(Id);
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CurrentMedication_Frequency')
    ALTER TABLE dbo.CurrentMedication ADD CONSTRAINT FK_CurrentMedication_Frequency FOREIGN KEY (FrequencyId) REFERENCES dbo.Frequency(Id);
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CurrentMedication_Formulation')
    ALTER TABLE dbo.CurrentMedication ADD CONSTRAINT FK_CurrentMedication_Formulation FOREIGN KEY (FormulationId) REFERENCES dbo.Formulation(Id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CurrentMedication_DoseUnit')
    ALTER TABLE dbo.CurrentMedication DROP CONSTRAINT FK_CurrentMedication_DoseUnit;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CurrentMedication_Route')
    ALTER TABLE dbo.CurrentMedication DROP CONSTRAINT FK_CurrentMedication_Route;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CurrentMedication_Frequency')
    ALTER TABLE dbo.CurrentMedication DROP CONSTRAINT FK_CurrentMedication_Frequency;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CurrentMedication_Formulation')
    ALTER TABLE dbo.CurrentMedication DROP CONSTRAINT FK_CurrentMedication_Formulation;
IF COL_LENGTH('dbo.CurrentMedication', 'DoseValue') IS NOT NULL
    ALTER TABLE dbo.CurrentMedication DROP COLUMN DoseValue;
IF COL_LENGTH('dbo.CurrentMedication', 'DoseUnitId') IS NOT NULL
    ALTER TABLE dbo.CurrentMedication DROP COLUMN DoseUnitId;
IF COL_LENGTH('dbo.CurrentMedication', 'RouteId') IS NOT NULL
    ALTER TABLE dbo.CurrentMedication DROP COLUMN RouteId;
IF COL_LENGTH('dbo.CurrentMedication', 'FrequencyId') IS NOT NULL
    ALTER TABLE dbo.CurrentMedication DROP COLUMN FrequencyId;
IF COL_LENGTH('dbo.CurrentMedication', 'FormulationId') IS NOT NULL
    ALTER TABLE dbo.CurrentMedication DROP COLUMN FormulationId;
");
        }
    }
}
