/*
    Safe SSMS examples for RISE.
    The INSERT/UPDATE/DELETE section uses a transaction and rolls back so it
    does not leave a test farmer in RISE_DB.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

-- SELECT, WHERE, and ORDER BY
SELECT [FarmerId], [FullName], [ContactNumber], [Barangay], [Municipality], [IsActive]
FROM [dbo].[Farmers]
WHERE [IsActive] = 1
ORDER BY [FullName];

-- JOIN: farmer and association
SELECT [f].[FarmerId], [f].[FullName], [a].[Name] AS [AssociationName]
FROM [dbo].[Farmers] AS [f]
LEFT JOIN [dbo].[FarmerAssociations] AS [a]
    ON [a].[FarmerAssociationId] = [f].[FarmerAssociationId]
WHERE [f].[IsActive] = 1
ORDER BY [a].[Name], [f].[FullName];

-- GROUP BY: annual production summary
SELECT YEAR([h].[HarvestDate]) AS [HarvestYear],
       COUNT_BIG(*) AS [HarvestRecordCount],
       SUM([h].[QuantityKilograms]) AS [TotalHarvestKilograms]
FROM [dbo].[HarvestRecords] AS [h]
GROUP BY YEAR([h].[HarvestDate])
ORDER BY [HarvestYear] DESC;

-- INSERT, SELECT, UPDATE, DELETE; all test writes are rolled back.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @TestToken nvarchar(20) =
        LEFT(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N''), 20);
    DECLARE @TestFarmerId int;

    INSERT INTO [dbo].[Farmers]
        ([FullName], [ContactNumber], [Address], [IsActive])
    VALUES
        (N'SSMS Query Test ' + @TestToken, N'TEST-' + @TestToken,
         N'Temporary SSMS query example', 1);
    SET @TestFarmerId = CONVERT(int, SCOPE_IDENTITY());

    SELECT [FarmerId], [FullName], [ContactNumber]
    FROM [dbo].[Farmers]
    WHERE [FarmerId] = @TestFarmerId;

    UPDATE [dbo].[Farmers]
    SET [Address] = N'Updated within rollback-safe example'
    WHERE [FarmerId] = @TestFarmerId;

    DELETE FROM [dbo].[Farmers]
    WHERE [FarmerId] = @TestFarmerId;

    ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;

-- ALTER TABLE example on a session-local table; no application schema changes.
CREATE TABLE [#RiseAlterDemo] ([DemoId] int NOT NULL);
ALTER TABLE [#RiseAlterDemo] ADD [DemoLabel] nvarchar(50) NULL;
SELECT [DemoId], [DemoLabel] FROM [#RiseAlterDemo];
DROP TABLE [#RiseAlterDemo];

/*
    For a production schema change, add the property to the C# entity and
    RiseDbContext, create an EF migration, and regenerate RISE_Database.sql.
*/
