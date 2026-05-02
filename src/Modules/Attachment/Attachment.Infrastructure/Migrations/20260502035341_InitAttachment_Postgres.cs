using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attachment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitAttachment_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "attachment");

            migrationBuilder.CreateTable(
                name: "issue_attachments",
                schema: "attachment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_attachments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_attachments_issue_id",
                schema: "attachment",
                table: "issue_attachments",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attachments_issue_id_created_at",
                schema: "attachment",
                table: "issue_attachments",
                columns: new[] { "issue_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_attachments_storage_key",
                schema: "attachment",
                table: "issue_attachments",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_attachments",
                schema: "attachment");
        }
    }
}
