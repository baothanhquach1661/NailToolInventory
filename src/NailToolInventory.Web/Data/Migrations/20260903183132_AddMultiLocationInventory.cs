using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NailToolInventory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLocationInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryLocations",
                columns: table => new
                {
                    Id = table.Column<int>(
                            type: "INTEGER",
                            nullable: false)
                        .Annotation(
                            "Sqlite:Autoincrement",
                            true),

                    Code = table.Column<string>(
                        type: "TEXT",
                        maxLength: 30,
                        nullable: false),

                    Name = table.Column<string>(
                        type: "TEXT",
                        maxLength: 150,
                        nullable: false),

                    Address = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true),

                    IsActive = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false),

                    CanFulfillOnlineOrders = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false),

                    FulfillmentPriority = table.Column<int>(
                        type: "INTEGER",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_InventoryLocations",
                        x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLevels",
                columns: table => new
                {
                    Id = table.Column<int>(
                            type: "INTEGER",
                            nullable: false)
                        .Annotation(
                            "Sqlite:Autoincrement",
                            true),

                    ProductId = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    InventoryLocationId = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    QuantityOnHand = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    ReservedQuantity = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    ReorderLevel = table.Column<int>(
                        type: "INTEGER",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_InventoryLevels",
                        x => x.Id);

                    table.ForeignKey(
                        name:
                            "FK_InventoryLevels_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name:
                            "FK_InventoryLevels_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name:
                    "IX_InventoryLevels_InventoryLocationId",
                table: "InventoryLevels",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name:
                    "IX_InventoryLevels_ProductId_InventoryLocationId",
                table: "InventoryLevels",
                columns: new[]
                {
                    "ProductId",
                    "InventoryLocationId"
                },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocations_Code",
                table: "InventoryLocations",
                column: "Code",
                unique: true);

            // Create the default warehouse for existing inventory.
            migrationBuilder.Sql(
                """
                INSERT INTO "InventoryLocations"
                (
                    "Code",
                    "Name",
                    "Address",
                    "IsActive",
                    "CanFulfillOnlineOrders",
                    "FulfillmentPriority"
                )
                VALUES
                (
                    'MAIN',
                    'Main Warehouse',
                    NULL,
                    1,
                    1,
                    1
                );
                """);

            // Move every product's existing inventory into Main Warehouse.
            migrationBuilder.Sql(
                """
                INSERT INTO "InventoryLevels"
                (
                    "ProductId",
                    "InventoryLocationId",
                    "QuantityOnHand",
                    "ReservedQuantity",
                    "ReorderLevel"
                )
                SELECT
                    product."Id",
                    location."Id",
                    product."QuantityOnHand",
                    0,
                    product."ReorderLevel"
                FROM "Products" AS product
                CROSS JOIN "InventoryLocations" AS location
                WHERE location."Code" = 'MAIN';
                """);
        }

        /// <inheritdoc />
        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryLevels");

            migrationBuilder.DropTable(
                name: "InventoryLocations");
        }
    }
}