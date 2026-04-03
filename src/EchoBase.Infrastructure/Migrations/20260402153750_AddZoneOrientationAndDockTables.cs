using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneOrientationAndDockTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Orientation",
                table: "DockZones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DockTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TableKey = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Locator = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DockZoneId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DockTables_DockZones_DockZoneId",
                        column: x => x.DockZoneId,
                        principalTable: "DockZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DockTables_DockZoneId_TableKey",
                table: "DockTables",
                columns: new[] { "DockZoneId", "TableKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DockTables");

            migrationBuilder.DropColumn(
                name: "Orientation",
                table: "DockZones");
        }
    }
}
