using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261119020250_DraftDistributionByDefault : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Status",
            table: "Distributions",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Draft",
            oldClrType: typeof(string),
            oldType: "nvarchar(20)",
            oldMaxLength: 20,
            oldDefaultValue: "Distributed");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Status",
            table: "Distributions",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Distributed",
            oldClrType: typeof(string),
            oldType: "nvarchar(20)",
            oldMaxLength: 20,
            oldDefaultValue: "Draft");
    }
}
