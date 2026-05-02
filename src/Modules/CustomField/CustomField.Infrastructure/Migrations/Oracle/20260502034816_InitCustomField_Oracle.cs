using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomField.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitCustomField_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "custom_fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    key = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    type = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_system = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_searchable = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    config_json = table.Column<string>(type: "CLOB", nullable: false),
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
                    table.PrimaryKey("pk_custom_fields", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_field_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    custom_field_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    value_json = table.Column<string>(type: "CLOB", nullable: false),
                    indexed_string = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    indexed_number = table.Column<decimal>(type: "DECIMAL(28,8)", precision: 28, scale: 8, nullable: true),
                    indexed_date = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_field_values", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custom_field_contexts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    custom_field_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    is_global = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_required = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    default_value_json = table.Column<string>(type: "CLOB", nullable: true),
                    project_ids = table.Column<string>(type: "CLOB", nullable: false),
                    issue_type_ids = table.Column<string>(type: "CLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_field_contexts", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_field_contexts_custom_fields_custom_field_id",
                        column: x => x.custom_field_id,
                        principalTable: "custom_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "custom_field_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    custom_field_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    parent_option_id = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    value = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    label = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    order = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    is_disabled = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_field_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_field_options_custom_fields_custom_field_id",
                        column: x => x.custom_field_id,
                        principalTable: "custom_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_custom_field_contexts_custom_field_id",
                table: "custom_field_contexts",
                column: "custom_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_custom_field_options_custom_field_id_parent_option_id_value",
                table: "custom_field_options",
                columns: new[] { "custom_field_id", "parent_option_id", "value" },
                unique: true,
                filter: "\"parent_option_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_custom_fields_is_system",
                table: "custom_fields",
                column: "is_system");

            migrationBuilder.CreateIndex(
                name: "ix_custom_fields_key",
                table: "custom_fields",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_issue_field_values_custom_field_id_indexed_date",
                table: "issue_field_values",
                columns: new[] { "custom_field_id", "indexed_date" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_field_values_custom_field_id_indexed_number",
                table: "issue_field_values",
                columns: new[] { "custom_field_id", "indexed_number" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_field_values_custom_field_id_indexed_string",
                table: "issue_field_values",
                columns: new[] { "custom_field_id", "indexed_string" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_field_values_issue_id_custom_field_id",
                table: "issue_field_values",
                columns: new[] { "issue_id", "custom_field_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_field_contexts");

            migrationBuilder.DropTable(
                name: "custom_field_options");

            migrationBuilder.DropTable(
                name: "issue_field_values");

            migrationBuilder.DropTable(
                name: "custom_fields");
        }
    }
}
