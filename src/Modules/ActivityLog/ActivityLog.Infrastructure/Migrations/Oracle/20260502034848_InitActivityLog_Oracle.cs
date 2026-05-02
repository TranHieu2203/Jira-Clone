using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivityLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitActivityLog_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    kind = table.Column<string>(type: "NVARCHAR2(192)", maxLength: 192, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    payload_json = table.Column<string>(type: "CLOB", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activity_entries_issue_id",
                table: "activity_entries",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_activity_entries_issue_id_occurred_at",
                table: "activity_entries",
                columns: new[] { "issue_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_entries");
        }
    }
}
