using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261125165216_FixSeasonRecordPrecision : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityUsed",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,3)",
            precision: 18,
            scale: 3,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityStillStocked",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,3)",
            precision: 18,
            scale: 3,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityNotYetUsed",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,3)",
            precision: 18,
            scale: 3,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityDisposed",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,3)",
            precision: 18,
            scale: 3,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityUsed",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,3)",
            oldPrecision: 18,
            oldScale: 3,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityStillStocked",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,3)",
            oldPrecision: 18,
            oldScale: 3,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityNotYetUsed",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,3)",
            oldPrecision: 18,
            oldScale: 3,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "QuantityDisposed",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,3)",
            oldPrecision: 18,
            oldScale: 3,
            oldNullable: true);
    }
}
