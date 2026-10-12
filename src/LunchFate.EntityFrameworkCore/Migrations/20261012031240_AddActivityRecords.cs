using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LunchFate.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityRecords : Migration
    {
        /// <summary>创建抽取及门店添加记录表和查询索引，不修改现有门店数据。</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppDrawRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecordTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsTemporary = table.Column<bool>(type: "bit", nullable: false),
                    PoiId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDrawRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppShopCreationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecordTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppShopCreationRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppDrawRecords_TenantId_RecordTime",
                table: "AppDrawRecords",
                columns: new[] { "TenantId", "RecordTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppDrawRecords_TenantId_UserId_RecordTime",
                table: "AppDrawRecords",
                columns: new[] { "TenantId", "UserId", "RecordTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppShopCreationRecords_TenantId_RecordTime",
                table: "AppShopCreationRecords",
                columns: new[] { "TenantId", "RecordTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppShopCreationRecords_TenantId_UserId_RecordTime",
                table: "AppShopCreationRecords",
                columns: new[] { "TenantId", "UserId", "RecordTime" });
        }

        /// <summary>回滚时删除两类记录表；此操作会丢失记录，不用于正常升级。</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppDrawRecords");

            migrationBuilder.DropTable(
                name: "AppShopCreationRecords");
        }
    }
}
