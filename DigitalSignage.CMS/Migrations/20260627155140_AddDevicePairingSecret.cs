using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalSignage.CMS.Migrations
{
    /// <inheritdoc />
    public partial class AddDevicePairingSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaired",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PairingSecret",
                table: "Devices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaired",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PairingSecret",
                table: "Devices");
        }
    }
}
