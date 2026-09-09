using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NailToolInventory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockTransferId",
                table: "InventoryTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransferNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SourceLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    DestinationLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CreatedByName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ShippedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransfers_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockTransfers_InventoryLocations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_InventoryLocations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StockTransferId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityReceived = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_StockTransferId",
                table: "InventoryTransactions",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_ProductId",
                table: "StockTransferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_StockTransferId_ProductId",
                table: "StockTransferItems",
                columns: new[] { "StockTransferId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_CreatedByUserId",
                table: "StockTransfers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationLocationId",
                table: "StockTransfers",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceLocationId",
                table: "StockTransfers",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_Status_CreatedAtUtc",
                table: "StockTransfers",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferNumber",
                table: "StockTransfers",
                column: "TransferNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_StockTransfers_StockTransferId",
                table: "InventoryTransactions",
                column: "StockTransferId",
                principalTable: "StockTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_StockTransfers_StockTransferId",
                table: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "StockTransferItems");

            migrationBuilder.DropTable(
                name: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_StockTransferId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "StockTransferId",
                table: "InventoryTransactions");
        }
    }
}
