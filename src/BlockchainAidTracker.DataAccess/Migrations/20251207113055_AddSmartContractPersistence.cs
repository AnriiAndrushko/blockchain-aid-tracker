using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlockchainAidTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartContractPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmartContracts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    StateJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartContracts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmartContracts_DeployedAt",
                table: "SmartContracts",
                column: "DeployedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SmartContracts_IsEnabled",
                table: "SmartContracts",
                column: "IsEnabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmartContracts");
        }
    }
}
