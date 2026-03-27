-- Audit log table for food categorisation remediation
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogCategorisation]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.AuditLogCategorisation
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FoodId INT NOT NULL,
        FoodName NVARCHAR(250) NOT NULL,
        OldCategoryId INT NULL,
        NewCategoryId INT NOT NULL,
        OldProductCategory NVARCHAR(100) NULL,
        NewProductCategory NVARCHAR(100) NOT NULL,
        OldMealCategory NVARCHAR(100) NULL,
        NewMealCategory NVARCHAR(100) NOT NULL,
        AuditDate DATETIME2 NOT NULL DEFAULT(GETDATE()),
        Notes NVARCHAR(500) NULL
    );
    CREATE INDEX IX_AuditLogCategorisation_FoodId ON dbo.AuditLogCategorisation(FoodId);
END
GO

-- Ensure canonical categories exist
-- Main Course / Indian (Non-Veg)
IF NOT EXISTS (SELECT 1 FROM dbo.AddCategory WHERE ProductCategory = 'Non-Veg' AND MealCategory = 'Main Course / Indian')
BEGIN
    INSERT INTO dbo.AddCategory (ProductCategory, ProductPic, MealCategory, CreatedAt)
    VALUES ('Non-Veg', 'Nonveg.jpeg', 'Main Course / Indian', GETDATE());
END

-- Main Course / Indian (Veg)
IF NOT EXISTS (SELECT 1 FROM dbo.AddCategory WHERE ProductCategory = 'Veg' AND MealCategory = 'Main Course / Indian')
BEGIN
    INSERT INTO dbo.AddCategory (ProductCategory, ProductPic, MealCategory, CreatedAt)
    VALUES ('Veg', 'Veg.jpeg', 'Main Course / Indian', GETDATE());
END

-- Salad
IF NOT EXISTS (SELECT 1 FROM dbo.AddCategory WHERE ProductCategory = 'Veg' AND MealCategory = 'Salads')
BEGIN
    INSERT INTO dbo.AddCategory (ProductCategory, ProductPic, MealCategory, CreatedAt)
    VALUES ('Veg', 'Veg.jpeg', 'Salads', GETDATE());
END

-- Italian
IF NOT EXISTS (SELECT 1 FROM dbo.AddCategory WHERE ProductCategory = 'Veg' AND MealCategory = 'Italian')
BEGIN
    INSERT INTO dbo.AddCategory (ProductCategory, ProductPic, MealCategory, CreatedAt)
    VALUES ('Veg', 'Veg.jpeg', 'Italian', GETDATE());
END

-- Healthy
IF NOT EXISTS (SELECT 1 FROM dbo.AddCategory WHERE ProductCategory = 'Veg' AND MealCategory = 'Healthy')
BEGIN
    INSERT INTO dbo.AddCategory (ProductCategory, ProductPic, MealCategory, CreatedAt)
    VALUES ('Veg', 'Veg.jpeg', 'Healthy', GETDATE());
END
GO
