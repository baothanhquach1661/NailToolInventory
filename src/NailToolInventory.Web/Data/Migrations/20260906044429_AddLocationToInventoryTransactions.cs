using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NailToolInventory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToInventoryTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InventoryLocationId",
                table: "InventoryTransactions",
                type: "INTEGER",
                nullable: true);

            // Assign all existing transactions
            // to the default Main Warehouse.
            migrationBuilder.Sql(
                """
                UPDATE "InventoryTransactions"
                SET "InventoryLocationId" =
                (
                    SELECT "Id"
                    FROM "InventoryLocations"
                    WHERE "Code" = 'MAIN'
                    LIMIT 1
                )
                WHERE "InventoryLocationId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name:
                    "IX_InventoryTransactions_InventoryLocationId_CreatedAtUtc",
                table: "InventoryTransactions",
                columns: new[]
                {
                    "InventoryLocationId",
                    "CreatedAtUtc"
                });

            migrationBuilder.AddForeignKey(
                name:
                    "FK_InventoryTransactions_InventoryLocations_InventoryLocationId",
                table: "InventoryTransactions",
                column: "InventoryLocationId",
                principalTable: "InventoryLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name:
                    "FK_InventoryTransactions_InventoryLocations_InventoryLocationId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name:
                    "IX_InventoryTransactions_InventoryLocationId_CreatedAtUtc",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "InventoryLocationId",
                table: "InventoryTransactions");
        }
    }
}