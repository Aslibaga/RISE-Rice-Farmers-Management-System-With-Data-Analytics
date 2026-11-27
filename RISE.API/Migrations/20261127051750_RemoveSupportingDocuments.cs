using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261127051750_RemoveSupportingDocuments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SupportingDocuments");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SupportingDocuments",
            columns: table => new
            {
                SupportingDocumentId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DistributionId = table.Column<int>(type: "int", nullable: true),
                FarmerId = table.Column<int>(type: "int", nullable: true),
                HarvestRecordId = table.Column<int>(type: "int", nullable: true),
                PlantingRecordId = table.Column<int>(type: "int", nullable: true),
                UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupportingDocuments", x => x.SupportingDocumentId);
                table.CheckConstraint("CK_SupportingDocuments_FileSize_NonNegative", "[FileSizeBytes] >= 0");
                table.CheckConstraint("CK_SupportingDocuments_OneParent", "((CASE WHEN [FarmerId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DistributionId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [PlantingRecordId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [HarvestRecordId] IS NULL THEN 0 ELSE 1 END)) = 1");
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
            name: "IX_SupportingDocuments_DistributionId",
            table: "SupportingDocuments",
            column: "DistributionId");

        migrationBuilder.CreateIndex(
            name: "IX_SupportingDocuments_FarmerId",
            table: "SupportingDocuments",
            column: "FarmerId");

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
    }
}
