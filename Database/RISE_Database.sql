/*
    RISE_DB schema script generated from RISE.API EF Core migrations.
    This ordered script is for a new, empty database only. Do not run it
    against an existing RISE_DB; update existing databases with
    "dotnet ef database update" so recorded migrations preserve user data.
    EF Core migrations are the authoritative table and constraint definition.
*/
IF DB_ID(N'RISE_DB') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [RISE_DB]');
END;
GO

USE [RISE_DB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Farmers] (
    [FarmerId] int NOT NULL IDENTITY,
    [FullName] nvarchar(max) NOT NULL,
    [ContactNumber] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Farmers] PRIMARY KEY ([FarmerId])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118135656_InitialCreate', N'11.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Farmers] ADD [AssociationName] nvarchar(150) NULL;

ALTER TABLE [Farmers] ADD [Barangay] nvarchar(100) NULL;

ALTER TABLE [Farmers] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [Farmers] ADD [Municipality] nvarchar(100) NULL;

CREATE INDEX [IX_Farmers_AssociationName] ON [Farmers] ([AssociationName]);

CREATE INDEX [IX_Farmers_Barangay] ON [Farmers] ([Barangay]);

CREATE INDEX [IX_Farmers_IsActive] ON [Farmers] ([IsActive]);

CREATE INDEX [IX_Farmers_Municipality] ON [Farmers] ([Municipality]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118141429_FarmerManagement', N'11.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [FarmerAssociations] (
    [FarmerAssociationId] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    CONSTRAINT [PK_FarmerAssociations] PRIMARY KEY ([FarmerAssociationId])
);

ALTER TABLE [Farmers] ADD [FarmerAssociationId] int NULL;

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

DROP INDEX [IX_Farmers_AssociationName] ON [Farmers];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME(OBJECT_NAME([c].[default_object_id]))
FROM [sys].[columns] [c]
WHERE [c].[object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'AssociationName';
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Farmers] DROP COLUMN [AssociationName];

CREATE INDEX [IX_Farmers_FarmerAssociationId] ON [Farmers] ([FarmerAssociationId]);

CREATE UNIQUE INDEX [IX_FarmerAssociations_Name] ON [FarmerAssociations] ([Name]);

ALTER TABLE [Farmers] ADD CONSTRAINT [FK_Farmers_FarmerAssociations_FarmerAssociationId] FOREIGN KEY ([FarmerAssociationId]) REFERENCES [FarmerAssociations] ([FarmerAssociationId]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118142240_NormalizeFarmerAssociations', N'11.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME(OBJECT_NAME([c].[default_object_id]))
FROM [sys].[columns] [c]
WHERE [c].[object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'FullName';
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Farmers] ALTER COLUMN [FullName] nvarchar(150) NOT NULL;

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME(OBJECT_NAME([c].[default_object_id]))
FROM [sys].[columns] [c]
WHERE [c].[object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'ContactNumber';
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Farmers] ALTER COLUMN [ContactNumber] nvarchar(30) NOT NULL;

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME(OBJECT_NAME([c].[default_object_id]))
FROM [sys].[columns] [c]
WHERE [c].[object_id] = OBJECT_ID(N'[Farmers]') AND [c].[name] = N'Address';
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Farmers] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Farmers] ALTER COLUMN [Address] nvarchar(300) NOT NULL;

CREATE INDEX [IX_Farmers_FullName] ON [Farmers] ([FullName]);

CREATE UNIQUE INDEX [IX_Farmers_FullName_ContactNumber] ON [Farmers] ([FullName], [ContactNumber]) WHERE [IsActive] = 1;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118155932_RepairFarmerDatabaseConstraints', N'11.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118161927_RepairExistingFarmerData', N'11.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [AssistancePrograms] (
    [AssistanceProgramId] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_AssistancePrograms] PRIMARY KEY ([AssistanceProgramId]),
    CONSTRAINT [CK_AssistancePrograms_DateRange] CHECK ([EndDate] IS NULL OR [EndDate] >= [StartDate])
);

CREATE TABLE [Farms] (
    [FarmId] int NOT NULL IDENTITY,
    [FarmerId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Barangay] nvarchar(100) NOT NULL,
    [Municipality] nvarchar(100) NOT NULL,
    [AreaHectares] decimal(12,4) NOT NULL,
    [SoilType] nvarchar(100) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Farms] PRIMARY KEY ([FarmId]),
    CONSTRAINT [CK_Farms_AreaHectares_Positive] CHECK ([AreaHectares] > 0),
    CONSTRAINT [FK_Farms_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([FarmerId]) ON DELETE NO ACTION
);

CREATE TABLE [Roles] (
    [RoleId] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
);

CREATE TABLE [Suppliers] (
    [SupplierId] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [ContactPerson] nvarchar(150) NULL,
    [PhoneNumber] nvarchar(30) NULL,
    [Email] nvarchar(254) NULL,
    [Address] nvarchar(500) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([SupplierId])
);

CREATE TABLE [SupplyCategories] (
    [SupplyCategoryId] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    CONSTRAINT [PK_SupplyCategories] PRIMARY KEY ([SupplyCategoryId])
);

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [UserName] nvarchar(100) NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [DisplayName] nvarchar(150) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);

CREATE TABLE [Supplies] (
    [SupplyId] int NOT NULL IDENTITY,
    [SupplyCategoryId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [UnitOfMeasure] nvarchar(30) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [StockQuantity] decimal(18,3) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Supplies] PRIMARY KEY ([SupplyId]),
    CONSTRAINT [CK_Supplies_StockQuantity_NonNegative] CHECK ([StockQuantity] >= 0),
    CONSTRAINT [CK_Supplies_UnitCost_NonNegative] CHECK ([UnitCost] >= 0),
    CONSTRAINT [FK_Supplies_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([SupplierId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Supplies_SupplyCategories_SupplyCategoryId] FOREIGN KEY ([SupplyCategoryId]) REFERENCES [SupplyCategories] ([SupplyCategoryId]) ON DELETE NO ACTION
);

CREATE TABLE [AuditLogs] (
    [AuditLogId] bigint NOT NULL IDENTITY,
    [UserId] int NULL,
    [Action] nvarchar(50) NOT NULL,
    [EntityName] nvarchar(150) NOT NULL,
    [EntityId] nvarchar(100) NULL,
    [OccurredAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [OldValuesJson] nvarchar(max) NULL,
    [NewValuesJson] nvarchar(max) NULL,
    [IpAddress] nvarchar(45) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId]),
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Distributions] (
    [DistributionId] int NOT NULL IDENTITY,
    [FarmerId] int NOT NULL,
    [AssistanceProgramId] int NULL,
    [CreatedByUserId] int NULL,
    [ReferenceNumber] nvarchar(80) NULL,
    [DistributionDate] date NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Distributed',
    [Notes] nvarchar(2000) NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Distributions] PRIMARY KEY ([DistributionId]),
    CONSTRAINT [CK_Distributions_Status] CHECK ([Status] IN (N'Draft', N'Approved', N'Distributed', N'Cancelled')),
    CONSTRAINT [FK_Distributions_AssistancePrograms_AssistanceProgramId] FOREIGN KEY ([AssistanceProgramId]) REFERENCES [AssistancePrograms] ([AssistanceProgramId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Distributions_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([FarmerId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Distributions_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [UserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [AssignedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [DistributionItems] (
    [DistributionItemId] int NOT NULL IDENTITY,
    [DistributionId] int NOT NULL,
    [SupplyId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [LotNumber] nvarchar(100) NULL,
    CONSTRAINT [PK_DistributionItems] PRIMARY KEY ([DistributionItemId]),
    CONSTRAINT [CK_DistributionItems_Quantity_Positive] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_DistributionItems_UnitCost_NonNegative] CHECK ([UnitCost] >= 0),
    CONSTRAINT [FK_DistributionItems_Distributions_DistributionId] FOREIGN KEY ([DistributionId]) REFERENCES [Distributions] ([DistributionId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DistributionItems_Supplies_SupplyId] FOREIGN KEY ([SupplyId]) REFERENCES [Supplies] ([SupplyId]) ON DELETE NO ACTION
);

CREATE TABLE [PlantingRecords] (
    [PlantingRecordId] int NOT NULL IDENTITY,
    [FarmId] int NOT NULL,
    [SeedDistributionItemId] int NULL,
    [CropName] nvarchar(100) NOT NULL DEFAULT N'Rice',
    [Variety] nvarchar(100) NULL,
    [Season] nvarchar(50) NULL,
    [PlantingDate] date NOT NULL,
    [AreaPlantedHectares] decimal(12,4) NOT NULL,
    [ExpectedHarvestDate] date NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Growing',
    CONSTRAINT [PK_PlantingRecords] PRIMARY KEY ([PlantingRecordId]),
    CONSTRAINT [CK_PlantingRecords_Area_Positive] CHECK ([AreaPlantedHectares] > 0),
    CONSTRAINT [CK_PlantingRecords_ExpectedHarvest] CHECK ([ExpectedHarvestDate] IS NULL OR [ExpectedHarvestDate] >= [PlantingDate]),
    CONSTRAINT [CK_PlantingRecords_Status] CHECK ([Status] IN (N'Growing', N'Harvested', N'Failed', N'Cancelled')),
    CONSTRAINT [FK_PlantingRecords_DistributionItems_SeedDistributionItemId] FOREIGN KEY ([SeedDistributionItemId]) REFERENCES [DistributionItems] ([DistributionItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PlantingRecords_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([FarmId]) ON DELETE NO ACTION
);

CREATE TABLE [CropMonitoringRecords] (
    [CropMonitoringRecordId] int NOT NULL IDENTITY,
    [PlantingRecordId] int NOT NULL,
    [RecordedByUserId] int NULL,
    [ObservedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [GrowthStage] nvarchar(100) NOT NULL,
    [CropHealth] nvarchar(100) NULL,
    [PestOrDisease] nvarchar(500) NULL,
    [RainfallMillimeters] decimal(10,2) NULL,
    [Notes] nvarchar(2000) NULL,
    CONSTRAINT [PK_CropMonitoringRecords] PRIMARY KEY ([CropMonitoringRecordId]),
    CONSTRAINT [CK_CropMonitoringRecords_Rainfall_NonNegative] CHECK ([RainfallMillimeters] IS NULL OR [RainfallMillimeters] >= 0),
    CONSTRAINT [FK_CropMonitoringRecords_PlantingRecords_PlantingRecordId] FOREIGN KEY ([PlantingRecordId]) REFERENCES [PlantingRecords] ([PlantingRecordId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CropMonitoringRecords_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [HarvestRecords] (
    [HarvestRecordId] int NOT NULL IDENTITY,
    [PlantingRecordId] int NOT NULL,
    [HarvestDate] date NOT NULL,
    [QuantityKilograms] decimal(18,3) NOT NULL,
    [MoisturePercent] decimal(5,2) NULL,
    [Grade] nvarchar(50) NULL,
    [Notes] nvarchar(2000) NULL,
    CONSTRAINT [PK_HarvestRecords] PRIMARY KEY ([HarvestRecordId]),
    CONSTRAINT [CK_HarvestRecords_Moisture_Range] CHECK ([MoisturePercent] IS NULL OR ([MoisturePercent] >= 0 AND [MoisturePercent] <= 100)),
    CONSTRAINT [CK_HarvestRecords_Quantity_Positive] CHECK ([QuantityKilograms] > 0),
    CONSTRAINT [FK_HarvestRecords_PlantingRecords_PlantingRecordId] FOREIGN KEY ([PlantingRecordId]) REFERENCES [PlantingRecords] ([PlantingRecordId]) ON DELETE NO ACTION
);

CREATE TABLE [SupplyUsageRecords] (
    [SupplyUsageRecordId] int NOT NULL IDENTITY,
    [PlantingRecordId] int NOT NULL,
    [SupplyId] int NOT NULL,
    [DistributionItemId] int NULL,
    [RecordedByUserId] int NULL,
    [UsageDate] date NOT NULL,
    [QuantityUsed] decimal(18,3) NOT NULL,
    [Notes] nvarchar(2000) NULL,
    CONSTRAINT [PK_SupplyUsageRecords] PRIMARY KEY ([SupplyUsageRecordId]),
    CONSTRAINT [CK_SupplyUsageRecords_Quantity_Positive] CHECK ([QuantityUsed] > 0),
    CONSTRAINT [FK_SupplyUsageRecords_DistributionItems_DistributionItemId] FOREIGN KEY ([DistributionItemId]) REFERENCES [DistributionItems] ([DistributionItemId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupplyUsageRecords_PlantingRecords_PlantingRecordId] FOREIGN KEY ([PlantingRecordId]) REFERENCES [PlantingRecords] ([PlantingRecordId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupplyUsageRecords_Supplies_SupplyId] FOREIGN KEY ([SupplyId]) REFERENCES [Supplies] ([SupplyId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupplyUsageRecords_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [HarvestVerificationRecords] (
    [HarvestVerificationRecordId] int NOT NULL IDENTITY,
    [HarvestRecordId] int NOT NULL,
    [VerifiedByUserId] int NULL,
    [VerifiedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [VerifiedQuantityKilograms] decimal(18,3) NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [Remarks] nvarchar(2000) NULL,
    CONSTRAINT [PK_HarvestVerificationRecords] PRIMARY KEY ([HarvestVerificationRecordId]),
    CONSTRAINT [CK_HarvestVerificationRecords_Quantity_NonNegative] CHECK ([VerifiedQuantityKilograms] IS NULL OR [VerifiedQuantityKilograms] >= 0),
    CONSTRAINT [CK_HarvestVerificationRecords_Status] CHECK ([Status] IN (N'Pending', N'Verified', N'Rejected')),
    CONSTRAINT [FK_HarvestVerificationRecords_HarvestRecords_HarvestRecordId] FOREIGN KEY ([HarvestRecordId]) REFERENCES [HarvestRecords] ([HarvestRecordId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HarvestVerificationRecords_Users_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [SupportingDocuments] (
    [SupportingDocumentId] int NOT NULL IDENTITY,
    [FarmerId] int NULL,
    [FarmId] int NULL,
    [DistributionId] int NULL,
    [PlantingRecordId] int NULL,
    [HarvestRecordId] int NULL,
    [UploadedByUserId] int NULL,
    [FileName] nvarchar(255) NOT NULL,
    [StoragePath] nvarchar(1000) NOT NULL,
    [ContentType] nvarchar(150) NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [Sha256] nvarchar(64) NULL,
    [UploadedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SupportingDocuments] PRIMARY KEY ([SupportingDocumentId]),
    CONSTRAINT [CK_SupportingDocuments_FileSize_NonNegative] CHECK ([FileSizeBytes] >= 0),
    CONSTRAINT [CK_SupportingDocuments_OneParent] CHECK (((CASE WHEN [FarmerId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [FarmId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DistributionId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [PlantingRecordId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [HarvestRecordId] IS NULL THEN 0 ELSE 1 END)) = 1),
    CONSTRAINT [FK_SupportingDocuments_Distributions_DistributionId] FOREIGN KEY ([DistributionId]) REFERENCES [Distributions] ([DistributionId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupportingDocuments_Farmers_FarmerId] FOREIGN KEY ([FarmerId]) REFERENCES [Farmers] ([FarmerId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupportingDocuments_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([FarmId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupportingDocuments_HarvestRecords_HarvestRecordId] FOREIGN KEY ([HarvestRecordId]) REFERENCES [HarvestRecords] ([HarvestRecordId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupportingDocuments_PlantingRecords_PlantingRecordId] FOREIGN KEY ([PlantingRecordId]) REFERENCES [PlantingRecords] ([PlantingRecordId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupportingDocuments_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_AssistancePrograms_Name] ON [AssistancePrograms] ([Name]);

CREATE INDEX [IX_AuditLogs_EntityName_EntityId] ON [AuditLogs] ([EntityName], [EntityId]);

CREATE INDEX [IX_AuditLogs_OccurredAtUtc] ON [AuditLogs] ([OccurredAtUtc]);

CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);

CREATE INDEX [IX_CropMonitoringRecords_PlantingRecordId_ObservedAtUtc] ON [CropMonitoringRecords] ([PlantingRecordId], [ObservedAtUtc]);

CREATE INDEX [IX_CropMonitoringRecords_RecordedByUserId] ON [CropMonitoringRecords] ([RecordedByUserId]);

CREATE UNIQUE INDEX [IX_DistributionItems_DistributionId_SupplyId] ON [DistributionItems] ([DistributionId], [SupplyId]);

CREATE INDEX [IX_DistributionItems_SupplyId] ON [DistributionItems] ([SupplyId]);

CREATE INDEX [IX_Distributions_AssistanceProgramId] ON [Distributions] ([AssistanceProgramId]);

CREATE INDEX [IX_Distributions_CreatedByUserId] ON [Distributions] ([CreatedByUserId]);

CREATE INDEX [IX_Distributions_DistributionDate] ON [Distributions] ([DistributionDate]);

CREATE INDEX [IX_Distributions_FarmerId] ON [Distributions] ([FarmerId]);

CREATE UNIQUE INDEX [IX_Distributions_ReferenceNumber] ON [Distributions] ([ReferenceNumber]) WHERE [ReferenceNumber] IS NOT NULL;

CREATE INDEX [IX_Farms_FarmerId] ON [Farms] ([FarmerId]);

CREATE INDEX [IX_HarvestRecords_HarvestDate] ON [HarvestRecords] ([HarvestDate]);

CREATE INDEX [IX_HarvestRecords_PlantingRecordId_HarvestDate] ON [HarvestRecords] ([PlantingRecordId], [HarvestDate]);

CREATE INDEX [IX_HarvestVerificationRecords_HarvestRecordId] ON [HarvestVerificationRecords] ([HarvestRecordId]);

CREATE INDEX [IX_HarvestVerificationRecords_VerifiedByUserId] ON [HarvestVerificationRecords] ([VerifiedByUserId]);

CREATE INDEX [IX_PlantingRecords_FarmId] ON [PlantingRecords] ([FarmId]);

CREATE INDEX [IX_PlantingRecords_PlantingDate] ON [PlantingRecords] ([PlantingDate]);

CREATE INDEX [IX_PlantingRecords_SeedDistributionItemId] ON [PlantingRecords] ([SeedDistributionItemId]);

CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);

CREATE UNIQUE INDEX [IX_Supplies_Code] ON [Supplies] ([Code]);

CREATE INDEX [IX_Supplies_SupplierId] ON [Supplies] ([SupplierId]);

CREATE INDEX [IX_Supplies_SupplyCategoryId] ON [Supplies] ([SupplyCategoryId]);

CREATE UNIQUE INDEX [IX_SupplyCategories_Name] ON [SupplyCategories] ([Name]);

CREATE INDEX [IX_SupplyUsageRecords_DistributionItemId] ON [SupplyUsageRecords] ([DistributionItemId]);

CREATE INDEX [IX_SupplyUsageRecords_PlantingRecordId_UsageDate] ON [SupplyUsageRecords] ([PlantingRecordId], [UsageDate]);

CREATE INDEX [IX_SupplyUsageRecords_RecordedByUserId] ON [SupplyUsageRecords] ([RecordedByUserId]);

CREATE INDEX [IX_SupplyUsageRecords_SupplyId] ON [SupplyUsageRecords] ([SupplyId]);

CREATE INDEX [IX_SupportingDocuments_DistributionId] ON [SupportingDocuments] ([DistributionId]);

CREATE INDEX [IX_SupportingDocuments_FarmerId] ON [SupportingDocuments] ([FarmerId]);

CREATE INDEX [IX_SupportingDocuments_FarmId] ON [SupportingDocuments] ([FarmId]);

CREATE INDEX [IX_SupportingDocuments_HarvestRecordId] ON [SupportingDocuments] ([HarvestRecordId]);

CREATE INDEX [IX_SupportingDocuments_PlantingRecordId] ON [SupportingDocuments] ([PlantingRecordId]);

CREATE INDEX [IX_SupportingDocuments_UploadedByUserId] ON [SupportingDocuments] ([UploadedByUserId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261119015851_OperationalLifecycle', N'11.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME(OBJECT_NAME([c].[default_object_id]))
FROM [sys].[columns] [c]
WHERE [c].[object_id] = OBJECT_ID(N'[Distributions]') AND [c].[name] = N'Status';
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Distributions] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [Distributions] ADD DEFAULT N'Draft' FOR [Status];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261119020250_DraftDistributionByDefault', N'11.0.0');

COMMIT;
GO

