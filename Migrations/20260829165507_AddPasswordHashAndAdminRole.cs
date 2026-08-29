using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusMapApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHashAndAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "profiles");
        }
    }
}
