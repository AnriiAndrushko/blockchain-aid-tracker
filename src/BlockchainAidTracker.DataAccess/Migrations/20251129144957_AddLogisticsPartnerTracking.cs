using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlockchainAidTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLogisticsPartnerTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ShipmentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryEvents_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryEvents_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentLocations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ShipmentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    LocationName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GpsAccuracy = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentLocations_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentLocations_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryEvents_CreatedByUserId",
                table: "DeliveryEvents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryEvents_CreatedTimestamp",
                table: "DeliveryEvents",
                column: "CreatedTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryEvents_EventType",
                table: "DeliveryEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryEvents_ShipmentId_CreatedTimestamp",
                table: "DeliveryEvents",
                columns: new[] { "ShipmentId", "CreatedTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLocations_CreatedTimestamp",
                table: "ShipmentLocations",
                column: "CreatedTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLocations_ShipmentId_CreatedTimestamp",
                table: "ShipmentLocations",
                columns: new[] { "ShipmentId", "CreatedTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLocations_UpdatedByUserId",
                table: "ShipmentLocations",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryEvents");

            migrationBuilder.DropTable(
                name: "ShipmentLocations");
        }
    }
}
