using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitAuditLog_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    action = table.Column<string>(type: "NVARCHAR2(80)", maxLength: 80, nullable: false),
                    scope = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    scope_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    payload_json = table.Column<string>(type: "CLOB", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_action_occurred_at",
                table: "audit_entries",
                columns: new[] { "action", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_actor_user_id",
                table: "audit_entries",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_occurred_at",
                table: "audit_entries",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_scope_id",
                table: "audit_entries",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_scope_occurred_at",
                table: "audit_entries",
                columns: new[] { "scope", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");
        }
    }
}
