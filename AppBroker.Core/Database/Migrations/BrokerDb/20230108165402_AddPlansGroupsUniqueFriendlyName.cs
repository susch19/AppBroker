using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBroker.Core.Database.Migrations.BrokerDb
{
    /// <inheritdoc />
    public partial class AddPlansGroupsUniqueFriendlyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HeatingPlanId",
                table: "HeaterConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FriendlyUniqueName",
                table: "Devices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupDeviceMappingModels",
                columns: table => new
                {
                    DeviceId = table.Column<long>(type: "INTEGER", nullable: false),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupDeviceMappingModels", x => new { x.DeviceId, x.GroupId });
                });

            migrationBuilder.CreateTable(
                name: "GroupModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HeatingPlanModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeatingPlanModels", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupDeviceMappingModels");

            migrationBuilder.DropTable(
                name: "GroupModels");

            migrationBuilder.DropTable(
                name: "HeatingPlanModels");

            migrationBuilder.DropColumn(
                name: "HeatingPlanId",
                table: "HeaterConfigs");

            migrationBuilder.DropColumn(
                name: "FriendlyUniqueName",
                table: "Devices");
        }
    }
}
