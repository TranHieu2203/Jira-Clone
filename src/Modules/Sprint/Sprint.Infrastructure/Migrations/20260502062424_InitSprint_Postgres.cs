using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sprint.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitSprint_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sprint");

            migrationBuilder.CreateTable(
                name: "sprint_commit_lines",
                schema: "sprint",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    burndown_points = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprint_commit_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                schema: "sprint",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    goal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sprint_issues",
                schema: "sprint",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprint_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_sprint_issues_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalSchema: "sprint",
                        principalTable: "sprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sprint_commit_lines_sprint_id_issue_id",
                schema: "sprint",
                table: "sprint_commit_lines",
                columns: new[] { "sprint_id", "issue_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sprint_issues_issue_id",
                schema: "sprint",
                table: "sprint_issues",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprint_issues_sprint_id_issue_id",
                schema: "sprint",
                table: "sprint_issues",
                columns: new[] { "sprint_id", "issue_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sprints_project_id_status",
                schema: "sprint",
                table: "sprints",
                columns: new[] { "project_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sprint_commit_lines",
                schema: "sprint");

            migrationBuilder.DropTable(
                name: "sprint_issues",
                schema: "sprint");

            migrationBuilder.DropTable(
                name: "sprints",
                schema: "sprint");
        }
    }
}
