using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitFormManagement_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    value = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    type = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    field_group = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    description = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    validation_json = table.Column<string>(type: "CLOB", nullable: true),
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
                    table.PrimaryKey("pk_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    template_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    template_version = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    data_json = table.Column<string>(type: "CLOB", nullable: false),
                    output_path = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    export_format = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    code = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    category = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    sfdt_content = table.Column<string>(type: "CLOB", nullable: false),
                    docx_bytes = table.Column<byte[]>(type: "BLOB", nullable: true),
                    used_fields_json = table.Column<string>(type: "CLOB", nullable: false),
                    version = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    status = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
                    table.PrimaryKey("pk_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_metadata_field_group",
                table: "metadata",
                column: "field_group");

            migrationBuilder.CreateIndex(
                name: "ix_metadata_value",
                table: "metadata",
                column: "value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_submissions_template_id",
                table: "submissions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_templates_category",
                table: "templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_templates_code",
                table: "templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_templates_status",
                table: "templates",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metadata");

            migrationBuilder.DropTable(
                name: "submissions");

            migrationBuilder.DropTable(
                name: "templates");
        }
    }
}
