using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVotingApp.Migrations
{
    /// <inheritdoc />
    public partial class voting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssuedCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ElectionId = table.Column<string>(type: "TEXT", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpentCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ElectionId = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialHash = table.Column<string>(type: "TEXT", nullable: false),
                    SpentAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpentCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuedCredentials_UserId_ElectionId",
                table: "IssuedCredentials",
                columns: new[] { "UserId", "ElectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpentCredentials_ElectionId_CredentialHash",
                table: "SpentCredentials",
                columns: new[] { "ElectionId", "CredentialHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssuedCredentials");

            migrationBuilder.DropTable(
                name: "SpentCredentials");
        }
    }
}
