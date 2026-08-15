using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ProfessionId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfessionId_PositionId",
                table: "Users",
                columns: new[] { "ProfessionId", "PositionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ProfessionPosition",
                table: "Users",
                columns: new[] { "ProfessionId", "PositionId" },
                principalTable: "Position",
                principalColumns: new[] { "ProfessionId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ProfessionPosition",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ProfessionId_PositionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfessionId",
                table: "Users",
                column: "ProfessionId");
        }
    }
}
