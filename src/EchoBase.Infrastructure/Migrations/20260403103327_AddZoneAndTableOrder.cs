using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneAndTableOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "DockZones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "DockTables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "DockZones");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "DockTables");
        }
    }
}
