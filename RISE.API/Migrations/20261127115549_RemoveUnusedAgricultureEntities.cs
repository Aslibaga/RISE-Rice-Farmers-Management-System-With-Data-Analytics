using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261127115549_RemoveUnusedAgricultureEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CropMonitoringRecords");

        migrationBuilder.DropTable(
            name: "HarvestVerificationRecords");

        migrationBuilder.DropTable(
            name: "Suppliers");

        migrationBuilder.DropTable(
            name: "SupplyUsageRecords");

        migrationBuilder.DropTable(
            name: "HarvestRecords");

        migrationBuilder.DropTable(
            name: "PlantingRecords");

        migrationBuilder.DropCheckConstraint(
            name: "CK_UtilizationMonitoring_NonNegative",
            table: "UtilizationMonitorings");

        migrationBuilder.DropColumn(
            name: "QuantityNotYetUsed",
            table: "UtilizationSeasonRecords");

        migrationBuilder.DropColumn(
            name: "QuantityNotYetUsed",
            table: "UtilizationMonitorings");

        migrationBuilder.AddCheckConstraint(
            name: "CK_UtilizationMonitoring_NonNegative",
            table: "UtilizationMonitorings",
            sql: "([QuantityUsed] IS NULL OR [QuantityUsed] >= 0) AND ([QuantityStillStocked] IS NULL OR [QuantityStillStocked] >= 0) AND ([QuantityDisposed] IS NULL OR [QuantityDisposed] >= 0)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_UtilizationMonitoring_NonNegative",
            table: "UtilizationMonitorings");

        migrationBuilder.AddColumn<decimal>(
            name: "QuantityNotYetUsed",
            table: "UtilizationSeasonRecords",
            type: "decimal(18,3)",
            precision: 18,
            scale: 3,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "QuantityNotYetUsed",
            table: "UtilizationMonitorings",
            type: "decimal(18,3)",
            precision: 18,
            scale: 3,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "PlantingRecords",
            columns: table => new
            {
                PlantingRecordId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SeedDistributionItemId = table.Column<int>(type: "int", nullable: true),
                AreaPlantedHectares = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                CropName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Rice"),
                ExpectedHarvestDate = table.Column<DateOnly>(type: "date", nullable: true),
                PlantingDate = table.Column<DateOnly>(type: "date", nullable: false),
                Season = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Growing"),
                Variety = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlantingRecords", x => x.PlantingRecordId);
                table.CheckConstraint("CK_PlantingRecords_Area_Positive", "[AreaPlantedHectares] > 0");
                table.CheckConstraint("CK_PlantingRecords_ExpectedHarvest", "[ExpectedHarvestDate] IS NULL OR [ExpectedHarvestDate] >= [PlantingDate]");
                table.CheckConstraint("CK_PlantingRecords_Status", "[Status] IN (N'Growing', N'Harvested', N'Failed', N'Cancelled')");
                table.ForeignKey(
                    name: "FK_PlantingRecords_DistributionItems_SeedDistributionItemId",
                    column: x => x.SeedDistributionItemId,
                    principalTable: "DistributionItems",
                    principalColumn: "DistributionItemId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                SupplierId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suppliers", x => x.SupplierId);
            });

        migrationBuilder.CreateTable(
            name: "CropMonitoringRecords",
            columns: table => new
            {
                CropMonitoringRecordId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PlantingRecordId = table.Column<int>(type: "int", nullable: false),
                RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                GrowthStage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                RainfallMillimeters = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CropMonitoringRecords", x => x.CropMonitoringRecordId);
                table.CheckConstraint("CK_CropMonitoringRecords_Rainfall_NonNegative", "[RainfallMillimeters] IS NULL OR [RainfallMillimeters] >= 0");
                table.ForeignKey(
                    name: "FK_CropMonitoringRecords_PlantingRecords_PlantingRecordId",
                    column: x => x.PlantingRecordId,
                    principalTable: "PlantingRecords",
                    principalColumn: "PlantingRecordId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CropMonitoringRecords_Users_RecordedByUserId",
                    column: x => x.RecordedByUserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "HarvestRecords",
            columns: table => new
            {
                HarvestRecordId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PlantingRecordId = table.Column<int>(type: "int", nullable: false),
                Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                HarvestDate = table.Column<DateOnly>(type: "date", nullable: false),
                MoisturePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                QuantityKilograms = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HarvestRecords", x => x.HarvestRecordId);
                table.CheckConstraint("CK_HarvestRecords_Moisture_Range", "[MoisturePercent] IS NULL OR ([MoisturePercent] >= 0 AND [MoisturePercent] <= 100)");
                table.CheckConstraint("CK_HarvestRecords_Quantity_Positive", "[QuantityKilograms] > 0");
                table.ForeignKey(
                    name: "FK_HarvestRecords_PlantingRecords_PlantingRecordId",
                    column: x => x.PlantingRecordId,
                    principalTable: "PlantingRecords",
                    principalColumn: "PlantingRecordId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SupplyUsageRecords",
            columns: table => new
            {
                SupplyUsageRecordId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DistributionItemId = table.Column<int>(type: "int", nullable: true),
                PlantingRecordId = table.Column<int>(type: "int", nullable: false),
                RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                SupplyId = table.Column<int>(type: "int", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                QuantityUsed = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                UsageDate = table.Column<DateOnly>(type: "date", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplyUsageRecords", x => x.SupplyUsageRecordId);
                table.CheckConstraint("CK_SupplyUsageRecords_Quantity_Positive", "[QuantityUsed] > 0");
                table.ForeignKey(
                    name: "FK_SupplyUsageRecords_DistributionItems_DistributionItemId",
                    column: x => x.DistributionItemId,
                    principalTable: "DistributionItems",
                    principalColumn: "DistributionItemId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplyUsageRecords_PlantingRecords_PlantingRecordId",
                    column: x => x.PlantingRecordId,
                    principalTable: "PlantingRecords",
                    principalColumn: "PlantingRecordId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplyUsageRecords_Supplies_SupplyId",
                    column: x => x.SupplyId,
                    principalTable: "Supplies",
                    principalColumn: "SupplyId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupplyUsageRecords_Users_RecordedByUserId",
                    column: x => x.RecordedByUserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "HarvestVerificationRecords",
            columns: table => new
            {
                HarvestVerificationRecordId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                HarvestRecordId = table.Column<int>(type: "int", nullable: false),
                VerifiedByUserId = table.Column<int>(type: "int", nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                VerifiedQuantityKilograms = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HarvestVerificationRecords", x => x.HarvestVerificationRecordId);
                table.CheckConstraint("CK_HarvestVerificationRecords_Quantity_NonNegative", "[VerifiedQuantityKilograms] IS NULL OR [VerifiedQuantityKilograms] >= 0");
                table.CheckConstraint("CK_HarvestVerificationRecords_Status", "[Status] IN (N'Pending', N'Verified', N'Rejected')");
                table.ForeignKey(
                    name: "FK_HarvestVerificationRecords_HarvestRecords_HarvestRecordId",
                    column: x => x.HarvestRecordId,
                    principalTable: "HarvestRecords",
                    principalColumn: "HarvestRecordId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_HarvestVerificationRecords_Users_VerifiedByUserId",
                    column: x => x.VerifiedByUserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddCheckConstraint(
            name: "CK_UtilizationMonitoring_NonNegative",
            table: "UtilizationMonitorings",
            sql: "([QuantityUsed] IS NULL OR [QuantityUsed] >= 0) AND ([QuantityStillStocked] IS NULL OR [QuantityStillStocked] >= 0) AND ([QuantityNotYetUsed] IS NULL OR [QuantityNotYetUsed] >= 0) AND ([QuantityDisposed] IS NULL OR [QuantityDisposed] >= 0)");

        migrationBuilder.CreateIndex(
            name: "IX_CropMonitoringRecords_PlantingRecordId_ObservedAtUtc",
            table: "CropMonitoringRecords",
            columns: new[] { "PlantingRecordId", "ObservedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_CropMonitoringRecords_RecordedByUserId",
            table: "CropMonitoringRecords",
            column: "RecordedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_HarvestRecords_HarvestDate",
            table: "HarvestRecords",
            column: "HarvestDate");

        migrationBuilder.CreateIndex(
            name: "IX_HarvestRecords_PlantingRecordId_HarvestDate",
            table: "HarvestRecords",
            columns: new[] { "PlantingRecordId", "HarvestDate" });

        migrationBuilder.CreateIndex(
            name: "IX_HarvestVerificationRecords_HarvestRecordId",
            table: "HarvestVerificationRecords",
            column: "HarvestRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_HarvestVerificationRecords_VerifiedByUserId",
            table: "HarvestVerificationRecords",
            column: "VerifiedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_PlantingRecords_PlantingDate",
            table: "PlantingRecords",
            column: "PlantingDate");

        migrationBuilder.CreateIndex(
            name: "IX_PlantingRecords_SeedDistributionItemId",
            table: "PlantingRecords",
            column: "SeedDistributionItemId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplyUsageRecords_DistributionItemId",
            table: "SupplyUsageRecords",
            column: "DistributionItemId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplyUsageRecords_PlantingRecordId_UsageDate",
            table: "SupplyUsageRecords",
            columns: new[] { "PlantingRecordId", "UsageDate" });

        migrationBuilder.CreateIndex(
            name: "IX_SupplyUsageRecords_RecordedByUserId",
            table: "SupplyUsageRecords",
            column: "RecordedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplyUsageRecords_SupplyId",
            table: "SupplyUsageRecords",
            column: "SupplyId");
    }
}
