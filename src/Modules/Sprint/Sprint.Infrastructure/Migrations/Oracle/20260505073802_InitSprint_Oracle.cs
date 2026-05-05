using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sprint.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitSprint_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sprint_commit_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    sprint_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    burndown_points = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprint_commit_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    project_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(160)", maxLength: 160, nullable: false),
                    goal = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    start_date = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    end_date = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sprint_issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    sprint_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    rank = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprint_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_sprint_issues_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalTable: "sprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sprint_commit_lines_sprint_id_issue_id",
                table: "sprint_commit_lines",
                columns: new[] { "sprint_id", "issue_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sprint_issues_issue_id",
                table: "sprint_issues",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprint_issues_sprint_id_issue_id",
                table: "sprint_issues",
                columns: new[] { "sprint_id", "issue_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sprints_project_id_status",
                table: "sprints",
                columns: new[] { "project_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sprint_commit_lines");

            migrationBuilder.DropTable(
                name: "sprint_issues");

            migrationBuilder.DropTable(
                name: "sprints");
        }
    }
}
