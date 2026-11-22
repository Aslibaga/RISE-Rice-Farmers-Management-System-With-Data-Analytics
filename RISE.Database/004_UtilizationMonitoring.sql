-- RISE utilization monitoring extension (safe to run once; preserves existing records)
IF OBJECT_ID(N'dbo.UtilizationMonitorings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UtilizationMonitorings
    (
        UtilizationMonitoringId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_UtilizationMonitorings PRIMARY KEY,
        DistributionItemId INT NOT NULL,
        QuantityUsed DECIMAL(18,3) NULL,
        QuantityStillStocked DECIMAL(18,3) NULL,
        QuantityNotYetUsed DECIMAL(18,3) NULL,
        QuantityDisposed DECIMAL(18,3) NULL,
        MonitoringDate DATE NOT NULL,
        Remarks NVARCHAR(2000) NULL,
        CONSTRAINT UQ_UtilizationMonitorings_DistributionItemId UNIQUE (DistributionItemId),
        CONSTRAINT FK_UtilizationMonitorings_DistributionItems
            FOREIGN KEY (DistributionItemId) REFERENCES dbo.DistributionItems(DistributionItemId),
        CONSTRAINT CK_UtilizationMonitoring_NonNegative CHECK
        (
            (QuantityUsed IS NULL OR QuantityUsed >= 0) AND
            (QuantityStillStocked IS NULL OR QuantityStillStocked >= 0) AND
            (QuantityNotYetUsed IS NULL OR QuantityNotYetUsed >= 0) AND
            (QuantityDisposed IS NULL OR QuantityDisposed >= 0)
        )
    );
END;
GO
