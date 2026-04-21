using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatSpark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteCodeAndUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bio",
                table: "users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website_url",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invite_code",
                table: "channels",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bio",
                table: "users");

            migrationBuilder.DropColumn(
                name: "website_url",
                table: "users");

            migrationBuilder.DropColumn(
                name: "invite_code",
                table: "channels");
        }
    }
}
