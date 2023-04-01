using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBroker.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class HistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "HistoryValueTimestampIndex",
                table: "HistoryValueBase",
                columns: new[] { "Timestamp", "HistoryValueId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "HistoryValueTimestampIndex",
                table: "HistoryValueBase");
        }
    }
}
