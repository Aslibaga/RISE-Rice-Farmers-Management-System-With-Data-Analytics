/*
    Optional end-to-end sample data.
    This script is safe to run for a smoke test: its final statement rolls back
    every sample row. Change the final ROLLBACK to COMMIT only in a development
    database if you intentionally want to keep the generated sample records.

    Run RISE_Database.sql, then RISE_Stored_Procedures.sql, before this script.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @SampleToken nvarchar(20) =
        LEFT(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N''), 20);
    DECLARE @AssociationId int;
    DECLARE @FarmerId int;
    DECLARE @FarmId int;
    DECLARE @ProgramId int;
    DECLARE @SupplierId int;
    DECLARE @CategoryId int;
    DECLARE @SupplyId int;
    DECLARE @DistributionId int;
    DECLARE @DistributionItemId int;
    DECLARE @PlantingRecordId int;
    DECLARE @HarvestRecordId int;
    DECLARE @PlantingDate date = DATEADD(day, -100, CONVERT(date, GETDATE()));
    DECLARE @Today date = CONVERT(date, GETDATE());

    INSERT INTO [dbo].[FarmerAssociations] ([Name])
    VALUES (N'Sample Association ' + @SampleToken);
    SET @AssociationId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO [dbo].[Farmers]
        ([FullName], [ContactNumber], [Address], [Barangay], [Municipality],
         [FarmerAssociationId], [IsActive])
    VALUES
        (N'Sample Farmer ' + @SampleToken, N'SAMPLE-' + @SampleToken,
         N'Example farm address', N'San Isidro', N'Ricefield',
         @AssociationId, 1);
    SET @FarmerId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO [dbo].[Farms]
        ([FarmerId], [Name], [Barangay], [Municipality], [AreaHectares], [SoilType], [IsActive])
    VALUES
        (@FarmerId, N'Sample Rice Farm ' + @SampleToken,
         N'San Isidro', N'Ricefield', 1.2500, N'Clay loam', 1);
    SET @FarmId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO [dbo].[AssistancePrograms]
        ([Name], [Description], [StartDate], [EndDate], [IsActive])
    VALUES
        (N'Sample Rice Assistance ' + @SampleToken,
         N'Demonstration program record; not an actual government program.',
         DATEADD(day, -120, @Today), NULL, 1);
    SET @ProgramId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Address], [IsActive])
    VALUES (N'Sample Agricultural Supplier ' + @SampleToken,
            N'Sample Contact', N'000-000-0000', N'Example supplier address', 1);
    SET @SupplierId = CONVERT(int, SCOPE_IDENTITY());

    SELECT TOP (1) @CategoryId = [SupplyCategoryId]
    FROM [dbo].[SupplyCategories]
    WHERE [Name] = N'Seeds';

    IF @CategoryId IS NULL
    BEGIN
        INSERT INTO [dbo].[SupplyCategories] ([Name], [Description])
        VALUES (N'Seeds', N'Rice seed and planting materials.');
        SET @CategoryId = CONVERT(int, SCOPE_IDENTITY());
    END;

    INSERT INTO [dbo].[Supplies]
        ([SupplyCategoryId], [SupplierId], [Code], [Name], [UnitOfMeasure],
         [UnitCost], [StockQuantity], [IsActive])
    VALUES
        (@CategoryId, @SupplierId, N'SAMPLE-' + @SampleToken,
         N'Sample certified rice seed', N'kg', 65.00, 1000.000, 1);
    SET @SupplyId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO [dbo].[Distributions]
        ([FarmerId], [AssistanceProgramId], [ReferenceNumber], [DistributionDate], [Status], [Notes])
    VALUES
        (@FarmerId, @ProgramId, N'SAMPLE-DIST-' + @SampleToken,
         @Today, N'Approved', N'Demonstration distribution record.');
    SET @DistributionId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO [dbo].[DistributionItems]
        ([DistributionId], [SupplyId], [Quantity], [UnitCost], [LotNumber])
    VALUES
        (@DistributionId, @SupplyId, 20.000, 65.00, N'SAMPLE-LOT-' + @SampleToken);
    SET @DistributionItemId = CONVERT(int, SCOPE_IDENTITY());

    EXEC [dbo].[usp_ConfirmDistribution] @DistributionId = @DistributionId;

    INSERT INTO [dbo].[PlantingRecords]
        ([FarmId], [SeedDistributionItemId], [CropName], [Variety], [Season],
         [PlantingDate], [AreaPlantedHectares], [ExpectedHarvestDate], [Status])
    VALUES
        (@FarmId, @DistributionItemId, N'Rice', N'Example high-yield variety',
         N'Sample season', @PlantingDate, 1.0000, DATEADD(day, 110, @PlantingDate), N'Growing');
    SET @PlantingRecordId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO [dbo].[CropMonitoringRecords]
        ([PlantingRecordId], [ObservedAtUtc], [GrowthStage], [CropHealth],
         [RainfallMillimeters], [Notes])
    VALUES
        (@PlantingRecordId, SYSUTCDATETIME(), N'Panicle initiation', N'Good',
         12.50, N'Demonstration monitoring record.');

    INSERT INTO [dbo].[SupplyUsageRecords]
        ([PlantingRecordId], [SupplyId], [DistributionItemId], [UsageDate], [QuantityUsed], [Notes])
    VALUES
        (@PlantingRecordId, @SupplyId, @DistributionItemId,
         DATEADD(day, 20, @PlantingDate), 10.000, N'Demonstration seed usage record.');

    INSERT INTO [dbo].[HarvestRecords]
        ([PlantingRecordId], [HarvestDate], [QuantityKilograms], [MoisturePercent], [Grade], [Notes])
    VALUES
        (@PlantingRecordId, @Today, 3500.000, 14.50, N'A',
         N'Demonstration harvest record.');
    SET @HarvestRecordId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO [dbo].[HarvestVerificationRecords]
        ([HarvestRecordId], [VerifiedAtUtc], [VerifiedQuantityKilograms], [Status], [Remarks])
    VALUES
        (@HarvestRecordId, SYSUTCDATETIME(), 3500.000, N'Verified',
         N'Demonstration verification record.');

    SELECT @FarmerId AS [SampleFarmerId],
           @FarmId AS [SampleFarmId],
           @DistributionId AS [SampleDistributionId],
           @PlantingRecordId AS [SamplePlantingRecordId],
           @HarvestRecordId AS [SampleHarvestRecordId];

    ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;
