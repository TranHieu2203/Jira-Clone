using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitWorkflow_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "issue_status_histories",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_status_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_schemes",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    default_workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    created_trace_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_schemes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_template = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    initial_status_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_workflows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_scheme_items",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_scheme_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_scheme_items_workflow_schemes_scheme_id",
                        column: x => x.scheme_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_schemes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_statuses",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_statuses_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalSchema: "workflow",
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_transitions",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    screen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_automatic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_transitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_transitions_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalSchema: "workflow",
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transition_post_functions",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    config_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transition_post_functions", x => x.id);
                    table.ForeignKey(
                        name: "fk_transition_post_functions_workflow_transitions_transition_id",
                        column: x => x.transition_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_transitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transition_rules",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    config_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transition_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_transition_rules_workflow_transitions_transition_id",
                        column: x => x.transition_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_transitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transition_validators",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    config_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transition_validators", x => x.id);
                    table.ForeignKey(
                        name: "fk_transition_validators_workflow_transitions_transition_id",
                        column: x => x.transition_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_transitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_status_histories_issue_id",
                schema: "workflow",
                table: "issue_status_histories",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_status_histories_issue_id_changed_at",
                schema: "workflow",
                table: "issue_status_histories",
                columns: new[] { "issue_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transition_post_functions_transition_id_order",
                schema: "workflow",
                table: "transition_post_functions",
                columns: new[] { "transition_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_transition_rules_transition_id_order",
                schema: "workflow",
                table: "transition_rules",
                columns: new[] { "transition_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_transition_validators_transition_id_order",
                schema: "workflow",
                table: "transition_validators",
                columns: new[] { "transition_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_scheme_items_scheme_id_issue_type_id",
                schema: "workflow",
                table: "workflow_scheme_items",
                columns: new[] { "scheme_id", "issue_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_schemes_project_id",
                schema: "workflow",
                table: "workflow_schemes",
                column: "project_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_statuses_workflow_id_key",
                schema: "workflow",
                table: "workflow_statuses",
                columns: new[] { "workflow_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_transitions_workflow_id_from_status_id_to_status_id",
                schema: "workflow",
                table: "workflow_transitions",
                columns: new[] { "workflow_id", "from_status_id", "to_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflows_is_template",
                schema: "workflow",
                table: "workflows",
                column: "is_template");

            migrationBuilder.CreateIndex(
                name: "ix_workflows_project_id_key",
                schema: "workflow",
                table: "workflows",
                columns: new[] { "project_id", "key" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_status_histories",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "transition_post_functions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "transition_rules",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "transition_validators",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_scheme_items",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_statuses",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_transitions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_schemes",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflows",
                schema: "workflow");
        }
    }
}
