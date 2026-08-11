using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupFksToIncidentReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These columns/constraints may already exist (added directly against the
            // live DB outside this project's migrations) — guard every step.
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'FormulationId') IS NULL
                    ALTER TABLE [dbo].[IncidentReports] ADD [FormulationId] int NULL;
            ");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'FrequencyId') IS NULL
                    ALTER TABLE [dbo].[IncidentReports] ADD [FrequencyId] int NULL;
            ");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'PatientOutcomeId') IS NULL
                    ALTER TABLE [dbo].[IncidentReports] ADD [PatientOutcomeId] int NULL;
            ");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'StageOfProcessId') IS NULL
                    ALTER TABLE [dbo].[IncidentReports] ADD [StageOfProcessId] int NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_FormulationId')
                    CREATE INDEX [IX_IncidentReports_FormulationId] ON [dbo].[IncidentReports] ([FormulationId]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_FrequencyId')
                    CREATE INDEX [IX_IncidentReports_FrequencyId] ON [dbo].[IncidentReports] ([FrequencyId]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_PatientOutcomeId')
                    CREATE INDEX [IX_IncidentReports_PatientOutcomeId] ON [dbo].[IncidentReports] ([PatientOutcomeId]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_StageOfProcessId')
                    CREATE INDEX [IX_IncidentReports_StageOfProcessId] ON [dbo].[IncidentReports] ([StageOfProcessId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_Formulation')
                    ALTER TABLE [dbo].[IncidentReports] ADD CONSTRAINT [FK_IncidentReports_Formulation]
                        FOREIGN KEY ([FormulationId]) REFERENCES [dbo].[Formulation] ([Id]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_Frequency')
                    ALTER TABLE [dbo].[IncidentReports] ADD CONSTRAINT [FK_IncidentReports_Frequency]
                        FOREIGN KEY ([FrequencyId]) REFERENCES [dbo].[Frequency] ([Id]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_PatientOutcome')
                    ALTER TABLE [dbo].[IncidentReports] ADD CONSTRAINT [FK_IncidentReports_PatientOutcome]
                        FOREIGN KEY ([PatientOutcomeId]) REFERENCES [dbo].[PatientOutcome] ([Id]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_StageOfProcess')
                    ALTER TABLE [dbo].[IncidentReports] ADD CONSTRAINT [FK_IncidentReports_StageOfProcess]
                        FOREIGN KEY ([StageOfProcessId]) REFERENCES [dbo].[StageOfProcess] ([Id]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_Formulation')
                    ALTER TABLE [dbo].[IncidentReports] DROP CONSTRAINT [FK_IncidentReports_Formulation];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_Frequency')
                    ALTER TABLE [dbo].[IncidentReports] DROP CONSTRAINT [FK_IncidentReports_Frequency];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_PatientOutcome')
                    ALTER TABLE [dbo].[IncidentReports] DROP CONSTRAINT [FK_IncidentReports_PatientOutcome];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IncidentReports_StageOfProcess')
                    ALTER TABLE [dbo].[IncidentReports] DROP CONSTRAINT [FK_IncidentReports_StageOfProcess];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_FormulationId')
                    DROP INDEX [IX_IncidentReports_FormulationId] ON [dbo].[IncidentReports];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_FrequencyId')
                    DROP INDEX [IX_IncidentReports_FrequencyId] ON [dbo].[IncidentReports];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_PatientOutcomeId')
                    DROP INDEX [IX_IncidentReports_PatientOutcomeId] ON [dbo].[IncidentReports];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IncidentReports_StageOfProcessId')
                    DROP INDEX [IX_IncidentReports_StageOfProcessId] ON [dbo].[IncidentReports];
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'FormulationId') IS NOT NULL
                    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [FormulationId];
            ");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'FrequencyId') IS NOT NULL
                    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [FrequencyId];
            ");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'PatientOutcomeId') IS NOT NULL
                    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [PatientOutcomeId];
            ");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.IncidentReports', 'StageOfProcessId') IS NOT NULL
                    ALTER TABLE [dbo].[IncidentReports] DROP COLUMN [StageOfProcessId];
            ");
        }
    }
}
