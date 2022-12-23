using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBroker.Core.Database.Migrations.BrokerDb
{
    public partial class AddedLastState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastState",
                table: "Devices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastState",
                table: "Devices");
        }
    }
}
