using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlockchainAidTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSupplierPaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RegistrationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BusinessCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EncryptedBankDetails = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    PaymentThreshold = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TaxId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VerificationStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SupplierId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ShipmentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BlockchainTransactionHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ExternalPaymentReference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierShipments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SupplierId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ShipmentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GoodsDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ProvidedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaymentReleased = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    PaymentReleasedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaymentTransactionReference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PaymentStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierShipments_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierShipments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_CreatedTimestamp",
                table: "PaymentRecords",
                column: "CreatedTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_ShipmentId",
                table: "PaymentRecords",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_Status_CreatedTimestamp",
                table: "PaymentRecords",
                columns: new[] { "Status", "CreatedTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_SupplierId",
                table: "PaymentRecords",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_SupplierId_CreatedTimestamp",
                table: "PaymentRecords",
                columns: new[] { "SupplierId", "CreatedTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_SupplierId_Status",
                table: "PaymentRecords",
                columns: new[] { "SupplierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CompanyName",
                table: "Suppliers",
                column: "CompanyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsActive",
                table: "Suppliers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsActive_VerificationStatus",
                table: "Suppliers",
                columns: new[] { "IsActive", "VerificationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_RegistrationId",
                table: "Suppliers",
                column: "RegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TaxId",
                table: "Suppliers",
                column: "TaxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_UserId",
                table: "Suppliers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_VerificationStatus",
                table: "Suppliers",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierShipments_PaymentReleased_PaymentStatus",
                table: "SupplierShipments",
                columns: new[] { "PaymentReleased", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierShipments_ShipmentId",
                table: "SupplierShipments",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierShipments_SupplierId",
                table: "SupplierShipments",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierShipments_SupplierId_PaymentStatus",
                table: "SupplierShipments",
                columns: new[] { "SupplierId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierShipments_SupplierId_ShipmentId",
                table: "SupplierShipments",
                columns: new[] { "SupplierId", "ShipmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "SupplierShipments");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
