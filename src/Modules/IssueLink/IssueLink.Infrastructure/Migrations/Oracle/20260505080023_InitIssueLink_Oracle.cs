using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitIssueLink_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issue_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    source_issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    source_project_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    target_issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    target_project_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    link_type = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_links", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_links_source_issue_id",
                table: "issue_links",
                column: "source_issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_links_source_issue_id_target_issue_id_link_type",
                table: "issue_links",
                columns: new[] { "source_issue_id", "target_issue_id", "link_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_issue_links_target_issue_id",
                table: "issue_links",
                column: "target_issue_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_links");
        }
    }
}
