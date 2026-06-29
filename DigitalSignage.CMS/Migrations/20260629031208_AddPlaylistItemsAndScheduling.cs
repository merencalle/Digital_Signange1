using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalSignage.CMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistItemsAndScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentIds",
                table: "Playlists");

            migrationBuilder.RenameColumn(
                name: "ScheduleJson",
                table: "Playlists",
                newName: "StartDate");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DailyEndTime",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DailyStartTime",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DaysOfWeek",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Playlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlaylistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlaylistId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistItems_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaylistItems_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_ContentItemId",
                table: "PlaylistItems",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_PlaylistId",
                table: "PlaylistItems",
                column: "PlaylistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "DailyEndTime",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "DailyStartTime",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "DaysOfWeek",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Playlists");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Playlists",
                newName: "ScheduleJson");

            migrationBuilder.AddColumn<string>(
                name: "ContentIds",
                table: "Playlists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
