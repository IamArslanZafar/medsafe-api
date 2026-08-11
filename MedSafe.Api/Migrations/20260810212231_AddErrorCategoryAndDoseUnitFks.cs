using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorCategoryAndDoseUnitFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These columns/constraints may already exist (added directly against the
            // live DB outside this project's migrations) — guard every step.
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'DoseUnitId') IS NULL
                    ALTER TABLE [dbo].[IncidentReports] ADD [DoseUnitId] int NULL;
            ");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'ErrorCategoryId') IS NULL
                    ALTER TABLE [dbo].[IncidentReports] ADD [ErrorCategoryId] int NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_DoseUnitId')
                    CREATE INDEX [IX_IncidentReports_DoseUnitId] ON [dbo].[IncidentReports] ([DoseUnitId]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_ErrorCategoryId')
                    CREATE INDEX [IX_IncidentReports_ErrorCategoryId] ON [dbo].[IncidentReports] ([ErrorCategoryId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_DoseUnit')
                    ALTER TABLE [dbo].[IncidentReports] ADD CONSTRAINT [FK_IncidentReports_DoseUnit]
                        FOREIGN KEY ([DoseUnitId]) REFERENCES [dbo].[DoseUnit] ([Id]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_ErrorCategory')
                    ALTER TABLE [dbo].[IncidentReports] ADD CONSTRAINT [FK_IncidentReports_ErrorCategory]
                        FOREIGN KEY ([ErrorCategoryId]) REFERENCES [dbo].[ErrorCategory] ([Id]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_DoseUnit')
                    ALTER TABLE [dbo].[IncidentReports] DROP CONSTRAINT [FK_IncidentReports_DoseUnit];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_ErrorCategory')
                    ALTER TABLE [dbo].[IncidentReports] DROP CONSTRAINT [FK_IncidentReports_ErrorCategory];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_DoseUnitId')
                    DROP INDEX [IX_IncidentReports_DoseUnitId] ON [dbo].[IncidentReports];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_ErrorCategoryId')
                    DROP INDEX [IX_IncidentReports_ErrorCategoryId] ON [dbo].[IncidentReports];
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'DoseUnitId') IS NOT NULL
                    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [DoseUnitId];
            ");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'ErrorCategoryId') IS NOT NULL
                    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [ErrorCategoryId];
            ");
        }
    }
}
