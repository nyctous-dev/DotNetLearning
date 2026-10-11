using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoidsSimulation.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSimulationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSimulationPreferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    BoidColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    BackgroundColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    ShowTrails = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSimulationPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSimulationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSimulationPreferences");
        }
    }
}
