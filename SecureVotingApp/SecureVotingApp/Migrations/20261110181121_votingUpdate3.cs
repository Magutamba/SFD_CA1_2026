using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVotingApp.Migrations
{
    /// <inheritdoc />
    public partial class votingUpdate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpentAt",
                table: "SpentCredentials");

            migrationBuilder.RenameColumn(
                name: "IssuedAt",
                table: "IssuedCredentials",
                newName: "SignedBlindedCredential");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "IssuedCredentials",
                newName: "RecoveryPackage");

            migrationBuilder.AddColumn<string>(
                name: "BlindedCredential",
                table: "IssuedCredentials",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "IssuedCredentials",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_IssuedCredentials_RecoveryPackage",
                table: "IssuedCredentials",
                column: "RecoveryPackage",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IssuedCredentials_RecoveryPackage",
                table: "IssuedCredentials");

            migrationBuilder.DropColumn(
                name: "BlindedCredential",
                table: "IssuedCredentials");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "IssuedCredentials");

            migrationBuilder.RenameColumn(
                name: "SignedBlindedCredential",
                table: "IssuedCredentials",
                newName: "IssuedAt");

            migrationBuilder.RenameColumn(
                name: "RecoveryPackage",
                table: "IssuedCredentials",
                newName: "ExpiresAt");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SpentAt",
                table: "SpentCredentials",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
