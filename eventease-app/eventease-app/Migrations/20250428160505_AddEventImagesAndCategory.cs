using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eventease_app.Migrations
{
    /// <inheritdoc />
    public partial class AddEventImagesAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroFileName",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LongDescription",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailFileName",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "HeroFileName",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LongDescription",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ThumbnailFileName",
                table: "Events");
        }
    }
}
