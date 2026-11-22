using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261118161927_RepairExistingFarmerData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE [f]
            SET [f].[FarmerAssociationId] = NULL
            FROM [Farmers] AS [f]
            INNER JOIN [FarmerAssociations] AS [a]
                ON [a].[FarmerAssociationId] = [f].[FarmerAssociationId]
            WHERE [a].[Name] = N'None';

            DELETE [a]
            FROM [FarmerAssociations] AS [a]
            WHERE [a].[Name] = N'None'
              AND NOT EXISTS (
                  SELECT 1
                  FROM [Farmers] AS [f]
                  WHERE [f].[FarmerAssociationId] = [a].[FarmerAssociationId]
              );

            UPDATE [Farmers]
            SET [FullName] = LTRIM(RTRIM([FullName])),
                [ContactNumber] = LTRIM(RTRIM([ContactNumber])),
                [Address] = LTRIM(RTRIM([Address]));

            UPDATE [Farmers]
            SET [Barangay] = NULL
            WHERE NULLIF(LTRIM(RTRIM([Barangay])), N'') IS NULL;

            UPDATE [Farmers]
            SET [Municipality] = NULL
            WHERE NULLIF(LTRIM(RTRIM([Municipality])), N'') IS NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1 FROM [FarmerAssociations] WHERE [Name] = N'None'
            )
            BEGIN
                INSERT INTO [FarmerAssociations] ([Name])
                VALUES (N'None');
            END;
            """);
    }
}
