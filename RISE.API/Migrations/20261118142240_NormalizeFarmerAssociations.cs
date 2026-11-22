using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261118142240_NormalizeFarmerAssociations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.AddColumn<int>(
            name: "FarmerAssociationId",
            table: "Farmers",
            type: "int",
            nullable: true);

        migrationBuilder.Sql(
            """
            INSERT INTO [FarmerAssociations] ([Name])
            SELECT DISTINCT LTRIM(RTRIM([AssociationName]))
            FROM [Farmers]
            WHERE NULLIF(LTRIM(RTRIM([AssociationName])), N'') IS NOT NULL;

            UPDATE [farmers]
            SET [FarmerAssociationId] = [associations].[FarmerAssociationId]
            FROM [Farmers] AS [farmers]
            INNER JOIN [FarmerAssociations] AS [associations]
                ON [associations].[Name] =
                    NULLIF(LTRIM(RTRIM([farmers].[AssociationName])), N'');
            """);

        migrationBuilder.DropIndex(
            name: "IX_Farmers_AssociationName",
            table: "Farmers");

        migrationBuilder.DropColumn(
            name: "AssociationName",
            table: "Farmers");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_FarmerAssociationId",
            table: "Farmers",
            column: "FarmerAssociationId");

        migrationBuilder.CreateIndex(
            name: "IX_FarmerAssociations_Name",
            table: "FarmerAssociations",
            column: "Name",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Farmers_FarmerAssociations_FarmerAssociationId",
            table: "Farmers",
            column: "FarmerAssociationId",
            principalTable: "FarmerAssociations",
            principalColumn: "FarmerAssociationId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Farmers_FarmerAssociations_FarmerAssociationId",
            table: "Farmers");

        migrationBuilder.DropIndex(
            name: "IX_Farmers_FarmerAssociationId",
            table: "Farmers");

        migrationBuilder.AddColumn<string>(
            name: "AssociationName",
            table: "Farmers",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE [farmers]
            SET [AssociationName] = [associations].[Name]
            FROM [Farmers] AS [farmers]
            INNER JOIN [FarmerAssociations] AS [associations]
                ON [associations].[FarmerAssociationId] =
                    [farmers].[FarmerAssociationId];
            """);

        migrationBuilder.DropTable(
            name: "FarmerAssociations");

        migrationBuilder.DropColumn(
            name: "FarmerAssociationId",
            table: "Farmers");

        migrationBuilder.CreateIndex(
            name: "IX_Farmers_AssociationName",
            table: "Farmers",
            column: "AssociationName");
    }
}
