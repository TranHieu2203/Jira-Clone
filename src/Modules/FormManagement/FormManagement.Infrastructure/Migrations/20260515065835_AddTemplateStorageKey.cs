using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateStorageKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                schema: "form_mgmt",
                table: "templates",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "storage_key",
                schema: "form_mgmt",
                table: "templates");
        }
    }
}
