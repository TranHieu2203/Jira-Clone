using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomField.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitCustomField_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "custom_field");

            migrationBuilder.CreateTable(
                name: "custom_fields",
                schema: "custom_field",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_searchable = table.Column<bool>(type: "boolean", nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("pk_custom_fields", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_field_values",
                schema: "custom_field",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value_json = table.Column<string>(type: "jsonb", nullable: false),
                    indexed_string = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    indexed_number = table.Column<decimal>(type: "numeric(28,8)", precision: 28, scale: 8, nullable: true),
                    indexed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_field_values", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custom_field_contexts",
                schema: "custom_field",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_global = table.Column<bool>(type: "boolean", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value_json = table.Column<string>(type: "jsonb", nullable: true),
                    project_ids = table.Column<string>(type: "jsonb", nullable: false),
                    issue_type_ids = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_field_contexts", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_field_contexts_custom_fields_custom_field_id",
                        column: x => x.custom_field_id,
                        principalSchema: "custom_field",
                        principalTable: "custom_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "custom_field_options",
                schema: "custom_field",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_disabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_field_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_field_options_custom_fields_custom_field_id",
                        column: x => x.custom_field_id,
                        principalSchema: "custom_field",
                        principalTable: "custom_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_custom_field_contexts_custom_field_id",
                schema: "custom_field",
                table: "custom_field_contexts",
                column: "custom_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_custom_field_options_custom_field_id_parent_option_id_value",
                schema: "custom_field",
                table: "custom_field_options",
                columns: new[] { "custom_field_id", "parent_option_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_custom_fields_is_system",
                schema: "custom_field",
                table: "custom_fields",
                column: "is_system");

            migrationBuilder.CreateIndex(
                name: "ix_custom_fields_key",
                schema: "custom_field",
                table: "custom_fields",
                column: "key",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_issue_field_values_custom_field_id_indexed_date",
                schema: "custom_field",
                table: "issue_field_values",
                columns: new[] { "custom_field_id", "indexed_date" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_field_values_custom_field_id_indexed_number",
                schema: "custom_field",
                table: "issue_field_values",
                columns: new[] { "custom_field_id", "indexed_number" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_field_values_custom_field_id_indexed_string",
                schema: "custom_field",
                table: "issue_field_values",
                columns: new[] { "custom_field_id", "indexed_string" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_field_values_issue_id_custom_field_id",
                schema: "custom_field",
                table: "issue_field_values",
                columns: new[] { "issue_id", "custom_field_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_field_contexts",
                schema: "custom_field");

            migrationBuilder.DropTable(
                name: "custom_field_options",
                schema: "custom_field");

            migrationBuilder.DropTable(
                name: "issue_field_values",
                schema: "custom_field");

            migrationBuilder.DropTable(
                name: "custom_fields",
                schema: "custom_field");
        }
    }
}
