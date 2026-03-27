-- Migration to create IngredientsMaster and audit recipes
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IngredientsMaster]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.IngredientsMaster
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        CaloriesPer100g FLOAT NOT NULL DEFAULT(0),
        ProteinPer100g FLOAT NOT NULL DEFAULT(0),
        CarbsPer100g FLOAT NOT NULL DEFAULT(0),
        FatPer100g FLOAT NOT NULL DEFAULT(0),
        Category NVARCHAR(50) NULL,
        CommonUnit NVARCHAR(50) NULL,
        UnitToGramRatio FLOAT NOT NULL DEFAULT(1)
    );
    CREATE INDEX IX_IngredientsMaster_Name ON dbo.IngredientsMaster(Name);
END
GO

-- Seed common ingredients
IF NOT EXISTS (SELECT 1 FROM dbo.IngredientsMaster WHERE Name = 'Chicken Breast')
BEGIN
    INSERT INTO dbo.IngredientsMaster (Name, CaloriesPer100g, ProteinPer100g, CarbsPer100g, FatPer100g, Category, CommonUnit, UnitToGramRatio)
    VALUES ('Chicken Breast', 165, 31, 0, 3.6, 'Protein', 'g', 1);
END

IF NOT EXISTS (SELECT 1 FROM dbo.IngredientsMaster WHERE Name = 'Basmati Rice')
BEGIN
    INSERT INTO dbo.IngredientsMaster (Name, CaloriesPer100g, ProteinPer100g, CarbsPer100g, FatPer100g, Category, CommonUnit, UnitToGramRatio)
    VALUES ('Basmati Rice', 130, 2.7, 28, 0.3, 'Grain', 'cup', 200);
END

IF NOT EXISTS (SELECT 1 FROM dbo.IngredientsMaster WHERE Name = 'Paneer')
BEGIN
    INSERT INTO dbo.IngredientsMaster (Name, CaloriesPer100g, ProteinPer100g, CarbsPer100g, FatPer100g, Category, CommonUnit, UnitToGramRatio)
    VALUES ('Paneer', 265, 18, 1.2, 20, 'Protein', 'g', 1);
END

IF NOT EXISTS (SELECT 1 FROM dbo.IngredientsMaster WHERE Name = 'Butter')
BEGIN
    INSERT INTO dbo.IngredientsMaster (Name, CaloriesPer100g, ProteinPer100g, CarbsPer100g, FatPer100g, Category, CommonUnit, UnitToGramRatio)
    VALUES ('Butter', 717, 0.9, 0.1, 81, 'Fat', 'tbsp', 14);
END
GO
