using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitWorkflow_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issue_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    workflow_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    from_status_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    to_status_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    transition_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    changed_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    changed_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    comment = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_status_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_schemes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    project_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    default_workflow_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_schemes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    project_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    key = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    is_template = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_active = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    initial_status_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
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
                    table.PrimaryKey("pk_workflows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_scheme_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    scheme_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    issue_type_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    workflow_id = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_scheme_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_scheme_items_workflow_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalTable: "workflow_schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    workflow_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    category = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    color = table.Column<string>(type: "NVARCHAR2(16)", maxLength: 16, nullable: true),
                    order = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_final = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_statuses_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_transitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    workflow_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    from_status_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    to_status_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    screen_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    is_automatic = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_transitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_transitions_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transition_post_functions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    transition_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    type_key = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    config_json = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: false),
                    order = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transition_post_functions", x => x.id);
                    table.ForeignKey(
                        name: "fk_transition_post_functions_workflow_transitions_transition_id",
                        column: x => x.transition_id,
                        principalTable: "workflow_transitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transition_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    transition_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    type_key = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    config_json = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: false),
                    order = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transition_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_transition_rules_workflow_transitions_transition_id",
                        column: x => x.transition_id,
                        principalTable: "workflow_transitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transition_validators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    transition_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    type_key = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    config_json = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: false),
                    order = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transition_validators", x => x.id);
                    table.ForeignKey(
                        name: "fk_transition_validators_workflow_transitions_transition_id",
                        column: x => x.transition_id,
                        principalTable: "workflow_transitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_status_histories_issue_id",
                table: "issue_status_histories",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_status_histories_issue_id_changed_at",
                table: "issue_status_histories",
                columns: new[] { "issue_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transition_post_functions_transition_id_order",
                table: "transition_post_functions",
                columns: new[] { "transition_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_transition_rules_transition_id_order",
                table: "transition_rules",
                columns: new[] { "transition_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_transition_validators_transition_id_order",
                table: "transition_validators",
                columns: new[] { "transition_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_scheme_items_scheme_id_issue_type_id",
                table: "workflow_scheme_items",
                columns: new[] { "scheme_id", "issue_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_schemes_project_id",
                table: "workflow_schemes",
                column: "project_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_statuses_workflow_id_key",
                table: "workflow_statuses",
                columns: new[] { "workflow_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_transitions_workflow_id_from_status_id_to_status_id",
                table: "workflow_transitions",
                columns: new[] { "workflow_id", "from_status_id", "to_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflows_is_template",
                table: "workflows",
                column: "is_template");

            migrationBuilder.CreateIndex(
                name: "ix_workflows_project_id_key",
                table: "workflows",
                columns: new[] { "project_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_status_histories");

            migrationBuilder.DropTable(
                name: "transition_post_functions");

            migrationBuilder.DropTable(
                name: "transition_rules");

            migrationBuilder.DropTable(
                name: "transition_validators");

            migrationBuilder.DropTable(
                name: "workflow_scheme_items");

            migrationBuilder.DropTable(
                name: "workflow_statuses");

            migrationBuilder.DropTable(
                name: "workflow_transitions");

            migrationBuilder.DropTable(
                name: "workflow_schemes");

            migrationBuilder.DropTable(
                name: "workflows");
        }
    }
}
