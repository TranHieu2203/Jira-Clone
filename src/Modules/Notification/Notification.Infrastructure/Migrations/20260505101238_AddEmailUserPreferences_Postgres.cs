using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailUserPreferences_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_user_preferences",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    no_assignee = table.Column<bool>(type: "boolean", nullable: false),
                    no_status = table.Column<bool>(type: "boolean", nullable: false),
                    no_comment = table.Column<bool>(type: "boolean", nullable: false),
                    no_mention = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_user_preferences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_template_key_to_email_status_sent_at",
                schema: "notification",
                table: "email_logs",
                columns: new[] { "template_key", "to_email", "status", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_user_preferences_user_id",
                schema: "notification",
                table: "email_user_preferences",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_user_preferences",
                schema: "notification");

            migrationBuilder.DropIndex(
                name: "ix_email_logs_template_key_to_email_status_sent_at",
                schema: "notification",
                table: "email_logs");
        }
    }
}
