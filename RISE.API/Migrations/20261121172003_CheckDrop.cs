using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261121172003_CheckDrop : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Supplies_Suppliers_SupplierId",
            table: "Supplies");

        migrationBuilder.DropIndex(
            name: "IX_Supplies_Code",
            table: "Supplies");

        migrationBuilder.DropIndex(
            name: "IX_Supplies_SupplierId",
            table: "Supplies");

        migrationBuilder.DropColumn(
            name: "Code",
            table: "Supplies");

        migrationBuilder.DropColumn(
            name: "SupplierId",
            table: "Supplies");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SupplierId",
            table: "Supplies",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Supplies_SupplierId",
            table: "Supplies",
            column: "SupplierId");

        migrationBuilder.AddForeignKey(
            name: "FK_Supplies_Suppliers_SupplierId",
            table: "Supplies",
            column: "SupplierId",
            principalTable: "Suppliers",
            principalColumn: "SupplierId");
    }
}
