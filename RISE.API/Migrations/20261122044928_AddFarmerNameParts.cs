using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261122044928_AddFarmerNameParts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FirstName",
            table: "Farmers",
            type: "nvarchar(60)",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LastName",
            table: "Farmers",
            type: "nvarchar(60)",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MiddleName",
            table: "Farmers",
            type: "nvarchar(60)",
            maxLength: 60,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FirstName",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "LastName",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "MiddleName",
            table: "Farmers");
    }
}
