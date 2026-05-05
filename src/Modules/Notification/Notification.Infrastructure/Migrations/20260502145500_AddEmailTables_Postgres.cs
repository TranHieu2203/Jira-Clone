using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Migrations;

public partial class AddEmailTables_Postgres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "email_templates",
            schema: "notification",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                subject_template = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                html_body_template = table.Column<string>(type: "text", nullable: false),
                text_body_template = table.Column<string>(type: "text", nullable: true),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_email_templates", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_email_templates_key",
            schema: "notification",
            table: "email_templates",
            column: "key",
            unique: true);

        migrationBuilder.CreateTable(
            name: "email_logs",
            schema: "notification",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                template_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                to_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                subject_rendered = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                body_preview = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                args_json = table.Column<string>(type: "jsonb", nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                provider_message_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_email_logs", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_email_logs_template_key_created_at",
            schema: "notification",
            table: "email_logs",
            columns: new[] { "template_key", "created_at" });

        migrationBuilder.CreateIndex(
            name: "ix_email_logs_to_email_created_at",
            schema: "notification",
            table: "email_logs",
            columns: new[] { "to_email", "created_at" });

        migrationBuilder.CreateIndex(
            name: "ix_email_logs_status_created_at",
            schema: "notification",
            table: "email_logs",
            columns: new[] { "status", "created_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "email_logs",
            schema: "notification");

        migrationBuilder.DropTable(
            name: "email_templates",
            schema: "notification");
    }
}

