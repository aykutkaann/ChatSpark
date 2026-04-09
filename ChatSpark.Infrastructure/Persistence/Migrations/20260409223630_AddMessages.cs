using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatSpark.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_channel_id_sent_at",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_messages_channel_id_sent_at",
                table: "messages",
                columns: new[] { "channel_id", "sent_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_channel_id_sent_at",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_messages_channel_id_sent_at",
                table: "messages",
                columns: new[] { "channel_id", "sent_at" });
        }
    }
}
