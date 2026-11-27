using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261127133225_RemoveSupplyUnitCost : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Supplies_UnitCost_NonNegative",
            table: "Supplies");

        migrationBuilder.DropColumn(
            name: "UnitCost",
            table: "Supplies");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "UnitCost",
            table: "Supplies",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Supplies_UnitCost_NonNegative",
            table: "Supplies",
            sql: "[UnitCost] >= 0");
    }
}
