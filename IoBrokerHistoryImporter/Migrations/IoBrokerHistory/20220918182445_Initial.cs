using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoBrokerHistoryImporter.Migrations.IoBrokerHistory
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "datapoints",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datapoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ts_bool",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ts = table.Column<long>(type: "INTEGER", nullable: false),
                    val = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ts_bool", x => x.id);
                    table.ForeignKey(
                        name: "FK_ts_bool_datapoints_id",
                        column: x => x.id,
                        principalTable: "datapoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ts_number",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ts = table.Column<long>(type: "INTEGER", nullable: false),
                    val = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ts_number", x => x.id);
                    table.ForeignKey(
                        name: "FK_ts_number_datapoints_id",
                        column: x => x.id,
                        principalTable: "datapoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ts_string",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ts = table.Column<long>(type: "INTEGER", nullable: false),
                    val = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ts_string", x => x.id);
                    table.ForeignKey(
                        name: "FK_ts_string_datapoints_id",
                        column: x => x.id,
                        principalTable: "datapoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ts_bool");

            migrationBuilder.DropTable(
                name: "ts_number");

            migrationBuilder.DropTable(
                name: "ts_string");

            migrationBuilder.DropTable(
                name: "datapoints");
        }
    }
}
