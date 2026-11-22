/*
    Reporting and lookup views for RISE.
    Run after RISE_Database.sql. The four analytics views mirror
    RISE.Database/002_AnalyticsViews.sql; farmer and inventory views are
    included here as additional reporting views.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[vw_FarmerDirectory]', N'V') IS NOT NULL
    DROP VIEW [dbo].[vw_FarmerDirectory];
GO

CREATE VIEW [dbo].[vw_FarmerDirectory]
AS
    SELECT
        [f].[FarmerId],
        [f].[FullName],
        [f].[ContactNumber],
        [f].[Address],
        [f].[Barangay],
        [f].[Municipality],
        [a].[Name] AS [AssociationName],
        [f].[IsActive]
    FROM [dbo].[Farmers] AS [f]
    LEFT JOIN [dbo].[FarmerAssociations] AS [a]
        ON [a].[FarmerAssociationId] = [f].[FarmerAssociationId];
GO

IF OBJECT_ID(N'[dbo].[vw_InventoryMonitoring]', N'V') IS NOT NULL
    DROP VIEW [dbo].[vw_InventoryMonitoring];
GO

CREATE VIEW [dbo].[vw_InventoryMonitoring]
AS
    SELECT
        [s].[SupplyId],
        [s].[Code],
        [s].[Name] AS [SupplyName],
        [c].[Name] AS [SupplyCategory],
        [supplier].[Name] AS [SupplierName],
        [s].[UnitOfMeasure],
        [s].[UnitCost],
        [s].[StockQuantity],
        CASE WHEN [s].[StockQuantity] = 0 THEN N'Out of stock'
             ELSE N'In stock' END AS [StockStatus],
        [s].[IsActive]
    FROM [dbo].[Supplies] AS [s]
    INNER JOIN [dbo].[SupplyCategories] AS [c]
        ON [c].[SupplyCategoryId] = [s].[SupplyCategoryId]
    INNER JOIN [dbo].[Suppliers] AS [supplier]
        ON [supplier].[SupplierId] = [s].[SupplierId];
GO

IF OBJECT_ID(N'[dbo].[vw_HarvestMonthlyAnalytics]', N'V') IS NOT NULL
    DROP VIEW [dbo].[vw_HarvestMonthlyAnalytics];
GO

CREATE VIEW [dbo].[vw_HarvestMonthlyAnalytics]
AS
    SELECT
        YEAR([h].[HarvestDate]) AS [HarvestYear],
        MONTH([h].[HarvestDate]) AS [HarvestMonth],
        [f].[Municipality],
        COUNT_BIG(*) AS [HarvestRecordCount],
        COUNT(DISTINCT [f].[FarmerId]) AS [FarmerCount],
        SUM([h].[QuantityKilograms]) AS [TotalHarvestKilograms]
    FROM [dbo].[HarvestRecords] AS [h]
    INNER JOIN [dbo].[PlantingRecords] AS [p]
        ON [p].[PlantingRecordId] = [h].[PlantingRecordId]
    INNER JOIN [dbo].[Farms] AS [farm]
        ON [farm].[FarmId] = [p].[FarmId]
    INNER JOIN [dbo].[Farmers] AS [f]
        ON [f].[FarmerId] = [farm].[FarmerId]
    GROUP BY YEAR([h].[HarvestDate]), MONTH([h].[HarvestDate]), [f].[Municipality];
GO

IF OBJECT_ID(N'[dbo].[vw_HarvestAnnualAnalytics]', N'V') IS NOT NULL
    DROP VIEW [dbo].[vw_HarvestAnnualAnalytics];
GO

CREATE VIEW [dbo].[vw_HarvestAnnualAnalytics]
AS
    SELECT
        YEAR([h].[HarvestDate]) AS [HarvestYear],
        [f].[Municipality],
        COUNT_BIG(*) AS [HarvestRecordCount],
        COUNT(DISTINCT [f].[FarmerId]) AS [FarmerCount],
        SUM([h].[QuantityKilograms]) AS [TotalHarvestKilograms]
    FROM [dbo].[HarvestRecords] AS [h]
    INNER JOIN [dbo].[PlantingRecords] AS [p]
        ON [p].[PlantingRecordId] = [h].[PlantingRecordId]
    INNER JOIN [dbo].[Farms] AS [farm]
        ON [farm].[FarmId] = [p].[FarmId]
    INNER JOIN [dbo].[Farmers] AS [f]
        ON [f].[FarmerId] = [farm].[FarmerId]
    GROUP BY YEAR([h].[HarvestDate]), [f].[Municipality];
GO

IF OBJECT_ID(N'[dbo].[vw_DistributionMonthlyAnalytics]', N'V') IS NOT NULL
    DROP VIEW [dbo].[vw_DistributionMonthlyAnalytics];
GO

CREATE VIEW [dbo].[vw_DistributionMonthlyAnalytics]
AS
    SELECT
        YEAR([d].[DistributionDate]) AS [DistributionYear],
        MONTH([d].[DistributionDate]) AS [DistributionMonth],
        [sc].[Name] AS [SupplyCategory],
        COUNT(DISTINCT [d].[DistributionId]) AS [DistributionCount],
        COUNT(DISTINCT [d].[FarmerId]) AS [FarmerCount],
        SUM([di].[Quantity]) AS [TotalQuantity],
        SUM([di].[Quantity] * [di].[UnitCost]) AS [TotalValue]
    FROM [dbo].[Distributions] AS [d]
    INNER JOIN [dbo].[DistributionItems] AS [di]
        ON [di].[DistributionId] = [d].[DistributionId]
    INNER JOIN [dbo].[Supplies] AS [s]
        ON [s].[SupplyId] = [di].[SupplyId]
    INNER JOIN [dbo].[SupplyCategories] AS [sc]
        ON [sc].[SupplyCategoryId] = [s].[SupplyCategoryId]
    WHERE [d].[Status] = N'Distributed'
    GROUP BY YEAR([d].[DistributionDate]), MONTH([d].[DistributionDate]), [sc].[Name];
GO

IF OBJECT_ID(N'[dbo].[vw_DistributionAnnualAnalytics]', N'V') IS NOT NULL
    DROP VIEW [dbo].[vw_DistributionAnnualAnalytics];
GO

CREATE VIEW [dbo].[vw_DistributionAnnualAnalytics]
AS
    SELECT
        YEAR([d].[DistributionDate]) AS [DistributionYear],
        [sc].[Name] AS [SupplyCategory],
        COUNT(DISTINCT [d].[DistributionId]) AS [DistributionCount],
        COUNT(DISTINCT [d].[FarmerId]) AS [FarmerCount],
        SUM([di].[Quantity]) AS [TotalQuantity],
        SUM([di].[Quantity] * [di].[UnitCost]) AS [TotalValue]
    FROM [dbo].[Distributions] AS [d]
    INNER JOIN [dbo].[DistributionItems] AS [di]
        ON [di].[DistributionId] = [d].[DistributionId]
    INNER JOIN [dbo].[Supplies] AS [s]
        ON [s].[SupplyId] = [di].[SupplyId]
    INNER JOIN [dbo].[SupplyCategories] AS [sc]
        ON [sc].[SupplyCategoryId] = [s].[SupplyCategoryId]
    WHERE [d].[Status] = N'Distributed'
    GROUP BY YEAR([d].[DistributionDate]), [sc].[Name];
GO
