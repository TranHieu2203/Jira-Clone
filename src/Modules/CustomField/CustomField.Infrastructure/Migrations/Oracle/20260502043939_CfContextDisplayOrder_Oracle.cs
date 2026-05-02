using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomField.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(CustomFieldDbContext))]
[Migration("20260502043939_CfContextDisplayOrder_Oracle")]
public partial class CfContextDisplayOrder_Oracle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "display_order",
            table: "custom_field_contexts",
            type: "NUMBER(10)",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            "UPDATE custom_field_contexts SET display_order = 1000 WHERE is_global = 1");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "display_order",
            table: "custom_field_contexts");
    }
}
