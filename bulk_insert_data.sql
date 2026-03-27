/*
=============================================================================
NUTRIBITE BULK DUMMY DATA INSERT SCRIPT
=============================================================================
Description: Inserts 100+ records into main tables (Users, Vendors, Foods, 
             Recipes, Orders) while maintaining referential integrity.
=============================================================================
*/

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Ensure Recipes table exists (matching earlier model definition)
    IF OBJECT_ID('Recipes', 'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[Recipes] (
            [Id] INT IDENTITY(1,1) PRIMARY KEY,
            [FoodId] INT NOT NULL,
            [Ingredients] NVARCHAR(MAX) NOT NULL,
            [Steps] NVARCHAR(MAX) NOT NULL,
            [ImagePath] NVARCHAR(255) NULL,
            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
        );
        PRINT 'Created [Recipes] table.';
    END

    -- 2. Insert 100 Users
    PRINT 'Inserting 100 Users...';
    DECLARE @i INT = 1;
    WHILE @i <= 100
    BEGIN
        INSERT INTO [UserSignup] (Name, Email, Password, CreatedAt, Phone, Status, CalorieGoal, Role, ProfilePictureUrl)
        VALUES (
            'User ' + CAST(@i AS NVARCHAR),
            'user' + CAST(@i AS NVARCHAR) + '@example.com',
            'Password123!', -- Simple placeholder password
            DATEADD(DAY, -ABS(CHECKSUM(NEWID()) % 365), GETDATE()), -- Random date in last year
            '+91 ' + CAST(9000000000 + @i AS NVARCHAR),
            CASE WHEN @i % 10 = 0 THEN 'Blocked' ELSE 'Active' END,
            CASE WHEN @i % 2 = 0 THEN 2000 ELSE 2500 END,
            'User',
            NULL
        );
        SET @i = @i + 1;
    END

    -- 3. Insert 100 Vendors
    PRINT 'Inserting 100 Vendors...';
    SET @i = 1;
    WHILE @i <= 100
    BEGIN
        INSERT INTO [VendorSignup] (VendorName, Email, PasswordHash, IsApproved, CreatedAt, IsRejected, Phone, Address, Description, OpeningHours, ClosingHours, LogoPath, UpiId)
        VALUES (
            'Kitchen Partner ' + CAST(@i AS NVARCHAR),
            'vendor' + CAST(@i AS NVARCHAR) + '@example.com',
            'ef797c8118f02dfb649607dd5d3f8c7623048c9c063d532cc95c5ed7a898a64f', -- SHA256 for 'password'
            CASE WHEN @i % 5 = 0 THEN 0 ELSE 1 END, -- 80% approved
            DATEADD(DAY, -ABS(CHECKSUM(NEWID()) % 365), GETDATE()),
            0,
            '+91 80000000' + CAST(@i AS NVARCHAR),
            'Street ' + CAST(@i AS NVARCHAR) + ', Food Hub, City',
            'Authentic homemade meals from Kitchen ' + CAST(@i AS NVARCHAR),
            '08:00 AM',
            '22:00 PM',
            '/images/vendors/default-logo.png',
            'vendor' + CAST(@i AS NVARCHAR) + '@okaxis'
        );
        SET @i = @i + 1;
    END

    -- 4. Insert 100 Foods
    PRINT 'Inserting 100 Foods...';
    SET @i = 1;
    DECLARE @VendorId INT, @ProdCatId INT, @MealCatId INT;
    WHILE @i <= 100
    BEGIN
        -- Get a random vendor and categories
        SELECT TOP 1 @VendorId = VendorId FROM VendorSignup ORDER BY NEWID();
        SELECT TOP 1 @ProdCatId = ProductCategoryId FROM ProductCategory ORDER BY NEWID();
        SELECT TOP 1 @MealCatId = MealCategoryId FROM MealCategory ORDER BY NEWID();

        INSERT INTO [Foods] (Name, Description, Price, CategoryId, VendorId, ImagePath, Calories, PreparationTime, Rating, Status, CreatedAt, ProductCategoryId, MealCategoryId, FoodType, NutritionistId, IsVerified, Protein, Carbs, Fat)
        VALUES (
            'NutriMeal ' + CAST(@i AS NVARCHAR),
            'A delicious and healthy homemade meal option #' + CAST(@i AS NVARCHAR),
            150 + (ABS(CHECKSUM(NEWID()) % 20) * 10), -- Random price between 150-350
            @ProdCatId,
            @VendorId,
            '/images/meals/default-food.jpg',
            400 + (ABS(CHECKSUM(NEWID()) % 400)), -- 400-800 calories
            '30-45 mins',
            4.0 + (ABS(CHECKSUM(NEWID()) % 10) / 10.0), -- 4.0-5.0 rating
            'Active',
            GETDATE(),
            @ProdCatId,
            @MealCatId,
            CASE WHEN @ProdCatId = 1 THEN 'Veg' ELSE 'Non-Veg' END,
            NULL,
            CASE WHEN @i % 3 = 0 THEN 1 ELSE 0 END,
            20 + (ABS(CHECKSUM(NEWID()) % 20)),
            40 + (ABS(CHECKSUM(NEWID()) % 40)),
            10 + (ABS(CHECKSUM(NEWID()) % 15))
        );
        SET @i = @i + 1;
    END

    -- 5. Insert 100 Recipes
    PRINT 'Inserting 100 Recipes...';
    DECLARE @FoodId INT;
    DECLARE food_cursor CURSOR FOR SELECT Id FROM Foods;
    OPEN food_cursor;
    FETCH NEXT FROM food_cursor INTO @FoodId;
    SET @i = 1;
    WHILE @@FETCH_STATUS = 0 AND @i <= 100
    BEGIN
        INSERT INTO [Recipes] (FoodId, Ingredients, Steps, ImagePath, CreatedAt)
        VALUES (
            @FoodId,
            'Ingredient 1, Ingredient 2, Ingredient 3, Secret Spice #' + CAST(@i AS NVARCHAR),
            'Step 1: Prep ingredients.&#10;Step 2: Cook on medium flame.&#10;Step 3: Garnish and serve.',
            NULL,
            GETDATE()
        );
        SET @i = @i + 1;
        FETCH NEXT FROM food_cursor INTO @FoodId;
    END
    CLOSE food_cursor;
    DEALLOCATE food_cursor;

    -- 6. Insert 100 Orders
    PRINT 'Inserting 100 Orders...';
    SET @i = 1;
    DECLARE @UserId INT, @OrderVendorId INT, @Amount DECIMAL(18,2), @Comm DECIMAL(18,2), @VendAmt DECIMAL(18,2);
    WHILE @i <= 100
    BEGIN
        SELECT TOP 1 @UserId = Id FROM UserSignup ORDER BY NEWID();
        SELECT TOP 1 @OrderVendorId = VendorId FROM VendorSignup WHERE IsApproved = 1 ORDER BY NEWID();
        SET @Amount = 200 + (ABS(CHECKSUM(NEWID()) % 500));
        SET @Comm = @Amount * 0.10; -- 10% commission
        SET @VendAmt = @Amount - @Comm;

        INSERT INTO [OrderTable] (UserId, TotalItems, PickupSlot, TotalCalories, PaymentStatus, Status, IsFlagged, IsResolved, CreatedAt, UpdatedAt, OrderType, DeliveryAddress, TotalAmount, CommissionAmount, VendorAmount, VendorId, Version, TrackingProgress)
        VALUES (
            @UserId,
            ABS(CHECKSUM(NEWID()) % 3) + 1,
            '12:00 - 01:00 PM',
            500 + (ABS(CHECKSUM(NEWID()) % 500)),
            CASE WHEN @i % 4 = 0 THEN 'Pending' ELSE 'Completed' END,
            CASE WHEN @i % 10 = 0 THEN 'Cancelled' ELSE 'Delivered' END,
            0,
            0,
            DATEADD(HOUR, -ABS(CHECKSUM(NEWID()) % 48), GETDATE()),
            GETDATE(),
            'Delivery',
            'Customer Home Address ' + CAST(@i AS NVARCHAR),
            @Amount,
            @Comm,
            @VendAmt,
            @OrderVendorId,
            1,
            100
        );
        SET @i = @i + 1;
    END

    -- Correcting "Chicken Masala Bowl" categorization
    UPDATE Foods SET FoodType = 'Corporate' WHERE Name LIKE '%Chicken Masala%';
    UPDATE Foods SET FoodType = 'Student' WHERE Name = 'Grilled Tofu & Sprout Salad';

    -- Seed Recipes for specific items
    INSERT INTO Recipes (FoodId, Ingredients, Steps, CreatedAt)
    SELECT Id, '200g Chicken, Masala Spices, Onion, Tomato, Ginger-Garlic Paste, 1 tbsp Oil', '1. Sauté onions and ginger-garlic paste. 2. Add chicken and spices. 3. Cook until tender. 4. Garnish and serve.', GETDATE()
    FROM Foods WHERE Name = 'Chicken Masala Bowl' AND NOT EXISTS (SELECT 1 FROM Recipes WHERE FoodId = Foods.Id);

    INSERT INTO Recipes (FoodId, Ingredients, Steps, CreatedAt)
    SELECT Id, 'Moong Dal, Rice, Turmeric, Cumin, Ghee, Salt', '1. Wash dal and rice. 2. Pressure cook with turmeric and salt. 3. Temper with cumin and ghee.', GETDATE()
    FROM Foods WHERE Name LIKE '%Dal Khichdi%' AND NOT EXISTS (SELECT 1 FROM Recipes WHERE FoodId = Foods.Id);

    COMMIT TRANSACTION;
    PRINT 'Bulk insert completed successfully!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred: ' + ERROR_MESSAGE();
END CATCH
