using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitIssueLink_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "issue_link");

            migrationBuilder.CreateTable(
                name: "issue_links",
                schema: "issue_link",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    link_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_links", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_links_source_issue_id",
                schema: "issue_link",
                table: "issue_links",
                column: "source_issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_links_source_issue_id_target_issue_id_link_type",
                schema: "issue_link",
                table: "issue_links",
                columns: new[] { "source_issue_id", "target_issue_id", "link_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_issue_links_target_issue_id",
                schema: "issue_link",
                table: "issue_links",
                column: "target_issue_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_links",
                schema: "issue_link");
        }
    }
}
