using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomField.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CfContextDisplayOrder_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                schema: "custom_field",
                table: "custom_field_contexts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Global contexts (seed cũ) xếp sau layout theo project (display_order nhỏ hơn = trước).
            migrationBuilder.Sql(
                "UPDATE custom_field.custom_field_contexts SET display_order = 1000 WHERE is_global = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_order",
                schema: "custom_field",
                table: "custom_field_contexts");
        }
    }
}
