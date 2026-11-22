USE [RISE_DB];
GO

-- Reference-data seed for the RISE operational lookup tables.
--
-- Run this script against RISE_DB AFTER the Entity Framework migrations have
-- been applied (see RISE.API/Migrations and RISE.Database/README.md). The
-- migrations create [dbo].[Roles] and [dbo].[SupplyCategories] with the
-- [Name] and [Description] columns this script seeds; this script only inserts
-- the standard lookup rows.
--
-- Each table block is guarded by IF OBJECT_ID(...) IS NOT NULL so the script
-- stays safe against a database where the migrations have not yet been applied:
-- missing tables are skipped instead of raising an error. The IF NOT EXISTS
-- checks keep the script idempotent on repeated runs.

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[dbo].[Roles]', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Administrator')
            INSERT INTO [dbo].[Roles] ([Name], [Description])
            VALUES (N'Administrator', N'Manages system users and configuration.');

        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Agriculture Officer')
            INSERT INTO [dbo].[Roles] ([Name], [Description])
            VALUES (N'Agriculture Officer', N'Manages assistance and farm records.');

        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Association Officer')
            INSERT INTO [dbo].[Roles] ([Name], [Description])
            VALUES (N'Association Officer', N'Maintains farmer association records.');

        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Viewer')
            INSERT INTO [dbo].[Roles] ([Name], [Description])
            VALUES (N'Viewer', N'Views records and analytics.');
    END

    IF OBJECT_ID(N'[dbo].[SupplyCategories]', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[SupplyCategories] WHERE [Name] = N'Seeds')
            INSERT INTO [dbo].[SupplyCategories] ([Name], [Description])
            VALUES (N'Seeds', N'Rice seed and planting materials.');

        IF NOT EXISTS (SELECT 1 FROM [dbo].[SupplyCategories] WHERE [Name] = N'Fertilizers')
            INSERT INTO [dbo].[SupplyCategories] ([Name], [Description])
            VALUES (N'Fertilizers', N'Soil nutrients and fertilizer products.');

        IF NOT EXISTS (SELECT 1 FROM [dbo].[SupplyCategories] WHERE [Name] = N'Crop Protection')
            INSERT INTO [dbo].[SupplyCategories] ([Name], [Description])
            VALUES (N'Crop Protection', N'Pest, disease, and weed control supplies.');

        IF NOT EXISTS (SELECT 1 FROM [dbo].[SupplyCategories] WHERE [Name] = N'Tools and Equipment')
            INSERT INTO [dbo].[SupplyCategories] ([Name], [Description])
            VALUES (N'Tools and Equipment', N'Farm tools and equipment.');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;
