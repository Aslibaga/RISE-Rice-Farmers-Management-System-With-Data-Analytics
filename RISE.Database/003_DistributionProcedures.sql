USE [RISE_DB];
GO

IF OBJECT_ID(N'[dbo].[usp_ConfirmDistribution]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_ConfirmDistribution];
GO

CREATE PROCEDURE [dbo].[usp_ConfirmDistribution]
    @DistributionId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Status nvarchar(20);

        SELECT @Status = [Status]
        FROM [dbo].[Distributions] WITH (UPDLOCK, HOLDLOCK)
        WHERE [DistributionId] = @DistributionId;

        IF @Status IS NULL
        BEGIN
            RAISERROR(N'Distribution was not found.', 16, 1);
        END;

        IF @Status <> N'Approved'
        BEGIN
            RAISERROR(N'Only approved distributions can be confirmed.', 16, 1);
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM [dbo].[DistributionItems]
            WHERE [DistributionId] = @DistributionId
        )
        BEGIN
            RAISERROR(N'A distribution must contain at least one item.', 16, 1);
        END;

        IF EXISTS (
            SELECT 1
            FROM [dbo].[DistributionItems] AS [di]
            INNER JOIN [dbo].[Supplies] AS [s] WITH (UPDLOCK, HOLDLOCK)
                ON [s].[SupplyId] = [di].[SupplyId]
            WHERE [di].[DistributionId] = @DistributionId
              AND [s].[StockQuantity] < [di].[Quantity]
        )
        BEGIN
            RAISERROR(N'One or more supplies do not have enough stock.', 16, 1);
        END;

        UPDATE [s]
        SET [s].[StockQuantity] = [s].[StockQuantity] - [items].[Quantity]
        FROM [dbo].[Supplies] AS [s]
        INNER JOIN (
            SELECT [SupplyId], SUM([Quantity]) AS [Quantity]
            FROM [dbo].[DistributionItems]
            WHERE [DistributionId] = @DistributionId
            GROUP BY [SupplyId]
        ) AS [items]
            ON [items].[SupplyId] = [s].[SupplyId];

        UPDATE [dbo].[Distributions]
        SET [Status] = N'Distributed',
            [DistributionDate] = CONVERT(date, SYSUTCDATETIME())
        WHERE [DistributionId] = @DistributionId;

        INSERT INTO [dbo].[AuditLogs]
            ([UserId], [Action], [EntityName], [EntityId], [OldValuesJson], [NewValuesJson])
        SELECT
            [CreatedByUserId],
            N'Confirm',
            N'Distribution',
            CONVERT(nvarchar(100), [DistributionId]),
            N'{"status":"Approved"}',
            N'{"status":"Distributed"}'
        FROM [dbo].[Distributions]
        WHERE [DistributionId] = @DistributionId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO
