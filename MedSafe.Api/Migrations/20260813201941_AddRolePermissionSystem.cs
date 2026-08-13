using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PermissionTag = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    SystemModuleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Parent",
                        column: x => x.ParentId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Permissions_SystemModule",
                        column: x => x.SystemModuleId,
                        principalTable: "SystemModules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permission",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Role",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Frontline clinical staff who submit incident reports", "Nurse" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Clinical reviewer who signs off on incident reports", "Physician" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Full administrative access", "Admin" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pharmacy staff", "Pharmacist" }
                });

            migrationBuilder.InsertData(
                table: "SystemModules",
                columns: new[] { "Id", "Description", "DisplayOrder", "Name" },
                values: new object[,]
                {
                    { 1, "Submitting and viewing incident reports", 1, "Incident Reports" },
                    { 2, "Reviewing and signing off on reports", 2, "Clinical Review" },
                    { 3, "Configuring notification/alert rules", 3, "Alert Rules" },
                    { 4, "Managing dropdown/lookup configuration data", 4, "Configurations" },
                    { 5, "Managing user accounts and roles", 5, "User Management" },
                    { 6, "Submitting and reviewing app feedback", 6, "Feedback" },
                    { 7, "Viewing the HIPAA audit trail", 7, "Audit Log" },
                    { 8, "Viewing analytics dashboard", 8, "Dashboard" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Name", "ParentId", "PermissionTag", "SystemModuleId" },
                values: new object[,]
                {
                    { 1, "Incident Reports", null, "incident_reports", 1 },
                    { 5, "Clinical Review", null, "clinical_review", 2 },
                    { 8, "Alert Rules", null, "alert_rules", 3 },
                    { 11, "Configurations", null, "configurations", 4 },
                    { 13, "User Management", null, "user_management", 5 },
                    { 15, "Feedback", null, "feedback", 6 },
                    { 18, "Audit Log", null, "audit_log", 7 },
                    { 20, "Dashboard", null, "dashboard", 8 },
                    { 2, "Submit Report", 1, "incident_reports.submit", 1 },
                    { 3, "View All Reports", 1, "incident_reports.view_all", 1 },
                    { 4, "Export Reports", 1, "incident_reports.export", 1 },
                    { 6, "Start Review", 5, "clinical_review.start", 2 },
                    { 7, "Sign Off Review", 5, "clinical_review.sign_off", 2 },
                    { 9, "View Alert Rules", 8, "alert_rules.view", 3 },
                    { 10, "Manage Alert Rules", 8, "alert_rules.manage", 3 },
                    { 12, "Manage Configurations", 11, "configurations.manage", 4 },
                    { 14, "Manage Users", 13, "user_management.manage", 5 },
                    { 16, "Submit Feedback", 15, "feedback.submit", 6 },
                    { 17, "Review Feedback", 15, "feedback.review", 6 },
                    { 19, "View Audit Log", 18, "audit_log.view", 7 },
                    { 21, "View Dashboard", 20, "dashboard.view", 8 }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 15, 1 },
                    { 20, 1 },
                    { 1, 2 },
                    { 5, 2 },
                    { 20, 2 },
                    { 1, 3 },
                    { 5, 3 },
                    { 8, 3 },
                    { 11, 3 },
                    { 13, 3 },
                    { 15, 3 },
                    { 18, 3 },
                    { 20, 3 },
                    { 1, 4 },
                    { 20, 4 },
                    { 2, 1 },
                    { 16, 1 },
                    { 21, 1 },
                    { 3, 2 },
                    { 6, 2 },
                    { 7, 2 },
                    { 21, 2 },
                    { 2, 3 },
                    { 3, 3 },
                    { 4, 3 },
                    { 6, 3 },
                    { 7, 3 },
                    { 9, 3 },
                    { 10, 3 },
                    { 12, 3 },
                    { 14, 3 },
                    { 16, 3 },
                    { 17, 3 },
                    { 19, 3 },
                    { 21, 3 },
                    { 2, 4 },
                    { 21, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ParentId",
                table: "Permissions",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionTag",
                table: "Permissions",
                column: "PermissionTag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_SystemModuleId",
                table: "Permissions",
                column: "SystemModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemModules_Name",
                table: "SystemModules",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Role",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Role",
                table: "Users");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "SystemModules");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");
        }
    }
}
