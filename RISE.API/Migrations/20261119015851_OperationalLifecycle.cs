using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261119015851_OperationalLifecycle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AssistancePrograms",
            columns: table => new
            {
                AssistanceProgramId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssistancePrograms", x => x.AssistanceProgramId);
                table.CheckConstraint("CK_AssistancePrograms_DateRange", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
            });

        migrationBuilder.CreateTable(
            name: "Farms",
            columns: table => new
            {
                FarmId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FarmerId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Barangay = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Municipality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                AreaHectares = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                SoilType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Farms", x => x.FarmId);
                table.CheckConstraint("CK_Farms_AreaHectares_Positive", "[AreaHectares] > 0");
                table.ForeignKey(
                    name: "FK_Farms_Farmers_FarmerId",
                    column: x => x.FarmerId,
                    principalTable: "Farmers",
                    principalColumn: "FarmerId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                RoleId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.RoleId);
            });

        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                SupplierId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ContactPerson = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suppliers", x => x.SupplierId);
            });

        migrationBuilder.CreateTable(
            name: "SupplyCategories",
            columns: table => new
            {
                SupplyCategoryId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplyCategories", x => x.SupplyCategoryId);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.UserId);
            });

        migrationBuilder.CreateTable(
            name: "Supplies",
            columns: table => new
            {
                SupplyId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SupplyCategoryId = table.Column<int>(type: "int", nullable: false),
                SupplierId = table.Column<int>(type: "int", nullable: false),
                Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                UnitOfMeasure = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                StockQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Supplies", x => x.SupplyId);
                table.CheckConstraint("CK_Supplies_StockQuantity_NonNegative", "[StockQuantity] >= 0");
                table.CheckConstraint("CK_Supplies_UnitCost_NonNegative", "[UnitCost] >= 0");
                table.ForeignKey(
                    name: "FK_Supplies_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "SupplierId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Supplies_SupplyCategories_SupplyCategoryId",
                    column: x => x.SupplyCategoryId,
                    principalTable: "SupplyCategories",
                    principalColumn: "SupplyCategoryId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                AuditLogId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: true),
                Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                EntityName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                table.ForeignKey(
                    name: "FK_AuditLogs_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Distributions",
            columns: table => new
            {
                DistributionId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FarmerId = table.Column<int>(type: "int", nullable: false),
                AssistanceProgramId = table.Column<int>(type: "int", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                ReferenceNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                DistributionDate = table.Column<DateOnly>(type: "date", nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Distributed"),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Distributions", x => x.DistributionId);
                table.CheckConstraint("CK_Distributions_Status", "[Status] IN (N'Draft', N'Approved', N'Distributed', N'Cancelled')");
                table.ForeignKey(
                    name: "FK_Distributions_AssistancePrograms_AssistanceProgramId",
                    column: x => x.AssistanceProgramId,
                    principalTable: "AssistancePrograms",
                    principalColumn: "AssistanceProgramId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Distributions_Farmers_FarmerId",
                    column: x => x.FarmerId,
                    principalTable: "Farmers",
                    principalColumn: "FarmerId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Distributions_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false),
                RoleId = table.Column<int>(type: "int", nullable: false),
                AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "RoleId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DistributionItems",
            columns: table => new
            {
                DistributionItemId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DistributionId = table.Column<int>(type: "int", nullable: false),
                SupplyId = table.Column<int>(type: "int", nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DistributionItems", x => x.DistributionItemId);
                table.CheckConstraint("CK_DistributionItems_Quantity_Positive", "[Quantity] > 0");
                table.CheckConstraint("CK_DistributionItems_UnitCost_NonNegative", "[UnitCost] >= 0");
                table.ForeignKey(
                    name: "FK_DistributionItems_Distributions_DistributionId",
                    column: x => x.DistributionId,
                    principalTable: "Distributions",
                    principalColumn: "DistributionId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DistributionItems_Supplies_SupplyId",
                    column: x => x.SupplyId,
                    principalTable: "Supplies",
                    principalColumn: "SupplyId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PlantingRecords",
            columns: table => new
            {
                PlantingRecordId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FarmId = table.Column<int>(type: "int", nullable: false),
                SeedDistributionItemId = table.Column<int>(type: "int", nullable: true),
                CropName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Rice"),
                Variety = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Season = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                PlantingDate = table.Column<DateOnly>(type: "date", nullable: false),
                AreaPlantedHectares = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                ExpectedHarvestDate = table.Column<DateOnly>(type: "date", nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Growing")
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
                table.ForeignKey(
                    name: "FK_PlantingRecords_Farms_FarmId",
                    column: x => x.FarmId,
                    principalTable: "Farms",
                    principalColumn: "FarmId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CropMonitoringRecords",
            columns: table => new
            {
                CropMonitoringRecordId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PlantingRecordId = table.Column<int>(type: "int", nullable: false),
                RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                GrowthStage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CropHealth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                PestOrDisease = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RainfallMillimeters = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
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
                HarvestDate = table.Column<DateOnly>(type: "date", nullable: false),
                QuantityKilograms = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                MoisturePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
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
                PlantingRecordId = table.Column<int>(type: "int", nullable: false),
                SupplyId = table.Column<int>(type: "int", nullable: false),
                DistributionItemId = table.Column<int>(type: "int", nullable: true),
                RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                QuantityUsed = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
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
                VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                VerifiedQuantityKilograms = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
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

        migrationBuilder.CreateTable(
            name: "SupportingDocuments",
            columns: table => new
            {
                SupportingDocumentId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FarmerId = table.Column<int>(type: "int", nullable: true),
                FarmId = table.Column<int>(type: "int", nullable: true),
                DistributionId = table.Column<int>(type: "int", nullable: true),
                PlantingRecordId = table.Column<int>(type: "int", nullable: true),
                HarvestRecordId = table.Column<int>(type: "int", nullable: true),
                UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupportingDocuments", x => x.SupportingDocumentId);
                table.CheckConstraint("CK_SupportingDocuments_FileSize_NonNegative", "[FileSizeBytes] >= 0");
                table.CheckConstraint("CK_SupportingDocuments_OneParent", "((CASE WHEN [FarmerId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [FarmId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DistributionId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [PlantingRecordId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [HarvestRecordId] IS NULL THEN 0 ELSE 1 END)) = 1");
                table.ForeignKey(
                    name: "FK_SupportingDocuments_Distributions_DistributionId",
                    column: x => x.DistributionId,
                    principalTable: "Distributions",
                    principalColumn: "DistributionId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupportingDocuments_Farmers_FarmerId",
                    column: x => x.FarmerId,
                    principalTable: "Farmers",
                    principalColumn: "FarmerId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupportingDocuments_Farms_FarmId",
                    column: x => x.FarmId,
                    principalTable: "Farms",
                    principalColumn: "FarmId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupportingDocuments_HarvestRecords_HarvestRecordId",
                    column: x => x.HarvestRecordId,
                    principalTable: "HarvestRecords",
                    principalColumn: "HarvestRecordId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupportingDocuments_PlantingRecords_PlantingRecordId",
                    column: x => x.PlantingRecordId,
                    principalTable: "PlantingRecords",
                    principalColumn: "PlantingRecordId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SupportingDocuments_Users_UploadedByUserId",
                    column: x => x.UploadedByUserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AssistancePrograms_Name",
            table: "AssistancePrograms",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_EntityName_EntityId",
            table: "AuditLogs",
            columns: new[] { "EntityName", "EntityId" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_OccurredAtUtc",
            table: "AuditLogs",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId",
            table: "AuditLogs",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_CropMonitoringRecords_PlantingRecordId_ObservedAtUtc",
            table: "CropMonitoringRecords",
            columns: new[] { "PlantingRecordId", "ObservedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_CropMonitoringRecords_RecordedByUserId",
            table: "CropMonitoringRecords",
            column: "RecordedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DistributionItems_DistributionId_SupplyId",
            table: "DistributionItems",
            columns: new[] { "DistributionId", "SupplyId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DistributionItems_SupplyId",
            table: "DistributionItems",
            column: "SupplyId");

        migrationBuilder.CreateIndex(
            name: "IX_Distributions_AssistanceProgramId",
            table: "Distributions",
            column: "AssistanceProgramId");

        migrationBuilder.CreateIndex(
            name: "IX_Distributions_CreatedByUserId",
            table: "Distributions",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Distributions_DistributionDate",
            table: "Distributions",
            column: "DistributionDate");

        migrationBuilder.CreateIndex(
            name: "IX_Distributions_FarmerId",
            table: "Distributions",
            column: "FarmerId");

        migrationBuilder.CreateIndex(
            name: "IX_Distributions_ReferenceNumber",
            table: "Distributions",
            column: "ReferenceNumber",
            unique: true,
            filter: "[ReferenceNumber] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Farms_FarmerId",
            table: "Farms",
            column: "FarmerId");

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
            name: "IX_PlantingRecords_FarmId",
            table: "PlantingRecords",
            column: "FarmId");

        migrationBuilder.CreateIndex(
            name: "IX_PlantingRecords_PlantingDate",
            table: "PlantingRecords",
            column: "PlantingDate");

        migrationBuilder.CreateIndex(
            name: "IX_PlantingRecords_SeedDistributionItemId",
            table: "PlantingRecords",
            column: "SeedDistributionItemId");

        migrationBuilder.CreateIndex(
            name: "IX_Roles_Name",
            table: "Roles",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Supplies_Code",
            table: "Supplies",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Supplies_SupplierId",
            table: "Supplies",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_Supplies_SupplyCategoryId",
            table: "Supplies",
            column: "SupplyCategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplyCategories_Name",
            table: "SupplyCategories",
            column: "Name",
            unique: true);

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

        migrationBuilder.CreateIndex(
            name: "IX_SupportingDocuments_DistributionId",
            table: "SupportingDocuments",
            column: "DistributionId");

        migrationBuilder.CreateIndex(
            name: "IX_SupportingDocuments_FarmerId",
            table: "SupportingDocuments",
            column: "FarmerId");

        migrationBuilder.CreateIndex(
            name: "IX_SupportingDocuments_FarmId",
            table: "SupportingDocuments",
            column: "FarmId");

        migrationBuilder.CreateIndex(
            name: "IX_SupportingDocuments_HarvestRecordId",
            table: "SupportingDocuments",
            column: "HarvestRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_SupportingDocuments_PlantingRecordId",
            table: "SupportingDocuments",
            column: "PlantingRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_SupportingDocuments_UploadedByUserId",
            table: "SupportingDocuments",
            column: "UploadedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            table: "UserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_UserName",
            table: "Users",
            column: "UserName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditLogs");

        migrationBuilder.DropTable(
            name: "CropMonitoringRecords");

        migrationBuilder.DropTable(
            name: "HarvestVerificationRecords");

        migrationBuilder.DropTable(
            name: "SupplyUsageRecords");

        migrationBuilder.DropTable(
            name: "SupportingDocuments");

        migrationBuilder.DropTable(
            name: "UserRoles");

        migrationBuilder.DropTable(
            name: "HarvestRecords");

        migrationBuilder.DropTable(
            name: "Roles");

        migrationBuilder.DropTable(
            name: "PlantingRecords");

        migrationBuilder.DropTable(
            name: "DistributionItems");

        migrationBuilder.DropTable(
            name: "Farms");

        migrationBuilder.DropTable(
            name: "Distributions");

        migrationBuilder.DropTable(
            name: "Supplies");

        migrationBuilder.DropTable(
            name: "AssistancePrograms");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropTable(
            name: "Suppliers");

        migrationBuilder.DropTable(
            name: "SupplyCategories");
    }
}
