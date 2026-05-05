using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Issue.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitIssue_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    project_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    key = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    number = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    issue_type_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    workflow_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    current_status_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    summary = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "CLOB", nullable: true),
                    priority = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    reporter_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    assignee_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    parent_issue_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    labels = table.Column<string>(type: "CLOB", nullable: false),
                    due_date = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    story_points = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: true),
                    original_estimate_minutes = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    remaining_estimate_minutes = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    time_spent_minutes = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    is_archived = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_deleted = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    deleted_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "saved_filters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    jql = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    description = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    is_shared = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_filters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_watchers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    user_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    added_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_watchers", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_watchers_issues_issue_id",
                        column: x => x.issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_watchers_issue_id_user_id",
                table: "issue_watchers",
                columns: new[] { "issue_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_issue_watchers_user_id",
                table: "issue_watchers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_assignee_id",
                table: "issues",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_current_status_id",
                table: "issues",
                column: "current_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_issue_type_id",
                table: "issues",
                column: "issue_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_key",
                table: "issues",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_issues_parent_issue_id",
                table: "issues",
                column: "parent_issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_project_id_number",
                table: "issues",
                columns: new[] { "project_id", "number" });

            migrationBuilder.CreateIndex(
                name: "ix_issues_reporter_id",
                table: "issues",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_filters_is_shared",
                table: "saved_filters",
                column: "is_shared");

            migrationBuilder.CreateIndex(
                name: "ix_saved_filters_owner_user_id",
                table: "saved_filters",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_watchers");

            migrationBuilder.DropTable(
                name: "saved_filters");

            migrationBuilder.DropTable(
                name: "issues");
        }
    }
}
