using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDockTableHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Docks_DockZones_DockZoneId",
                table: "Docks");

            migrationBuilder.DropIndex(
                name: "IX_Docks_DockZoneId",
                table: "Docks");

            migrationBuilder.DropColumn(
                name: "DockZoneId",
                table: "Docks");

            migrationBuilder.AddColumn<Guid>(
                name: "DockTableId",
                table: "Docks",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Side",
                table: "Docks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Docks_DockTableId",
                table: "Docks",
                column: "DockTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Docks_DockTables_DockTableId",
                table: "Docks",
                column: "DockTableId",
                principalTable: "DockTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── Migración de datos ───────────────────────────────────────────────────
            // Asigna DockTableId y Side a cada puesto existente según su código.
            // Nostromo (mesa única): N-A* → lado A, N-B* → lado B
            migrationBuilder.Sql("UPDATE Docks SET DockTableId = 'e0000000-0000-0000-0000-000000000001', Side = 0 WHERE Code GLOB 'N-A*'");
            migrationBuilder.Sql("UPDATE Docks SET DockTableId = 'e0000000-0000-0000-0000-000000000001', Side = 1 WHERE Code GLOB 'N-B*'");
            // Derelict mesa 1: D-1A* → lado A, D-1B* → lado B
            migrationBuilder.Sql("UPDATE Docks SET DockTableId = 'e0000000-0000-0000-0000-000000000002', Side = 0 WHERE Code GLOB 'D-1A*'");
            migrationBuilder.Sql("UPDATE Docks SET DockTableId = 'e0000000-0000-0000-0000-000000000002', Side = 1 WHERE Code GLOB 'D-1B*'");
            // Derelict mesa 2: D-2A* → lado A, D-2B* → lado B
            migrationBuilder.Sql("UPDATE Docks SET DockTableId = 'e0000000-0000-0000-0000-000000000003', Side = 0 WHERE Code GLOB 'D-2A*'");
            migrationBuilder.Sql("UPDATE Docks SET DockTableId = 'e0000000-0000-0000-0000-000000000003', Side = 1 WHERE Code GLOB 'D-2B*'");
            // Derelict mesa 3: D-3A* → lado A, D-3B* → lado B
            migrationBuilder.Sql("UPDATE Docks SET DockTableId = 'e0000000-0000-0000-0000-000000000004', Side = 0 WHERE Code GLOB 'D-3A*'");
            migrationBuilder.Sql("UPDATE Docks SET DockTableId = 'e0000000-0000-0000-0000-000000000004', Side = 1 WHERE Code GLOB 'D-3B*'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Docks_DockTables_DockTableId",
                table: "Docks");

            migrationBuilder.DropIndex(
                name: "IX_Docks_DockTableId",
                table: "Docks");

            migrationBuilder.DropColumn(
                name: "DockTableId",
                table: "Docks");

            migrationBuilder.DropColumn(
                name: "Side",
                table: "Docks");

            migrationBuilder.AddColumn<Guid>(
                name: "DockZoneId",
                table: "Docks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Docks_DockZoneId",
                table: "Docks",
                column: "DockZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Docks_DockZones_DockZoneId",
                table: "Docks",
                column: "DockZoneId",
                principalTable: "DockZones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
