using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitProject_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    workspace_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    key = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    avatar_url = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    lead_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    type = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_archived = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_deleted = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    deleted_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    next_issue_number = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    avatar_url = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    owner_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
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
                    table.PrimaryKey("pk_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    project_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    icon = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "NVARCHAR2(16)", maxLength: 16, nullable: true),
                    order = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_subtask = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_system = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_types_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    project_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    user_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    role = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    joined_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    workspace_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    user_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    role = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    joined_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_workspace_members_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_types_project_id_key",
                table: "issue_types",
                columns: new[] { "project_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_members_project_id_user_id",
                table: "project_members",
                columns: new[] { "project_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_members_user_id",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_lead_id",
                table: "projects",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_workspace_id_key",
                table: "projects",
                columns: new[] { "workspace_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workspace_members_user_id",
                table: "workspace_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_members_workspace_id_user_id",
                table: "workspace_members",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_owner_id",
                table: "workspaces",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_slug",
                table: "workspaces",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_types");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "workspace_members");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "workspaces");
        }
    }
}
