using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class TopUpContributingFactors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'contributingFactors')
                BEGIN
                    DECLARE @cf INT = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'contributingFactors')

                    MERGE [dbo].[DropdownValues] AS target
                    USING (VALUES
                        ('cf-adcs-issues', N'ADCs (e.g., Pyxis or Omnicell) Issues', 0),
                        ('cf-communication-failure', 'Communication Failure', 1),
                        ('cf-competency-deficit', 'Competency Deficit', 2),
                        ('cf-computer-error', 'Computer Error', 3),
                        ('cf-failure-in-performing-double-check', 'Failure in Performing Double Check', 4),
                        ('cf-failure-to-adhere-to-work-procedures', 'Failure to Adhere to Work Procedures', 5),
                        ('cf-fatigue-lack-of-sleep', 'Fatigue / Lack of Sleep', 6),
                        ('cf-frequent-interruption-and-distractions', 'Frequent Interruption and Distractions', 7),
                        ('cf-illegible-handwriting', 'Illegible Handwriting', 8),
                        ('cf-incorrect-missing-patient-information', 'Incorrect / Missing Patient Information', 9),
                        ('cf-inexperienced-personnel', 'Inexperienced Personnel', 10),
                        ('cf-lighting-issues', 'Lighting Issues', 11),
                        ('cf-look-alike-sound-alike-medication', 'Look Alike - Sound Alike Medication', 12),
                        ('cf-missing-incomplete-instructions', 'Missing / Incomplete Instructions', 13),
                        ('cf-na', 'NA', 14),
                        ('cf-noise-level', 'Noise Level', 15),
                        ('cf-patient-family-education', 'Patient / Family Education', 16),
                        ('cf-peak-hours', 'Peak Hours', 17),
                        ('cf-performance-deficit', 'Performance Deficit', 18),
                        ('cf-policy-procedure-issue', 'Policy/Procedure Issue', 19),
                        ('cf-pump-infusion-issues', 'Pump/Infusion Issues', 20),
                        ('cf-reconciliation-failure', 'Reconciliation Failure', 21),
                        ('cf-refusal-of-medication', 'Refusal of Medication', 22),
                        ('cf-self-administration', 'Self Administration', 23),
                        ('cf-staffing', 'Staffing', 24),
                        ('cf-stress-high-workload', 'Stress (High Volume Workload, etc.)', 25),
                        ('cf-unapproved-abbreviation-use', 'Unapproved Abbreviation Use', 26)
                    ) AS src ([Id], [Value], [SortOrder])
                    ON target.[Id] = src.[Id] AND target.[DropdownDefinitionId] = @cf
                    WHEN MATCHED THEN
                        UPDATE SET target.[SortOrder] = src.[SortOrder]
                    WHEN NOT MATCHED THEN
                        INSERT ([Id], [DropdownDefinitionId], [Value], [SortOrder])
                        VALUES (src.[Id], @cf, src.[Value], src.[SortOrder]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [dbo].[DropdownValues]
                WHERE [DropdownDefinitionId] = (SELECT Id FROM [dbo].[DropdownDefinitions] WHERE [Key] = 'contributingFactors')
                  AND [Id] IN (
                    'cf-adcs-issues','cf-communication-failure','cf-competency-deficit','cf-computer-error',
                    'cf-failure-in-performing-double-check','cf-failure-to-adhere-to-work-procedures',
                    'cf-fatigue-lack-of-sleep','cf-frequent-interruption-and-distractions','cf-illegible-handwriting',
                    'cf-incorrect-missing-patient-information','cf-lighting-issues','cf-look-alike-sound-alike-medication',
                    'cf-missing-incomplete-instructions','cf-na','cf-peak-hours','cf-reconciliation-failure',
                    'cf-staffing','cf-stress-high-workload','cf-unapproved-abbreviation-use'
                  );
            ");
        }
    }
}
