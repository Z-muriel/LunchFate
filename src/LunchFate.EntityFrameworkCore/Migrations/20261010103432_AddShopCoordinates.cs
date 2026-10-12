using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LunchFate.Migrations
{
    /// <inheritdoc />
    public partial class AddShopCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "AppShops",
                type: "float(10)",
                precision: 10,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "AppShops",
                type: "float(10)",
                precision: 10,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AppShops");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AppShops");
        }
    }
}
