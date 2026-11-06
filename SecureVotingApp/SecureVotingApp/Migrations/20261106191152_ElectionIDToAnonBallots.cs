using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVotingApp.Migrations
{
    /// <inheritdoc />
    public partial class ElectionIDToAnonBallots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ElectionId",
                table: "AnonBallots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectionId",
                table: "AnonBallots");
        }
    }
}
