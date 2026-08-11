using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteIdToIncidentReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RouteId + FK_IncidentReports_Route may already exist (added directly
            // against the live DB outside this project's migrations) — guard every step.
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'RouteId') IS NULL
                    ALTER TABLE [dbo].[IncidentReports] ADD [RouteId] int NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_RouteId')
                    CREATE INDEX [IX_IncidentReports_RouteId] ON [dbo].[IncidentReports] ([RouteId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_Route')
                    ALTER TABLE [dbo].[IncidentReports] ADD CONSTRAINT [FK_IncidentReports_Route]
                        FOREIGN KEY ([RouteId]) REFERENCES [dbo].[Route] ([Id]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_Route')
                    ALTER TABLE [dbo].[IncidentReports] DROP CONSTRAINT [FK_IncidentReports_Route];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_RouteId')
                    DROP INDEX [IX_IncidentReports_RouteId] ON [dbo].[IncidentReports];
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'RouteId') IS NOT NULL
                    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [RouteId];
            ");
        }
    }
}
