using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlockchainAidTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MigrationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Validators",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PublicKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EncryptedPrivateKey = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastBlockCreatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalBlocksCreated = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Validators", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Validators_IsActive",
                table: "Validators",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Validators_IsActive_Priority",
                table: "Validators",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Validators_Name",
                table: "Validators",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Validators_Priority",
                table: "Validators",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Validators_PublicKey",
                table: "Validators",
                column: "PublicKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Validators");
        }
    }
}
