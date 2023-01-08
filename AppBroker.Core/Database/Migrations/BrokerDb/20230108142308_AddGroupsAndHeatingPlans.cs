﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBroker.Core.Database.Migrations.BrokerDb
{
    /// <inheritdoc />
    public partial class AddGroupsAndHeatingPlans : Migration
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
                    table.ForeignKey(
                        name: "FK_GroupDeviceMappingModels_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupDeviceMappingModels_GroupModels_GroupId",
                        column: x => x.GroupId,
                        principalTable: "GroupModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeaterConfigs_HeatingPlanId",
                table: "HeaterConfigs",
                column: "HeatingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupDeviceMappingModels_GroupId",
                table: "GroupDeviceMappingModels",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_HeaterConfigs_HeatingPlanModels_HeatingPlanId",
                table: "HeaterConfigs",
                column: "HeatingPlanId",
                principalTable: "HeatingPlanModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HeaterConfigs_HeatingPlanModels_HeatingPlanId",
                table: "HeaterConfigs");

            migrationBuilder.DropTable(
                name: "GroupDeviceMappingModels");

            migrationBuilder.DropTable(
                name: "HeatingPlanModels");

            migrationBuilder.DropTable(
                name: "GroupModels");

            migrationBuilder.DropIndex(
                name: "IX_HeaterConfigs_HeatingPlanId",
                table: "HeaterConfigs");

            migrationBuilder.DropColumn(
                name: "HeatingPlanId",
                table: "HeaterConfigs");
        }
    }
}
