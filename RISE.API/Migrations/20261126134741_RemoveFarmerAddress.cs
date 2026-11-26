using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261126134741_RemoveFarmerAddress : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Address",
            table: "Farmers");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Address",
            table: "Farmers",
            type: "nvarchar(300)",
            maxLength: 300,
            nullable: false,
            defaultValue: "");
    }
}
