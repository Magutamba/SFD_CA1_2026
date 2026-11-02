using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVotingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLastUsedTotpTimeStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TotpSecret",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastUsedTotpTimeStep",
                table: "Users",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUsedTotpTimeStep",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "TotpSecret",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
