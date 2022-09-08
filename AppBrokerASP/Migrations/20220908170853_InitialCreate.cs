using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBrokerASP.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PropertyName = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoryValueBase",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HistoryValueId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryValueBase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryValueBase_Properties_HistoryValueId",
                        column: x => x.HistoryValueId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoryValueBool",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryValueBool", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryValueBool_HistoryValueBase_Id",
                        column: x => x.Id,
                        principalTable: "HistoryValueBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoryValueDateTime",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryValueDateTime", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryValueDateTime_HistoryValueBase_Id",
                        column: x => x.Id,
                        principalTable: "HistoryValueBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoryValueDouble",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryValueDouble", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryValueDouble_HistoryValueBase_Id",
                        column: x => x.Id,
                        principalTable: "HistoryValueBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoryValueLong",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryValueLong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryValueLong_HistoryValueBase_Id",
                        column: x => x.Id,
                        principalTable: "HistoryValueBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoryValueString",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryValueString", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryValueString_HistoryValueBase_Id",
                        column: x => x.Id,
                        principalTable: "HistoryValueBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoryValueTimeSpan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryValueTimeSpan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryValueTimeSpan_HistoryValueBase_Id",
                        column: x => x.Id,
                        principalTable: "HistoryValueBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoryValueBase_HistoryValueId",
                table: "HistoryValueBase",
                column: "HistoryValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_DeviceId",
                table: "Properties",
                column: "DeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoryValueBool");

            migrationBuilder.DropTable(
                name: "HistoryValueDateTime");

            migrationBuilder.DropTable(
                name: "HistoryValueDouble");

            migrationBuilder.DropTable(
                name: "HistoryValueLong");

            migrationBuilder.DropTable(
                name: "HistoryValueString");

            migrationBuilder.DropTable(
                name: "HistoryValueTimeSpan");

            migrationBuilder.DropTable(
                name: "HistoryValueBase");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
