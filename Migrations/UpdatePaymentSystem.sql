-- 1. Update OrderTable with new columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[OrderTable]') AND name = N'TotalAmount')
BEGIN
    ALTER TABLE [OrderTable] ADD [TotalAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0.00;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[OrderTable]') AND name = N'CommissionAmount')
BEGIN
    ALTER TABLE [OrderTable] ADD [CommissionAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0.00;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[OrderTable]') AND name = N'VendorAmount')
BEGIN
    ALTER TABLE [OrderTable] ADD [VendorAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0.00;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[OrderTable]') AND name = N'VendorId')
BEGIN
    ALTER TABLE [OrderTable] ADD [VendorId] INT NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[OrderTable]') AND name = N'AdminId')
BEGIN
    ALTER TABLE [OrderTable] ADD [AdminId] INT NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[OrderTable]') AND name = N'Version')
BEGIN
    ALTER TABLE [OrderTable] ADD [Version] INT NOT NULL DEFAULT 1;
END

-- 1.1 Update Payment table with TransactionId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Payment]') AND name = N'TransactionId')
BEGIN
    ALTER TABLE [Payment] ADD [TransactionId] NVARCHAR(MAX) NULL;
END

-- 2. Create VendorPayouts Table (FIXED VendorId reference)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[VendorPayouts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [VendorPayouts] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [OrderId] INT NULL,
        [VendorId] INT NOT NULL,
        [Amount] DECIMAL(18, 2) NOT NULL,
        [TotalSales] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
        [CommissionDeducted] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
        [PayoutMonth] NVARCHAR(10) NULL,
        [Status] INT NOT NULL DEFAULT 0, -- 0 = Pending, 1 = PaidToVendor
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_VendorPayouts_OrderTable FOREIGN KEY (OrderId) REFERENCES OrderTable(OrderId),
        CONSTRAINT FK_VendorPayouts_VendorSignup FOREIGN KEY (VendorId) REFERENCES VendorSignup(VendorId)
    );
END

-- 2.1 Update VendorPayouts if table already exists but lacks monthly columns
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[VendorPayouts]') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[VendorPayouts]') AND name = N'TotalSales')
    BEGIN
        ALTER TABLE [VendorPayouts] ADD [TotalSales] DECIMAL(18, 2) NOT NULL DEFAULT 0.00;
        ALTER TABLE [VendorPayouts] ADD [CommissionDeducted] DECIMAL(18, 2) NOT NULL DEFAULT 0.00;
        ALTER TABLE [VendorPayouts] ADD [PayoutMonth] NVARCHAR(10) NULL;
        ALTER TABLE [VendorPayouts] ALTER COLUMN [OrderId] INT NULL;
    END
END

-- 3. Create PaymentAuditLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[PaymentAuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [PaymentAuditLogs] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [ActorId] NVARCHAR(MAX) NOT NULL,
        [VendorPayoutId] INT NOT NULL,
        [OldStatus] NVARCHAR(MAX) NOT NULL,
        [NewStatus] NVARCHAR(MAX) NOT NULL,
        [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO
