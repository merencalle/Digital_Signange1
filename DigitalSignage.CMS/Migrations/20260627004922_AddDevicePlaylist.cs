using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalSignage.CMS.Migrations
{
    /// <inheritdoc />
    public partial class AddDevicePlaylist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlaylistId",
                table: "Devices",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "Devices");
        }
    }
}
