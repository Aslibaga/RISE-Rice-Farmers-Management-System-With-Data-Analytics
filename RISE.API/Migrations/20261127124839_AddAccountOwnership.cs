using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261127124839_AddAccountOwnership : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SupplyCategories_Name",
            table: "SupplyCategories");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_FullName_ContactNumber",
            table: "Farmers");

        migrationBuilder.AddColumn<int>(
            name: "OwnerUserId",
            table: "SupplyCategories",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "OwnerUserId",
            table: "Supplies",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "OwnerUserId",
            table: "Farmers",
            type: "int",
            nullable: false,
            defaultValue: 0);

        // Existing records belong to the first (administrator) account.
        migrationBuilder.Sql("UPDATE [Farmers] SET [OwnerUserId] = ISNULL((SELECT TOP(1) [UserId] FROM [Users] ORDER BY [UserId]), 0)");
        migrationBuilder.Sql("UPDATE [Supplies] SET [OwnerUserId] = ISNULL((SELECT TOP(1) [UserId] FROM [Users] ORDER BY [UserId]), 0)");
        migrationBuilder.Sql("UPDATE [SupplyCategories] SET [OwnerUserId] = ISNULL((SELECT TOP(1) [UserId] FROM [Users] ORDER BY [UserId]), 0)");

        migrationBuilder.CreateIndex(
            name: "IX_SupplyCategories_Name_OwnerUserId",
            table: "SupplyCategories",
            columns: new[] { "Name", "OwnerUserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SupplyCategories_OwnerUserId",
            table: "SupplyCategories",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Supplies_OwnerUserId",
            table: "Supplies",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_FullName_ContactNumber_OwnerUserId",
            table: "Farmers",
            columns: new[] { "FullName", "ContactNumber", "OwnerUserId" },
            unique: true,
            filter: "[IsActive] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_OwnerUserId",
            table: "Farmers",
            column: "OwnerUserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SupplyCategories_Name_OwnerUserId",
            table: "SupplyCategories");

        migrationBuilder.DropIndex(
            name: "IX_SupplyCategories_OwnerUserId",
            table: "SupplyCategories");

        migrationBuilder.DropIndex(
            name: "IX_Supplies_OwnerUserId",
            table: "Supplies");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_FullName_ContactNumber_OwnerUserId",
            table: "Farmers");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_OwnerUserId",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "OwnerUserId",
            table: "SupplyCategories");

        migrationBuilder.DropColumn(
            name: "OwnerUserId",
            table: "Supplies");

        migrationBuilder.DropColumn(
            name: "OwnerUserId",
            table: "Farmers");

        migrationBuilder.CreateIndex(
            name: "IX_SupplyCategories_Name",
            table: "SupplyCategories",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_FullName_ContactNumber",
            table: "Farmers",
            columns: new[] { "FullName", "ContactNumber" },
            unique: true,
            filter: "[IsActive] = 1");
    }
}
