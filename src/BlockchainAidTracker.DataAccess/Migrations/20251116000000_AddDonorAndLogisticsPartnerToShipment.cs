using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlockchainAidTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorAndLogisticsPartnerToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DonorId",
                table: "Shipments",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedLogisticsPartnerId",
                table: "Shipments",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogisticsPartnerPublicKey",
                table: "Shipments",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DonorId",
                table: "Shipments",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_AssignedLogisticsPartnerId",
                table: "Shipments",
                column: "AssignedLogisticsPartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_DonorId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_AssignedLogisticsPartnerId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DonorId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "AssignedLogisticsPartnerId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "LogisticsPartnerPublicKey",
                table: "Shipments");
        }
    }
}
