using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261127053647_RemoveEmptyUnusedColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_DistributionItems_UnitCost_NonNegative",
            table: "DistributionItems");

        migrationBuilder.DropColumn(
            name: "Address",
            table: "Suppliers");

        migrationBuilder.DropColumn(
            name: "ContactPerson",
            table: "Suppliers");

        migrationBuilder.DropColumn(
            name: "Email",
            table: "Suppliers");

        migrationBuilder.DropColumn(
            name: "PhoneNumber",
            table: "Suppliers");

        migrationBuilder.DropColumn(
            name: "Remarks",
            table: "HarvestVerificationRecords");

        migrationBuilder.DropColumn(
            name: "LotNumber",
            table: "DistributionItems");

        migrationBuilder.DropColumn(
            name: "UnitCost",
            table: "DistributionItems");

        migrationBuilder.DropColumn(
            name: "CropHealth",
            table: "CropMonitoringRecords");

        migrationBuilder.DropColumn(
            name: "Notes",
            table: "CropMonitoringRecords");

        migrationBuilder.DropColumn(
            name: "PestOrDisease",
            table: "CropMonitoringRecords");

        migrationBuilder.DropColumn(
            name: "Description",
            table: "AssistancePrograms");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Address",
            table: "Suppliers",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ContactPerson",
            table: "Suppliers",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Email",
            table: "Suppliers",
            type: "nvarchar(254)",
            maxLength: 254,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PhoneNumber",
            table: "Suppliers",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Remarks",
            table: "HarvestVerificationRecords",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LotNumber",
            table: "DistributionItems",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "UnitCost",
            table: "DistributionItems",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "CropHealth",
            table: "CropMonitoringRecords",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Notes",
            table: "CropMonitoringRecords",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PestOrDisease",
            table: "CropMonitoringRecords",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "AssistancePrograms",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_DistributionItems_UnitCost_NonNegative",
            table: "DistributionItems",
            sql: "[UnitCost] >= 0");
    }
}
