using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBroker.Core.Database.Migrations.BrokerDb
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeName = table.Column<string>(type: "TEXT", nullable: false),
                    FriendlyName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceToDeviceMappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentId = table.Column<long>(type: "INTEGER", nullable: true),
                    ChildId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceToDeviceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceToDeviceMappings_Devices_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceToDeviceMappings_Devices_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HeaterConfigs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<long>(type: "INTEGER", nullable: true),
                    DayOfWeek = table.Column<byte>(type: "INTEGER", nullable: false),
                    TimeOfDay = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Temperature = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeaterConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeaterConfigs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceToDeviceMappings_ChildId",
                table: "DeviceToDeviceMappings",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceToDeviceMappings_ParentId",
                table: "DeviceToDeviceMappings",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_HeaterConfigs_DeviceId",
                table: "HeaterConfigs",
                column: "DeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceToDeviceMappings");

            migrationBuilder.DropTable(
                name: "HeaterConfigs");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
