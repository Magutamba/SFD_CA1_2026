using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVotingApp.Migrations
{
    /// <inheritdoc />
    public partial class votingUpdate5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IssuedCredentials_RecoveryPackage",
                table: "IssuedCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedCredentials_RecoveryPackage",
                table: "IssuedCredentials",
                column: "RecoveryPackage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IssuedCredentials_RecoveryPackage",
                table: "IssuedCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedCredentials_RecoveryPackage",
                table: "IssuedCredentials",
                column: "RecoveryPackage",
                unique: true);
        }
    }
}
