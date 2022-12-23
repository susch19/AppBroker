using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBroker.Core.Database.Migrations
{
    public partial class HeaterConfigs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoryValueHeaterConfig",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeOfDay = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Temperature = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryValueHeaterConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryValueHeaterConfig_HistoryValueBase_Id",
                        column: x => x.Id,
                        principalTable: "HistoryValueBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoryValueHeaterConfig");
        }
    }
}
