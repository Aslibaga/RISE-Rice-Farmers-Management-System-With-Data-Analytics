using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261118155932_RepairFarmerDatabaseConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "FullName",
            table: "Farmers",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "ContactNumber",
            table: "Farmers",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "Address",
            table: "Farmers",
            type: "nvarchar(300)",
            maxLength: 300,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_FullName",
            table: "Farmers",
            column: "FullName");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_FullName_ContactNumber",
            table: "Farmers",
            columns: new[] { "FullName", "ContactNumber" },
            unique: true,
            filter: "[IsActive] = 1");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Farmers_FullName",
            table: "Farmers");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_FullName_ContactNumber",
            table: "Farmers");

        migrationBuilder.AlterColumn<string>(
            name: "FullName",
            table: "Farmers",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(150)",
            oldMaxLength: 150);

        migrationBuilder.AlterColumn<string>(
            name: "ContactNumber",
            table: "Farmers",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(30)",
            oldMaxLength: 30);

        migrationBuilder.AlterColumn<string>(
            name: "Address",
            table: "Farmers",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(300)",
            oldMaxLength: 300);
    }
}
