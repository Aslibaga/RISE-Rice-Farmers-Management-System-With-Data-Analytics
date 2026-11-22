using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RISE.API.Data;

#nullable disable

namespace RISE.API.Migrations;

[DbContext(typeof(RiseDbContext))]
[Migration("20261119150000_UtilizationMonitoring")]
public partial class UtilizationMonitoringMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UtilizationMonitorings",
            columns: table => new
            {
                UtilizationMonitoringId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DistributionItemId = table.Column<int>(type: "int", nullable: false),
                QuantityUsed = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                QuantityStillStocked = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                QuantityNotYetUsed = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                QuantityDisposed = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                MonitoringDate = table.Column<DateOnly>(type: "date", nullable: false),
                Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UtilizationMonitorings", x => x.UtilizationMonitoringId);
                table.ForeignKey("FK_UtilizationMonitorings_DistributionItems_DistributionItemId",
                    x => x.DistributionItemId, "DistributionItems", "DistributionItemId",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_UtilizationMonitoring_NonNegative",
                    "([QuantityUsed] IS NULL OR [QuantityUsed] >= 0) AND ([QuantityStillStocked] IS NULL OR [QuantityStillStocked] >= 0) AND ([QuantityNotYetUsed] IS NULL OR [QuantityNotYetUsed] >= 0) AND ([QuantityDisposed] IS NULL OR [QuantityDisposed] >= 0)");
            });
        migrationBuilder.CreateIndex(
            name: "IX_UtilizationMonitorings_DistributionItemId",
            table: "UtilizationMonitorings", column: "DistributionItemId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UtilizationMonitorings");
    }
}
