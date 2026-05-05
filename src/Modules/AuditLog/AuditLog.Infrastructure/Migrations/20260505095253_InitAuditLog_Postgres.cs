using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitAuditLog_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_action_occurred_at",
                schema: "audit",
                table: "audit_entries",
                columns: new[] { "action", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_actor_user_id",
                schema: "audit",
                table: "audit_entries",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_occurred_at",
                schema: "audit",
                table: "audit_entries",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_scope_id",
                schema: "audit",
                table: "audit_entries",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_scope_occurred_at",
                schema: "audit",
                table: "audit_entries",
                columns: new[] { "scope", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "audit");
        }
    }
}
