using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSafeAPI.Migrations
{
    /// <inheritdoc />
    // The Research & Publications module used to be a single page, covered by
    // one permission (research_publications.manage). Now that the frontend
    // split it into two pages — "New Research Work" (the submit form) and
    // "Dashboard" (the list/table) — it gets a second, distinct permission for
    // the Dashboard, mirroring how Clinical Pharmacy Intervention/CPD/Quality
    // Project Tracker already have separate "Submit X" + "View X Dashboard"
    // permissions. The existing permission's Id/Tag are left untouched (only
    // its display Name changes, from "Manage Research Publications" to
    // "Submit Research Work") so no existing role grant or frontend alias
    // breaks — it now specifically covers the submit-form page it always
    // actually gated, next to the new View Dashboard permission alongside it.
    public partial class AddResearchPublicationsDashboardPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 40,
                column: "Name",
                value: "Submit Research Work");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Name", "ParentId", "PermissionTag", "SystemModuleId" },
                values: new object[] { 44, "View Research Publications Dashboard", 39, "research_publications.view_dashboard", 15 });

            // Grant it to whichever role(s) currently hold research_publications.manage
            // (Id 40) — Admin only, per the live RolePermissions table today — so access
            // doesn't narrow for anyone already using the module.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { 44, 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 44, 3 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 40,
                column: "Name",
                value: "Manage Research Publications");
        }
    }
}
