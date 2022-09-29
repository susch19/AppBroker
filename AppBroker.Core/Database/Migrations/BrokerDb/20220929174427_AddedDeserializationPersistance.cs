using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBroker.Core.Database.Migrations.BrokerDb
{
    public partial class AddedDeserializationPersistance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HeaterConfigs_Devices_DeviceId",
                table: "HeaterConfigs");

            migrationBuilder.AlterColumn<long>(
                name: "DeviceId",
                table: "HeaterConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeserializationData",
                table: "Devices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StartAutomatically",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_HeaterConfigs_Devices_DeviceId",
                table: "HeaterConfigs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HeaterConfigs_Devices_DeviceId",
                table: "HeaterConfigs");

            migrationBuilder.DropColumn(
                name: "DeserializationData",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "StartAutomatically",
                table: "Devices");

            migrationBuilder.AlterColumn<long>(
                name: "DeviceId",
                table: "HeaterConfigs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_HeaterConfigs_Devices_DeviceId",
                table: "HeaterConfigs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id");
        }
    }
}
