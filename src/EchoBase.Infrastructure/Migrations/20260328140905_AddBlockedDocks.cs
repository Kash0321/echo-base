using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedDocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedDocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DockId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlockedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedDocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockedDocks_Docks_DockId",
                        column: x => x.DockId,
                        principalTable: "Docks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlockedDocks_Users_BlockedByUserId",
                        column: x => x.BlockedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedDocks_BlockedByUserId",
                table: "BlockedDocks",
                column: "BlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedDocks_DockId_IsActive",
                table: "BlockedDocks",
                columns: new[] { "DockId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedDocks_DockId_StartDate_EndDate",
                table: "BlockedDocks",
                columns: new[] { "DockId", "StartDate", "EndDate" },
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedDocks");
        }
    }
}
