using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261126111723_RemoveUnusedEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Farmers_FarmerAssociations_FarmerAssociationId",
            table: "Farmers");

        migrationBuilder.DropForeignKey(
            name: "FK_PlantingRecords_Farms_FarmId",
            table: "PlantingRecords");

        migrationBuilder.DropForeignKey(
            name: "FK_SupportingDocuments_Farms_FarmId",
            table: "SupportingDocuments");

        migrationBuilder.DropTable(
            name: "FarmerAssociations");

        migrationBuilder.DropTable(
            name: "Farms");

        migrationBuilder.DropIndex(
            name: "IX_SupportingDocuments_FarmId",
            table: "SupportingDocuments");

        migrationBuilder.DropCheckConstraint(
            name: "CK_SupportingDocuments_OneParent",
            table: "SupportingDocuments");

        migrationBuilder.DropIndex(
            name: "IX_PlantingRecords_FarmId",
            table: "PlantingRecords");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_FarmerAssociationId",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "FarmId",
            table: "SupportingDocuments");

        migrationBuilder.DropColumn(
            name: "Description",
            table: "Supplies");

        migrationBuilder.DropColumn(
            name: "UpdatedAtUtc",
            table: "Supplies");

        migrationBuilder.DropColumn(
            name: "FarmId",
            table: "PlantingRecords");

        migrationBuilder.DropColumn(
            name: "FarmerAssociationId",
            table: "Farmers");

        migrationBuilder.AddCheckConstraint(
            name: "CK_SupportingDocuments_OneParent",
            table: "SupportingDocuments",
            sql: "((CASE WHEN [FarmerId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DistributionId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [PlantingRecordId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [HarvestRecordId] IS NULL THEN 0 ELSE 1 END)) = 1");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_SupportingDocuments_OneParent",
            table: "SupportingDocuments");

        migrationBuilder.AddColumn<int>(
            name: "FarmId",
            table: "SupportingDocuments",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "Supplies",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAtUtc",
            table: "Supplies",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "FarmId",
            table: "PlantingRecords",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "FarmerAssociationId",
            table: "Farmers",
            type: "int",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "FarmerAssociations",
            columns: table => new
            {
                FarmerAssociationId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FarmerAssociations", x => x.FarmerAssociationId);
            });

        migrationBuilder.CreateTable(
            name: "Farms",
            columns: table => new
            {
                FarmId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FarmerId = table.Column<int>(type: "int", nullable: false),
                AreaHectares = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                Barangay = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                Municipality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                SoilType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
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

        migrationBuilder.CreateIndex(
            name: "IX_SupportingDocuments_FarmId",
            table: "SupportingDocuments",
            column: "FarmId");

        migrationBuilder.AddCheckConstraint(
            name: "CK_SupportingDocuments_OneParent",
            table: "SupportingDocuments",
            sql: "((CASE WHEN [FarmerId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [FarmId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DistributionId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [PlantingRecordId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [HarvestRecordId] IS NULL THEN 0 ELSE 1 END)) = 1");

        migrationBuilder.CreateIndex(
            name: "IX_PlantingRecords_FarmId",
            table: "PlantingRecords",
            column: "FarmId");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_FarmerAssociationId",
            table: "Farmers",
            column: "FarmerAssociationId");

        migrationBuilder.CreateIndex(
            name: "IX_FarmerAssociations_Name",
            table: "FarmerAssociations",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Farms_FarmerId",
            table: "Farms",
            column: "FarmerId");

        migrationBuilder.AddForeignKey(
            name: "FK_Farmers_FarmerAssociations_FarmerAssociationId",
            table: "Farmers",
            column: "FarmerAssociationId",
            principalTable: "FarmerAssociations",
            principalColumn: "FarmerAssociationId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PlantingRecords_Farms_FarmId",
            table: "PlantingRecords",
            column: "FarmId",
            principalTable: "Farms",
            principalColumn: "FarmId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_SupportingDocuments_Farms_FarmId",
            table: "SupportingDocuments",
            column: "FarmId",
            principalTable: "Farms",
            principalColumn: "FarmId",
            onDelete: ReferentialAction.Restrict);
    }
}
