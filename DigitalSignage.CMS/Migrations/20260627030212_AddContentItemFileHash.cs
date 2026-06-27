using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalSignage.CMS.Migrations
{
    /// <inheritdoc />
    public partial class AddContentItemFileHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "ContentItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "ContentItems");
        }
    }
}
