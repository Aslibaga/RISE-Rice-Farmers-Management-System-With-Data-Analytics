using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261125165056_AddMonitoringSeasonHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppSettings",
            columns: table => new
            {
                AppSettingId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppSettings", x => x.AppSettingId);
            });

        migrationBuilder.CreateTable(
            name: "UtilizationSeasonRecords",
            columns: table => new
            {
                UtilizationSeasonRecordId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SeasonKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                DistributionItemId = table.Column<int>(type: "int", nullable: false),
                QuantityUsed = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                QuantityStillStocked = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                QuantityNotYetUsed = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                QuantityDisposed = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                MonitoringDate = table.Column<DateOnly>(type: "date", nullable: false),
                Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UtilizationSeasonRecords", x => x.UtilizationSeasonRecordId);
                table.ForeignKey(
                    name: "FK_UtilizationSeasonRecords_DistributionItems_DistributionItemId",
                    column: x => x.DistributionItemId,
                    principalTable: "DistributionItems",
                    principalColumn: "DistributionItemId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "AppSettings",
            columns: new[] { "AppSettingId", "Key", "Value" },
            values: new object[] { 1, "ActiveSeason", "2026-Wet" });

        migrationBuilder.CreateIndex(
            name: "IX_AppSettings_Key",
            table: "AppSettings",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UtilizationSeasonRecords_DistributionItemId",
            table: "UtilizationSeasonRecords",
            column: "DistributionItemId");

        migrationBuilder.CreateIndex(
            name: "IX_UtilizationSeasonRecords_SeasonKey_DistributionItemId",
            table: "UtilizationSeasonRecords",
            columns: new[] { "SeasonKey", "DistributionItemId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AppSettings");

        migrationBuilder.DropTable(
            name: "UtilizationSeasonRecords");
    }
}
