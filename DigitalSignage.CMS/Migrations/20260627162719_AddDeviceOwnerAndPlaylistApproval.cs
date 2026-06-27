using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalSignage.CMS.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceOwnerAndPlaylistApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Playlists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByUserId",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Devices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Devices");
        }
    }
}
