using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NailToolInventory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PerformedByName",
                table: "InventoryTransactions",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedByUserId",
                table: "InventoryTransactions",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PerformedByUserId",
                table: "InventoryTransactions",
                column: "PerformedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_AspNetUsers_PerformedByUserId",
                table: "InventoryTransactions",
                column: "PerformedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_AspNetUsers_PerformedByUserId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_PerformedByUserId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PerformedByName",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PerformedByUserId",
                table: "InventoryTransactions");
        }
    }
}
