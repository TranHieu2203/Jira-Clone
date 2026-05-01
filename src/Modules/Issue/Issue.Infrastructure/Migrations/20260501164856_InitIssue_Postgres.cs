using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Issue.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitIssue_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "issue");

            migrationBuilder.CreateTable(
                name: "issues",
                schema: "issue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    issue_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_issue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    labels = table.Column<string>(type: "jsonb", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    story_points = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    original_estimate_minutes = table.Column<int>(type: "integer", nullable: true),
                    remaining_estimate_minutes = table.Column<int>(type: "integer", nullable: true),
                    time_spent_minutes = table.Column<int>(type: "integer", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_watchers",
                schema: "issue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_watchers", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_watchers_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "issue",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_watchers_issue_id_user_id",
                schema: "issue",
                table: "issue_watchers",
                columns: new[] { "issue_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_issue_watchers_user_id",
                schema: "issue",
                table: "issue_watchers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_assignee_id",
                schema: "issue",
                table: "issues",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_current_status_id",
                schema: "issue",
                table: "issues",
                column: "current_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_issue_type_id",
                schema: "issue",
                table: "issues",
                column: "issue_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_key",
                schema: "issue",
                table: "issues",
                column: "key",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_issues_parent_issue_id",
                schema: "issue",
                table: "issues",
                column: "parent_issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_project_id_number",
                schema: "issue",
                table: "issues",
                columns: new[] { "project_id", "number" });

            migrationBuilder.CreateIndex(
                name: "ix_issues_reporter_id",
                schema: "issue",
                table: "issues",
                column: "reporter_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_watchers",
                schema: "issue");

            migrationBuilder.DropTable(
                name: "issues",
                schema: "issue");
        }
    }
}
