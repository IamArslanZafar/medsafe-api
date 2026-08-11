using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDropdownConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DropdownDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DropdownDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DropdownValues",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DropdownDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DropdownValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DropdownValues_DropdownDefinitions_DropdownDefinitionId",
                        column: x => x.DropdownDefinitionId,
                        principalTable: "DropdownDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DropdownDefinitions_Key",
                table: "DropdownDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DropdownValues_DropdownDefinitionId_Value",
                table: "DropdownValues",
                columns: new[] { "DropdownDefinitionId", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DropdownValues");

            migrationBuilder.DropTable(
                name: "DropdownDefinitions");
        }
    }
}
