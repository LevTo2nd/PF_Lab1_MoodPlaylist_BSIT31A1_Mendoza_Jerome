using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoodPlaylistGenerator.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalMediaSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "YouTubeUrl",
                table: "Songs",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Songs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Songs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Songs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalFilePath",
                table: "Songs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "Songs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "LocalFilePath",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Songs");

            migrationBuilder.AlterColumn<string>(
                name: "YouTubeUrl",
                table: "Songs",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
