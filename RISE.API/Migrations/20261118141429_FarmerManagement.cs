using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261118141429_FarmerManagement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AssociationName",
            table: "Farmers",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Barangay",
            table: "Farmers",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "Farmers",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "Municipality",
            table: "Farmers",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_AssociationName",
            table: "Farmers",
            column: "AssociationName");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_Barangay",
            table: "Farmers",
            column: "Barangay");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_IsActive",
            table: "Farmers",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_Municipality",
            table: "Farmers",
            column: "Municipality");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Farmers_AssociationName",
            table: "Farmers");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_Barangay",
            table: "Farmers");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_IsActive",
            table: "Farmers");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_Municipality",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "AssociationName",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "Barangay",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "IsActive",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "Municipality",
            table: "Farmers");
    }
}
