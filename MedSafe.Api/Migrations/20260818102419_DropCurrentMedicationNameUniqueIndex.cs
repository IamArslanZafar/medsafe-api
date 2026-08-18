using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class DropCurrentMedicationNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_CurrentMedication_Name' AND parent_object_id = OBJECT_ID('dbo.CurrentMedication'))
    ALTER TABLE dbo.CurrentMedication DROP CONSTRAINT UQ_CurrentMedication_Name;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_CurrentMedication_Name' AND parent_object_id = OBJECT_ID('dbo.CurrentMedication'))
    ALTER TABLE dbo.CurrentMedication ADD CONSTRAINT UQ_CurrentMedication_Name UNIQUE (Name);
");
        }
    }
}
