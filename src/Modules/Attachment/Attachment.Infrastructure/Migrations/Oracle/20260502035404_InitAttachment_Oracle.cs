using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attachment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitAttachment_Oracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issue_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    issue_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    file_name = table.Column<string>(type: "NVARCHAR2(260)", maxLength: 260, nullable: false),
                    content_type = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    storage_key = table.Column<string>(type: "NVARCHAR2(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    created_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    updated_by = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true),
                    created_trace_id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_attachments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_attachments_issue_id",
                table: "issue_attachments",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attachments_issue_id_created_at",
                table: "issue_attachments",
                columns: new[] { "issue_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_attachments_storage_key",
                table: "issue_attachments",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_attachments");
        }
    }
}
