using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LunchFate.Migrations
{
    /// <inheritdoc />
    public partial class AddShopNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppShops_Name",
                table: "AppShops");

            migrationBuilder.CreateIndex(
                name: "IX_AppShops_Name",
                table: "AppShops",
                column: "Name",
                unique: true,
                filter: "IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppShops_Name",
                table: "AppShops");

            migrationBuilder.CreateIndex(
                name: "IX_AppShops_Name",
                table: "AppShops",
                column: "Name");
        }
    }
}
