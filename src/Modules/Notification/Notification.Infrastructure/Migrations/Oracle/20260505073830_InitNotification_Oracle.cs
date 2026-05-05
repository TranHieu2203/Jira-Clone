using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitNotification_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    template_key = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    to_email = table.Column<string>(type: "NVARCHAR2(320)", maxLength: 320, nullable: false),
                    subject_rendered = table.Column<string>(type: "NVARCHAR2(512)", maxLength: 512, nullable: false),
                    body_preview = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    args_json = table.Column<string>(type: "CLOB", nullable: true),
                    status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    provider = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    provider_message_id = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: true),
                    error = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    sent_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    key = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: false),
                    subject_template = table.Column<string>(type: "NVARCHAR2(512)", maxLength: 512, nullable: false),
                    html_body_template = table.Column<string>(type: "CLOB", nullable: false),
                    text_body_template = table.Column<string>(type: "CLOB", nullable: true),
                    is_enabled = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "in_app_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    type = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    payload_json = table.Column<string>(type: "CLOB", nullable: false),
                    is_read = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_in_app_notifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_status_created_at",
                table: "email_logs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_template_key_created_at",
                table: "email_logs",
                columns: new[] { "template_key", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_to_email_created_at",
                table: "email_logs",
                columns: new[] { "to_email", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_templates_key",
                table: "email_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_in_app_notifications_recipient_user_id_created_at",
                table: "in_app_notifications",
                columns: new[] { "recipient_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_in_app_notifications_recipient_user_id_is_read",
                table: "in_app_notifications",
                columns: new[] { "recipient_user_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "in_app_notifications");
        }
    }
}
