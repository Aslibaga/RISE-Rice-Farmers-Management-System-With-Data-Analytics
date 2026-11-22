/*
    Operational SQL procedures for RISE.
    EF Core migrations remain authoritative for table definitions.
    This script targets SQL Server 2008-compatible T-SQL.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[usp_RegisterFarmer]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_RegisterFarmer];
GO

CREATE PROCEDURE [dbo].[usp_RegisterFarmer]
    @FullName nvarchar(150),
    @ContactNumber nvarchar(30),
    @Address nvarchar(300),
    @Barangay nvarchar(100) = NULL,
    @Municipality nvarchar(100) = NULL,
    @AssociationName nvarchar(150) = NULL,
    @FarmerId int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        SET @FullName = LTRIM(RTRIM(@FullName));
        SET @ContactNumber = LTRIM(RTRIM(@ContactNumber));
        SET @Address = LTRIM(RTRIM(@Address));
        SET @Barangay = NULLIF(LTRIM(RTRIM(@Barangay)), N'');
        SET @Municipality = NULLIF(LTRIM(RTRIM(@Municipality)), N'');
        SET @AssociationName = NULLIF(LTRIM(RTRIM(@AssociationName)), N'');

        IF NULLIF(@FullName, N'') IS NULL
            RAISERROR(N'Farmer name is required.', 16, 1);
        IF NULLIF(@ContactNumber, N'') IS NULL
            RAISERROR(N'Contact number is required.', 16, 1);
        IF NULLIF(@Address, N'') IS NULL
            RAISERROR(N'Address is required.', 16, 1);

        IF EXISTS (
            SELECT 1
            FROM [dbo].[Farmers] WITH (UPDLOCK, HOLDLOCK)
            WHERE [IsActive] = 1
              AND [FullName] = @FullName
              AND [ContactNumber] = @ContactNumber
        )
            RAISERROR(N'An active farmer with this name and contact number already exists.', 16, 1);

        DECLARE @FarmerAssociationId int = NULL;

        IF @AssociationName IS NOT NULL
        BEGIN
            SELECT @FarmerAssociationId = [FarmerAssociationId]
            FROM [dbo].[FarmerAssociations] WITH (UPDLOCK, HOLDLOCK)
            WHERE [Name] = @AssociationName;

            IF @FarmerAssociationId IS NULL
            BEGIN
                INSERT INTO [dbo].[FarmerAssociations] ([Name])
                VALUES (@AssociationName);
                SET @FarmerAssociationId = CONVERT(int, SCOPE_IDENTITY());
            END;
        END;

        INSERT INTO [dbo].[Farmers]
            ([FullName], [ContactNumber], [Address], [Barangay], [Municipality],
             [FarmerAssociationId], [IsActive])
        VALUES
            (@FullName, @ContactNumber, @Address, @Barangay, @Municipality,
             @FarmerAssociationId, 1);

        SET @FarmerId = CONVERT(int, SCOPE_IDENTITY());

        INSERT INTO [dbo].[AuditLogs] ([Action], [EntityName], [EntityId], [NewValuesJson])
        VALUES (N'Create', N'Farmer', CONVERT(nvarchar(100), @FarmerId),
                N'{"fullName":"' + REPLACE(@FullName, N'"', N'\"') + N'"}');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID(N'[dbo].[usp_UpdateFarmer]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_UpdateFarmer];
GO

CREATE PROCEDURE [dbo].[usp_UpdateFarmer]
    @FarmerId int,
    @FullName nvarchar(150),
    @ContactNumber nvarchar(30),
    @Address nvarchar(300),
    @Barangay nvarchar(100) = NULL,
    @Municipality nvarchar(100) = NULL,
    @AssociationName nvarchar(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        SET @FullName = LTRIM(RTRIM(@FullName));
        SET @ContactNumber = LTRIM(RTRIM(@ContactNumber));
        SET @Address = LTRIM(RTRIM(@Address));
        SET @Barangay = NULLIF(LTRIM(RTRIM(@Barangay)), N'');
        SET @Municipality = NULLIF(LTRIM(RTRIM(@Municipality)), N'');
        SET @AssociationName = NULLIF(LTRIM(RTRIM(@AssociationName)), N'');

        IF NULLIF(@FullName, N'') IS NULL OR NULLIF(@ContactNumber, N'') IS NULL
            OR NULLIF(@Address, N'') IS NULL
            RAISERROR(N'Farmer name, contact number, and address are required.', 16, 1);

        IF NOT EXISTS (
            SELECT 1 FROM [dbo].[Farmers] WITH (UPDLOCK, HOLDLOCK)
            WHERE [FarmerId] = @FarmerId AND [IsActive] = 1
        )
            RAISERROR(N'Active farmer was not found.', 16, 1);

        IF EXISTS (
            SELECT 1 FROM [dbo].[Farmers]
            WHERE [IsActive] = 1 AND [FarmerId] <> @FarmerId
              AND [FullName] = @FullName AND [ContactNumber] = @ContactNumber
        )
            RAISERROR(N'Another active farmer has this name and contact number.', 16, 1);

        DECLARE @FarmerAssociationId int = NULL;

        IF @AssociationName IS NOT NULL
        BEGIN
            SELECT @FarmerAssociationId = [FarmerAssociationId]
            FROM [dbo].[FarmerAssociations] WITH (UPDLOCK, HOLDLOCK)
            WHERE [Name] = @AssociationName;

            IF @FarmerAssociationId IS NULL
            BEGIN
                INSERT INTO [dbo].[FarmerAssociations] ([Name])
                VALUES (@AssociationName);
                SET @FarmerAssociationId = CONVERT(int, SCOPE_IDENTITY());
            END;
        END;

        UPDATE [dbo].[Farmers]
        SET [FullName] = @FullName,
            [ContactNumber] = @ContactNumber,
            [Address] = @Address,
            [Barangay] = @Barangay,
            [Municipality] = @Municipality,
            [FarmerAssociationId] = @FarmerAssociationId
        WHERE [FarmerId] = @FarmerId;

        INSERT INTO [dbo].[AuditLogs] ([Action], [EntityName], [EntityId], [NewValuesJson])
        VALUES (N'Update', N'Farmer', CONVERT(nvarchar(100), @FarmerId),
                N'{"fullName":"' + REPLACE(@FullName, N'"', N'\"') + N'"}');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID(N'[dbo].[usp_RecordHarvest]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_RecordHarvest];
GO

CREATE PROCEDURE [dbo].[usp_RecordHarvest]
    @PlantingRecordId int,
    @HarvestDate date,
    @QuantityKilograms decimal(18,3),
    @MoisturePercent decimal(5,2) = NULL,
    @Grade nvarchar(50) = NULL,
    @Notes nvarchar(2000) = NULL,
    @RecordedByUserId int = NULL,
    @HarvestRecordId int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @PlantingDate date;
        SELECT @PlantingDate = [PlantingDate]
        FROM [dbo].[PlantingRecords] WITH (UPDLOCK, HOLDLOCK)
        WHERE [PlantingRecordId] = @PlantingRecordId;

        IF @PlantingDate IS NULL
            RAISERROR(N'Planting record was not found.', 16, 1);
        IF @HarvestDate < @PlantingDate
            RAISERROR(N'Harvest date cannot be earlier than planting date.', 16, 1);
        IF @QuantityKilograms <= 0
            RAISERROR(N'Harvest quantity must be greater than zero.', 16, 1);
        IF @MoisturePercent IS NOT NULL
           AND (@MoisturePercent < 0 OR @MoisturePercent > 100)
            RAISERROR(N'Moisture percentage must be between zero and one hundred.', 16, 1);

        INSERT INTO [dbo].[HarvestRecords]
            ([PlantingRecordId], [HarvestDate], [QuantityKilograms],
             [MoisturePercent], [Grade], [Notes])
        VALUES
            (@PlantingRecordId, @HarvestDate, @QuantityKilograms,
             @MoisturePercent, @Grade, @Notes);

        SET @HarvestRecordId = CONVERT(int, SCOPE_IDENTITY());

        INSERT INTO [dbo].[AuditLogs] ([UserId], [Action], [EntityName], [EntityId], [NewValuesJson])
        VALUES (@RecordedByUserId, N'Create', N'HarvestRecord',
                CONVERT(nvarchar(100), @HarvestRecordId),
                N'{"quantityKilograms":' + CONVERT(nvarchar(40), @QuantityKilograms) + N'}');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID(N'[dbo].[usp_AdjustInventory]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_AdjustInventory];
GO

CREATE PROCEDURE [dbo].[usp_AdjustInventory]
    @SupplyId int,
    @QuantityChange decimal(18,3),
    @Reason nvarchar(200),
    @UserId int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @QuantityChange = 0
            RAISERROR(N'Inventory adjustment must not be zero.', 16, 1);
        IF NULLIF(LTRIM(RTRIM(@Reason)), N'') IS NULL
            RAISERROR(N'Adjustment reason is required.', 16, 1);

        DECLARE @OldStock decimal(18,3);
        SELECT @OldStock = [StockQuantity]
        FROM [dbo].[Supplies] WITH (UPDLOCK, HOLDLOCK)
        WHERE [SupplyId] = @SupplyId AND [IsActive] = 1;

        IF @OldStock IS NULL
            RAISERROR(N'Active supply was not found.', 16, 1);
        IF @OldStock + @QuantityChange < 0
            RAISERROR(N'Inventory adjustment would make stock negative.', 16, 1);

        UPDATE [dbo].[Supplies]
        SET [StockQuantity] = [StockQuantity] + @QuantityChange
        WHERE [SupplyId] = @SupplyId;

        INSERT INTO [dbo].[AuditLogs]
            ([UserId], [Action], [EntityName], [EntityId], [OldValuesJson], [NewValuesJson])
        VALUES
            (@UserId, N'InventoryAdjustment', N'Supply', CONVERT(nvarchar(100), @SupplyId),
             N'{"stockQuantity":' + CONVERT(nvarchar(40), @OldStock) + N'}',
             N'{"stockQuantity":' + CONVERT(nvarchar(40), @OldStock + @QuantityChange) +
             N',"reason":"' + REPLACE(REPLACE(@Reason, N'\', N'\\'), N'"', N'\"') + N'"}');

        SELECT [SupplyId], [Code], [Name], [StockQuantity]
        FROM [dbo].[Supplies]
        WHERE [SupplyId] = @SupplyId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID(N'[dbo].[usp_RecordSupplyUsage]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_RecordSupplyUsage];
GO

CREATE PROCEDURE [dbo].[usp_RecordSupplyUsage]
    @PlantingRecordId int,
    @SupplyId int,
    @UsageDate date,
    @QuantityUsed decimal(18,3),
    @DistributionItemId int = NULL,
    @RecordedByUserId int = NULL,
    @Notes nvarchar(2000) = NULL,
    @SupplyUsageRecordId int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @QuantityUsed <= 0
            RAISERROR(N'Usage quantity must be greater than zero.', 16, 1);

        DECLARE @PlantingDate date;
        SELECT @PlantingDate = [PlantingDate]
        FROM [dbo].[PlantingRecords] WITH (UPDLOCK, HOLDLOCK)
        WHERE [PlantingRecordId] = @PlantingRecordId;

        IF @PlantingDate IS NULL
            RAISERROR(N'Planting record was not found.', 16, 1);
        IF @UsageDate < @PlantingDate
            RAISERROR(N'Usage date cannot be earlier than planting date.', 16, 1);
        IF NOT EXISTS (
            SELECT 1 FROM [dbo].[Supplies]
            WHERE [SupplyId] = @SupplyId AND [IsActive] = 1
        )
            RAISERROR(N'Active supply was not found.', 16, 1);

        IF @DistributionItemId IS NOT NULL AND NOT EXISTS (
            SELECT 1
            FROM [dbo].[DistributionItems] AS [di]
            INNER JOIN [dbo].[Distributions] AS [d]
                ON [d].[DistributionId] = [di].[DistributionId]
            INNER JOIN [dbo].[Farms] AS [farm]
                ON [farm].[FarmerId] = [d].[FarmerId]
            INNER JOIN [dbo].[PlantingRecords] AS [p]
                ON [p].[FarmId] = [farm].[FarmId]
            WHERE [di].[DistributionItemId] = @DistributionItemId
              AND [di].[SupplyId] = @SupplyId
              AND [d].[Status] = N'Distributed'
              AND [p].[PlantingRecordId] = @PlantingRecordId
        )
            RAISERROR(N'Distribution item does not match this farmer, crop, and supply.', 16, 1);

        INSERT INTO [dbo].[SupplyUsageRecords]
            ([PlantingRecordId], [SupplyId], [DistributionItemId], [RecordedByUserId],
             [UsageDate], [QuantityUsed], [Notes])
        VALUES
            (@PlantingRecordId, @SupplyId, @DistributionItemId, @RecordedByUserId,
             @UsageDate, @QuantityUsed, @Notes);

        SET @SupplyUsageRecordId = CONVERT(int, SCOPE_IDENTITY());

        INSERT INTO [dbo].[AuditLogs] ([UserId], [Action], [EntityName], [EntityId], [NewValuesJson])
        VALUES (@RecordedByUserId, N'Create', N'SupplyUsageRecord',
                CONVERT(nvarchar(100), @SupplyUsageRecordId),
                N'{"quantityUsed":' + CONVERT(nvarchar(40), @QuantityUsed) + N'}');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID(N'[dbo].[usp_GetAnnualProductionSummary]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_GetAnnualProductionSummary];
GO

CREATE PROCEDURE [dbo].[usp_GetAnnualProductionSummary]
    @HarvestYear int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [HarvestYear], [Municipality], [HarvestRecordCount],
           [FarmerCount], [TotalHarvestKilograms]
    FROM [dbo].[vw_HarvestAnnualAnalytics]
    WHERE [HarvestYear] = @HarvestYear
    ORDER BY [Municipality];
END;
GO

IF OBJECT_ID(N'[dbo].[usp_GetMonthlyProductionSummary]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_GetMonthlyProductionSummary];
GO

CREATE PROCEDURE [dbo].[usp_GetMonthlyProductionSummary]
    @HarvestYear int,
    @HarvestMonth int
AS
BEGIN
    SET NOCOUNT ON;
    IF @HarvestMonth < 1 OR @HarvestMonth > 12
    BEGIN
        RAISERROR(N'Harvest month must be between one and twelve.', 16, 1);
        RETURN;
    END;

    SELECT [HarvestYear], [HarvestMonth], [Municipality], [HarvestRecordCount],
           [FarmerCount], [TotalHarvestKilograms]
    FROM [dbo].[vw_HarvestMonthlyAnalytics]
    WHERE [HarvestYear] = @HarvestYear AND [HarvestMonth] = @HarvestMonth
    ORDER BY [Municipality];
END;
GO
