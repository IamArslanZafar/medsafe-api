using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Users', 'ProfessionId') IS NULL
    ALTER TABLE [dbo].[Users] ADD [ProfessionId] int NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_ProfessionId')
    CREATE INDEX [IX_Users_ProfessionId] ON [dbo].[Users] ([ProfessionId]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Profession')
    ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_Users_Profession]
        FOREIGN KEY ([ProfessionId]) REFERENCES [dbo].[Profession] ([Id]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Profession')
    ALTER TABLE [dbo].[Users] DROP CONSTRAINT [FK_Users_Profession];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_ProfessionId')
    DROP INDEX [IX_Users_ProfessionId] ON [dbo].[Users];
");
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Users', 'ProfessionId') IS NOT NULL
    ALTER TABLE [dbo].[Users] DROP COLUMN [ProfessionId];
");
        }
    }
}
