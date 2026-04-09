using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using ApplicationUtility;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext _db,
                            UserManager<ApplicationUser> userManager,
                            RoleManager<IdentityRole> roleManager)
    {
        _db.Database.Migrate(); // ensure migrations are applied

        // ======================
        // Seed Roles (if they don't exist)
        // ======================
        // ======================
        // Seed Roles (if they don't exist)
        // ======================
        var roles = new[] { SD.Role_Customer, SD.Role_Employee, SD.Role_Admin, SD.Role_Company };
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).Result)
            {
                roleManager.CreateAsync(new IdentityRole(role)).Wait();
            }
        }

        // ======================
        // Seed Admin User (recreate if exists to ensure correct password)
        // ======================
        var adminEmail = "admin@cartiva.com";
        var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
        if (adminUser != null)
        {
            // Delete existing admin user to recreate with correct password
            userManager.DeleteAsync(adminUser).Wait();
        }

        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Name = "Admin",
            Country = "Norway",
            IsActive = true
        };
        var createResult = userManager.CreateAsync(adminUser, "Admin12#").Result;
        if (createResult.Succeeded)
        {
            userManager.AddToRoleAsync(adminUser, SD.Role_Admin).Wait();
        }
        else
        {
            // Log errors (optional)
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            Console.WriteLine($"Admin user creation failed: {errors}");
        }

        // ======================
        // Seed Categories
        // ======================
        if (!_db.Categories.Any())
        {
            _db.Categories.AddRange(
                new Category { Name = "Men" },
                new Category { Name = "Women" },
                new Category { Name = "Kids" },
                new Category { Name = "Suits" },
                new Category { Name = "Sportswear" },
                new Category { Name = "Accessories" }
            );
            _db.SaveChanges();
        }

        // ======================
        // Seed SizeSystems
        // ======================
        if (!_db.SizeSystems.Any())
        {
            var sizeSystems = new List<SizeSystem>
            {
                new SizeSystem { Name = "Adult Regular", SizeType = "Regular", Description = "Regular Sizes (S-XXL)", IconClass = "bi-person", AlertClass = "alert-info" },
                new SizeSystem { Name = "Women Regular", SizeType = "Regular", Description = "Women's Regular Sizes (XS-XL)", IconClass = "bi-person", AlertClass = "alert-info" },
                new SizeSystem { Name = "Adult Suit", SizeType = "Suit", Description = "Suit Sizes (44-56 EU)", IconClass = "bi-person-badge", AlertClass = "alert-primary" },
                new SizeSystem { Name = "Kids", SizeType = "Kid", Description = "Kids Sizes (50-176 cm)", IconClass = "bi-emoji-smile", AlertClass = "alert-success" },
                new SizeSystem { Name = "Shoe Sizes", SizeType = "Shoe", Description = "Shoe Sizes (EU 35-46)", IconClass = "bi-box", AlertClass = "alert-warning" }
            };
            _db.SizeSystems.AddRange(sizeSystems);
            _db.SaveChanges();
        }

        // ======================
        // Update Categories with SizeSystemId
        // ======================
        var menCategory = _db.Categories.First(c => c.Name == "Men");
        var womenCategory = _db.Categories.First(c => c.Name == "Women");
        var kidsCategory = _db.Categories.First(c => c.Name == "Kids");
        var suitsCategory = _db.Categories.First(c => c.Name == "Suits");
        var sportswearCategory = _db.Categories.First(c => c.Name == "Sportswear");
        var accessoriesCategory = _db.Categories.First(c => c.Name == "Accessories");

        var adultRegular = _db.SizeSystems.First(ss => ss.Name == "Adult Regular");
        var womenRegular = _db.SizeSystems.First(ss => ss.Name == "Women Regular");
        var adultSuit = _db.SizeSystems.First(ss => ss.Name == "Adult Suit");
        var kids = _db.SizeSystems.First(ss => ss.Name == "Kids");
        var shoeSizes = _db.SizeSystems.First(ss => ss.Name == "Shoe Sizes");

        menCategory.SizeSystemId = adultRegular.Id;
        womenCategory.SizeSystemId = womenRegular.Id;
        kidsCategory.SizeSystemId = kids.Id;
        suitsCategory.SizeSystemId = adultSuit.Id;
        sportswearCategory.SizeSystemId = shoeSizes.Id;
        // Accessories category has no size system (null)

        _db.Categories.UpdateRange(menCategory, womenCategory, kidsCategory, suitsCategory, sportswearCategory, accessoriesCategory);
        _db.SaveChanges();

        // ======================
        // Seed SizeValues
        // ======================
        if (!_db.SizeValues.Any())
        {
            var sizeValues = new List<SizeValue>
            {
                // Adult Regular
                new SizeValue { SizeSystemId = adultRegular.Id, Value = "S", DisplayText = "S", Description = "Small", SortOrder = 1 },
                new SizeValue { SizeSystemId = adultRegular.Id, Value = "M", DisplayText = "M", Description = "Medium", SortOrder = 2 },
                new SizeValue { SizeSystemId = adultRegular.Id, Value = "L", DisplayText = "L", Description = "Large", SortOrder = 3 },
                new SizeValue { SizeSystemId = adultRegular.Id, Value = "XL", DisplayText = "XL", Description = "Extra Large", SortOrder = 4 },
                new SizeValue { SizeSystemId = adultRegular.Id, Value = "XXL", DisplayText = "XXL", Description = "Double Extra Large", SortOrder = 5 },

                // Women Regular
                new SizeValue { SizeSystemId = womenRegular.Id, Value = "XS", DisplayText = "XS", Description = "Extra Small", SortOrder = 1 },
                new SizeValue { SizeSystemId = womenRegular.Id, Value = "S", DisplayText = "S", Description = "Small", SortOrder = 2 },
                new SizeValue { SizeSystemId = womenRegular.Id, Value = "M", DisplayText = "M", Description = "Medium", SortOrder = 3 },
                new SizeValue { SizeSystemId = womenRegular.Id, Value = "L", DisplayText = "L", Description = "Large", SortOrder = 4 },
                new SizeValue { SizeSystemId = womenRegular.Id, Value = "XL", DisplayText = "XL", Description = "Extra Large", SortOrder = 5 },

                // Adult Suit
                new SizeValue { SizeSystemId = adultSuit.Id, Value = "44", DisplayText = "44", Description = "UK 34\" / XS", SortOrder = 1 },
                new SizeValue { SizeSystemId = adultSuit.Id, Value = "46", DisplayText = "46", Description = "UK 36\" / S", SortOrder = 2 },
                new SizeValue { SizeSystemId = adultSuit.Id, Value = "48", DisplayText = "48", Description = "UK 38\" / M", SortOrder = 3 },
                new SizeValue { SizeSystemId = adultSuit.Id, Value = "50", DisplayText = "50", Description = "UK 40\" / L", SortOrder = 4 },
                new SizeValue { SizeSystemId = adultSuit.Id, Value = "52", DisplayText = "52", Description = "UK 42\" / XL", SortOrder = 5 },
                new SizeValue { SizeSystemId = adultSuit.Id, Value = "54", DisplayText = "54", Description = "UK 44\" / XXL", SortOrder = 6 },
                new SizeValue { SizeSystemId = adultSuit.Id, Value = "56", DisplayText = "56", Description = "UK 46\" / XXXL", SortOrder = 7 },

                // Kids
                new SizeValue { SizeSystemId = kids.Id, Value = "50", DisplayText = "50 cm", Description = "0-3 months", SortOrder = 1 },
                new SizeValue { SizeSystemId = kids.Id, Value = "68", DisplayText = "68 cm", Description = "0-6 months", SortOrder = 2 },
                new SizeValue { SizeSystemId = kids.Id, Value = "80", DisplayText = "80 cm", Description = "6-12 months", SortOrder = 3 },
                new SizeValue { SizeSystemId = kids.Id, Value = "92", DisplayText = "92 cm", Description = "2-3 years", SortOrder = 4 },
                new SizeValue { SizeSystemId = kids.Id, Value = "104", DisplayText = "104 cm", Description = "4-5 years", SortOrder = 5 },
                new SizeValue { SizeSystemId = kids.Id, Value = "116", DisplayText = "116 cm", Description = "6-7 years", SortOrder = 6 },
                new SizeValue { SizeSystemId = kids.Id, Value = "128", DisplayText = "128 cm", Description = "8-9 years", SortOrder = 7 },
                new SizeValue { SizeSystemId = kids.Id, Value = "140", DisplayText = "140 cm", Description = "10-11 years", SortOrder = 8 },
                new SizeValue { SizeSystemId = kids.Id, Value = "152", DisplayText = "152 cm", Description = "12-13 years", SortOrder = 9 },
                new SizeValue { SizeSystemId = kids.Id, Value = "164", DisplayText = "164 cm", Description = "14-15 years", SortOrder = 10 },
                new SizeValue { SizeSystemId = kids.Id, Value = "176", DisplayText = "176 cm", Description = "16+ years", SortOrder = 11 },

                // Shoe sizes
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "35", DisplayText = "EU 35", Description = "UK 2.5 / US 5", SortOrder = 1 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "36", DisplayText = "EU 36", Description = "UK 3.5 / US 6", SortOrder = 2 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "37", DisplayText = "EU 37", Description = "UK 4.5 / US 7", SortOrder = 3 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "38", DisplayText = "EU 38", Description = "UK 5.5 / US 8", SortOrder = 4 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "39", DisplayText = "EU 39", Description = "UK 6.5 / US 9", SortOrder = 5 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "40", DisplayText = "EU 40", Description = "UK 7.5 / US 10", SortOrder = 6 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "41", DisplayText = "EU 41", Description = "UK 8.5 / US 11", SortOrder = 7 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "42", DisplayText = "EU 42", Description = "UK 9.5 / US 12", SortOrder = 8 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "43", DisplayText = "EU 43", Description = "UK 10.5 / US 13", SortOrder = 9 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "44", DisplayText = "EU 44", Description = "UK 11.5 / US 14", SortOrder = 10 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "45", DisplayText = "EU 45", Description = "UK 12.5 / US 15", SortOrder = 11 },
                new SizeValue { SizeSystemId = shoeSizes.Id, Value = "46", DisplayText = "EU 46", Description = "UK 13.5 / US 16", SortOrder = 12 }
            };
            _db.SizeValues.AddRange(sizeValues);
            _db.SaveChanges();
        }

        // ======================
        // Seed Products
        // ======================
        if (!_db.Products.Any())
        {
            var products = new List<Product>
            {
                new Product { Name = "Classic T-Shirt", Brand = "FashionCo", CategoryId = menCategory.Id, Description = "Comfortable cotton t-shirt", ImageUrl = "/images/products/mens-tshirt.jpg" },
                new Product { Name = "Denim Jeans", Brand = "Levi's", CategoryId = menCategory.Id, Description = "Classic fit denim jeans", ImageUrl = "/images/products/jeans.jpg" },
                new Product { Name = "Summer Dress", Brand = "Zara", CategoryId = womenCategory.Id, Description = "Floral print summer dress", ImageUrl = "/images/products/dress.jpg" },
                new Product { Name = "Handbag", Brand = "Coach", CategoryId = womenCategory.Id, Description = "Elegant leather handbag", ImageUrl = "/images/products/handbag.jpg" },
                new Product { Name = "Kids Hoodie", Brand = "Puma", CategoryId = kidsCategory.Id, Description = "Warm hoodie for kids", ImageUrl = "/images/products/kids-hoodie.jpg" },
                new Product { Name = "Business Suit", Brand = "Hugo Boss", CategoryId = suitsCategory.Id, Description = "Elegant business suit", ImageUrl = "/images/products/suit.jpg" },
                new Product { Name = "Running Shoes", Brand = "Nike", CategoryId = sportswearCategory.Id, Description = "Lightweight running shoes", ImageUrl = "/images/products/shoes.jpg" },
                new Product { Name = "Leather Belt", Brand = "Gucci", CategoryId = accessoriesCategory.Id, Description = "Genuine leather belt - one size fits most", ImageUrl = "/images/products/belt.jpg" },
                new Product { Name = "Wool Scarf", Brand = "Burberry", CategoryId = accessoriesCategory.Id, Description = "Classic check pattern scarf", ImageUrl = "/images/products/scarf.jpg" }
            };
            _db.Products.AddRange(products);
            _db.SaveChanges();
        }

        // ======================
        // Seed ProductVariants
        // ======================
        if (!_db.ProductVariants.Any())
        {
            var tshirt = _db.Products.First(p => p.Name == "Classic T-Shirt");
            var jeans = _db.Products.First(p => p.Name == "Denim Jeans");
            var dress = _db.Products.First(p => p.Name == "Summer Dress");
            var handbag = _db.Products.First(p => p.Name == "Handbag");
            var hoodie = _db.Products.First(p => p.Name == "Kids Hoodie");
            var suit = _db.Products.First(p => p.Name == "Business Suit");
            var shoes = _db.Products.First(p => p.Name == "Running Shoes");
            var belt = _db.Products.First(p => p.Name == "Leather Belt");
            var scarf = _db.Products.First(p => p.Name == "Wool Scarf");

            var sizeS = _db.SizeValues.First(sv => sv.Value == "S" && sv.SizeSystem.Name == "Adult Regular");
            var sizeM = _db.SizeValues.First(sv => sv.Value == "M" && sv.SizeSystem.Name == "Adult Regular");
            var sizeL = _db.SizeValues.First(sv => sv.Value == "L" && sv.SizeSystem.Name == "Adult Regular");
            var sizeXL = _db.SizeValues.First(sv => sv.Value == "XL" && sv.SizeSystem.Name == "Adult Regular");
            var sizeXXL = _db.SizeValues.First(sv => sv.Value == "XXL" && sv.SizeSystem.Name == "Adult Regular");

            var womenXS = _db.SizeValues.First(sv => sv.Value == "XS" && sv.SizeSystem.Name == "Women Regular");
            var womenS = _db.SizeValues.First(sv => sv.Value == "S" && sv.SizeSystem.Name == "Women Regular");
            var womenM = _db.SizeValues.First(sv => sv.Value == "M" && sv.SizeSystem.Name == "Women Regular");
            var womenL = _db.SizeValues.First(sv => sv.Value == "L" && sv.SizeSystem.Name == "Women Regular");
            var womenXL = _db.SizeValues.First(sv => sv.Value == "XL" && sv.SizeSystem.Name == "Women Regular");

            var suitSize44 = _db.SizeValues.First(sv => sv.Value == "44" && sv.SizeSystem.Name == "Adult Suit");
            var suitSize46 = _db.SizeValues.First(sv => sv.Value == "46" && sv.SizeSystem.Name == "Adult Suit");
            var suitSize48 = _db.SizeValues.First(sv => sv.Value == "48" && sv.SizeSystem.Name == "Adult Suit");
            var suitSize50 = _db.SizeValues.First(sv => sv.Value == "50" && sv.SizeSystem.Name == "Adult Suit");
            var suitSize52 = _db.SizeValues.First(sv => sv.Value == "52" && sv.SizeSystem.Name == "Adult Suit");
            var suitSize54 = _db.SizeValues.First(sv => sv.Value == "54" && sv.SizeSystem.Name == "Adult Suit");

            var kidsSize104 = _db.SizeValues.First(sv => sv.Value == "104" && sv.SizeSystem.Name == "Kids");
            var kidsSize116 = _db.SizeValues.First(sv => sv.Value == "116" && sv.SizeSystem.Name == "Kids");
            var kidsSize128 = _db.SizeValues.First(sv => sv.Value == "128" && sv.SizeSystem.Name == "Kids");
            var kidsSize140 = _db.SizeValues.First(sv => sv.Value == "140" && sv.SizeSystem.Name == "Kids");

            var shoeSize39 = _db.SizeValues.First(sv => sv.Value == "39" && sv.SizeSystem.Name == "Shoe Sizes");
            var shoeSize40 = _db.SizeValues.First(sv => sv.Value == "40" && sv.SizeSystem.Name == "Shoe Sizes");
            var shoeSize41 = _db.SizeValues.First(sv => sv.Value == "41" && sv.SizeSystem.Name == "Shoe Sizes");
            var shoeSize42 = _db.SizeValues.First(sv => sv.Value == "42" && sv.SizeSystem.Name == "Shoe Sizes");
            var shoeSize43 = _db.SizeValues.First(sv => sv.Value == "43" && sv.SizeSystem.Name == "Shoe Sizes");
            var shoeSize44 = _db.SizeValues.First(sv => sv.Value == "44" && sv.SizeSystem.Name == "Shoe Sizes");
            var shoeSize45 = _db.SizeValues.First(sv => sv.Value == "45" && sv.SizeSystem.Name == "Shoe Sizes");
            var shoeSize46 = _db.SizeValues.First(sv => sv.Value == "46" && sv.SizeSystem.Name == "Shoe Sizes");

            var variants = new List<ProductVariant>
            {
                // T‑Shirt
                new ProductVariant { ProductId = tshirt.Id, Color = "Red", SizeValueId = sizeS.Id, Price = 299.99m, Stock = 50 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Red", SizeValueId = sizeM.Id, Price = 299.99m, Stock = 75 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Red", SizeValueId = sizeL.Id, Price = 299.99m, Stock = 45 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Blue", SizeValueId = sizeS.Id, Price = 299.99m, Stock = 40 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Blue", SizeValueId = sizeM.Id, Price = 299.99m, Stock = 60 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Blue", SizeValueId = sizeL.Id, Price = 299.99m, Stock = 55 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Green", SizeValueId = sizeM.Id, Price = 299.99m, Stock = 35 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Green", SizeValueId = sizeL.Id, Price = 299.99m, Stock = 30 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Black", SizeValueId = sizeM.Id, Price = 329.99m, Stock = 80 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Black", SizeValueId = sizeL.Id, Price = 329.99m, Stock = 70 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Black", SizeValueId = sizeXL.Id, Price = 349.99m, Stock = 40 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Black", SizeValueId = sizeXXL.Id, Price = 349.99m, Stock = 25 },
                new ProductVariant { ProductId = tshirt.Id, Color = "White", SizeValueId = sizeM.Id, Price = 299.99m, Stock = 65 },
                new ProductVariant { ProductId = tshirt.Id, Color = "White", SizeValueId = sizeL.Id, Price = 299.99m, Stock = 55 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Navy", SizeValueId = sizeM.Id, Price = 299.99m, Stock = 45 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Navy", SizeValueId = sizeL.Id, Price = 299.99m, Stock = 40 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Gray", SizeValueId = sizeM.Id, Price = 299.99m, Stock = 35 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Gray", SizeValueId = sizeL.Id, Price = 299.99m, Stock = 30 },

                // Jeans
                new ProductVariant { ProductId = jeans.Id, Color = "Blue", SizeValueId = sizeS.Id, Price = 799.99m, Stock = 40 },
                new ProductVariant { ProductId = jeans.Id, Color = "Blue", SizeValueId = sizeM.Id, Price = 799.99m, Stock = 55 },
                new ProductVariant { ProductId = jeans.Id, Color = "Blue", SizeValueId = sizeL.Id, Price = 799.99m, Stock = 50 },
                new ProductVariant { ProductId = jeans.Id, Color = "Blue", SizeValueId = sizeXL.Id, Price = 799.99m, Stock = 35 },
                new ProductVariant { ProductId = jeans.Id, Color = "Black", SizeValueId = sizeM.Id, Price = 849.99m, Stock = 45 },
                new ProductVariant { ProductId = jeans.Id, Color = "Black", SizeValueId = sizeL.Id, Price = 849.99m, Stock = 40 },
                new ProductVariant { ProductId = jeans.Id, Color = "Black", SizeValueId = sizeXL.Id, Price = 849.99m, Stock = 25 },
                new ProductVariant { ProductId = jeans.Id, Color = "Gray", SizeValueId = sizeM.Id, Price = 799.99m, Stock = 30 },
                new ProductVariant { ProductId = jeans.Id, Color = "Gray", SizeValueId = sizeL.Id, Price = 799.99m, Stock = 25 },

                // Dress
                new ProductVariant { ProductId = dress.Id, Color = "Red", SizeValueId = womenXS.Id, Price = 599.99m, Stock = 20 },
                new ProductVariant { ProductId = dress.Id, Color = "Red", SizeValueId = womenS.Id, Price = 599.99m, Stock = 25 },
                new ProductVariant { ProductId = dress.Id, Color = "Red", SizeValueId = womenM.Id, Price = 599.99m, Stock = 30 },
                new ProductVariant { ProductId = dress.Id, Color = "Red", SizeValueId = womenL.Id, Price = 599.99m, Stock = 20 },
                new ProductVariant { ProductId = dress.Id, Color = "Blue", SizeValueId = womenS.Id, Price = 599.99m, Stock = 28 },
                new ProductVariant { ProductId = dress.Id, Color = "Blue", SizeValueId = womenM.Id, Price = 599.99m, Stock = 32 },
                new ProductVariant { ProductId = dress.Id, Color = "Blue", SizeValueId = womenL.Id, Price = 599.99m, Stock = 25 },
                new ProductVariant { ProductId = dress.Id, Color = "Blue", SizeValueId = womenXL.Id, Price = 649.99m, Stock = 15 },
                new ProductVariant { ProductId = dress.Id, Color = "Green", SizeValueId = womenS.Id, Price = 599.99m, Stock = 18 },
                new ProductVariant { ProductId = dress.Id, Color = "Green", SizeValueId = womenM.Id, Price = 599.99m, Stock = 22 },
                new ProductVariant { ProductId = dress.Id, Color = "Black", SizeValueId = womenM.Id, Price = 649.99m, Stock = 35 },
                new ProductVariant { ProductId = dress.Id, Color = "Black", SizeValueId = womenL.Id, Price = 649.99m, Stock = 28 },
                new ProductVariant { ProductId = dress.Id, Color = "Navy", SizeValueId = womenM.Id, Price = 599.99m, Stock = 20 },
                new ProductVariant { ProductId = dress.Id, Color = "Navy", SizeValueId = womenL.Id, Price = 599.99m, Stock = 15 },

                // Handbag (no size)
                new ProductVariant { ProductId = handbag.Id, Color = "Brown", SizeValueId = null, Price = 1999.99m, Stock = 3 },
                new ProductVariant { ProductId = handbag.Id, Color = "Black", SizeValueId = null, Price = 1999.99m, Stock = 5 },
                new ProductVariant { ProductId = handbag.Id, Color = "Tan", SizeValueId = null, Price = 1899.99m, Stock = 0 },

                // Kids Hoodie
                new ProductVariant { ProductId = hoodie.Id, Color = "Blue", SizeValueId = kidsSize104.Id, Price = 349.99m, Stock = 40 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Blue", SizeValueId = kidsSize116.Id, Price = 349.99m, Stock = 35 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Blue", SizeValueId = kidsSize128.Id, Price = 349.99m, Stock = 30 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Blue", SizeValueId = kidsSize140.Id, Price = 349.99m, Stock = 25 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Red", SizeValueId = kidsSize104.Id, Price = 349.99m, Stock = 35 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Red", SizeValueId = kidsSize116.Id, Price = 349.99m, Stock = 30 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Red", SizeValueId = kidsSize128.Id, Price = 349.99m, Stock = 25 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Green", SizeValueId = kidsSize104.Id, Price = 349.99m, Stock = 20 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Green", SizeValueId = kidsSize116.Id, Price = 349.99m, Stock = 18 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Green", SizeValueId = kidsSize128.Id, Price = 349.99m, Stock = 15 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Yellow", SizeValueId = kidsSize104.Id, Price = 349.99m, Stock = 12 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Yellow", SizeValueId = kidsSize116.Id, Price = 349.99m, Stock = 8 },
                new ProductVariant { ProductId = hoodie.Id, Color = "Pink", SizeValueId = kidsSize104.Id, Price = 349.99m, Stock = 0 },

                // Suit
                new ProductVariant { ProductId = suit.Id, Color = "Black", SizeValueId = suitSize44.Id, Price = 3999.99m, Stock = 8 },
                new ProductVariant { ProductId = suit.Id, Color = "Black", SizeValueId = suitSize46.Id, Price = 3999.99m, Stock = 10 },
                new ProductVariant { ProductId = suit.Id, Color = "Black", SizeValueId = suitSize48.Id, Price = 3999.99m, Stock = 12 },
                new ProductVariant { ProductId = suit.Id, Color = "Black", SizeValueId = suitSize50.Id, Price = 3999.99m, Stock = 8 },
                new ProductVariant { ProductId = suit.Id, Color = "Black", SizeValueId = suitSize52.Id, Price = 3999.99m, Stock = 6 },
                new ProductVariant { ProductId = suit.Id, Color = "Navy", SizeValueId = suitSize46.Id, Price = 4299.99m, Stock = 7 },
                new ProductVariant { ProductId = suit.Id, Color = "Navy", SizeValueId = suitSize48.Id, Price = 4299.99m, Stock = 9 },
                new ProductVariant { ProductId = suit.Id, Color = "Navy", SizeValueId = suitSize50.Id, Price = 4299.99m, Stock = 7 },
                new ProductVariant { ProductId = suit.Id, Color = "Navy", SizeValueId = suitSize52.Id, Price = 4299.99m, Stock = 5 },
                new ProductVariant { ProductId = suit.Id, Color = "Navy", SizeValueId = suitSize54.Id, Price = 4499.99m, Stock = 3 },
                new ProductVariant { ProductId = suit.Id, Color = "Charcoal", SizeValueId = suitSize48.Id, Price = 4199.99m, Stock = 6 },
                new ProductVariant { ProductId = suit.Id, Color = "Charcoal", SizeValueId = suitSize50.Id, Price = 4199.99m, Stock = 5 },
                new ProductVariant { ProductId = suit.Id, Color = "Charcoal", SizeValueId = suitSize52.Id, Price = 4199.99m, Stock = 4 },

                // Shoes
                new ProductVariant { ProductId = shoes.Id, Color = "Black/Red", SizeValueId = shoeSize39.Id, Price = 1299.99m, Stock = 15 },
                new ProductVariant { ProductId = shoes.Id, Color = "Black/Red", SizeValueId = shoeSize40.Id, Price = 1299.99m, Stock = 20 },
                new ProductVariant { ProductId = shoes.Id, Color = "Black/Red", SizeValueId = shoeSize41.Id, Price = 1299.99m, Stock = 25 },
                new ProductVariant { ProductId = shoes.Id, Color = "Black/Red", SizeValueId = shoeSize42.Id, Price = 1299.99m, Stock = 22 },
                new ProductVariant { ProductId = shoes.Id, Color = "Black/Red", SizeValueId = shoeSize43.Id, Price = 1299.99m, Stock = 18 },
                new ProductVariant { ProductId = shoes.Id, Color = "Black/Red", SizeValueId = shoeSize44.Id, Price = 1299.99m, Stock = 15 },
                new ProductVariant { ProductId = shoes.Id, Color = "Black/Red", SizeValueId = shoeSize45.Id, Price = 1299.99m, Stock = 10 },
                new ProductVariant { ProductId = shoes.Id, Color = "Black/Red", SizeValueId = shoeSize46.Id, Price = 1299.99m, Stock = 5 },
                new ProductVariant { ProductId = shoes.Id, Color = "White/Blue", SizeValueId = shoeSize39.Id, Price = 1299.99m, Stock = 12 },
                new ProductVariant { ProductId = shoes.Id, Color = "White/Blue", SizeValueId = shoeSize40.Id, Price = 1299.99m, Stock = 18 },
                new ProductVariant { ProductId = shoes.Id, Color = "White/Blue", SizeValueId = shoeSize41.Id, Price = 1299.99m, Stock = 22 },
                new ProductVariant { ProductId = shoes.Id, Color = "White/Blue", SizeValueId = shoeSize42.Id, Price = 1299.99m, Stock = 20 },
                new ProductVariant { ProductId = shoes.Id, Color = "White/Blue", SizeValueId = shoeSize43.Id, Price = 1299.99m, Stock = 16 },
                new ProductVariant { ProductId = shoes.Id, Color = "White/Blue", SizeValueId = shoeSize44.Id, Price = 1299.99m, Stock = 12 },
                new ProductVariant { ProductId = shoes.Id, Color = "White/Blue", SizeValueId = shoeSize45.Id, Price = 1299.99m, Stock = 6 },
                new ProductVariant { ProductId = shoes.Id, Color = "All Black", SizeValueId = shoeSize40.Id, Price = 1399.99m, Stock = 14 },
                new ProductVariant { ProductId = shoes.Id, Color = "All Black", SizeValueId = shoeSize41.Id, Price = 1399.99m, Stock = 16 },
                new ProductVariant { ProductId = shoes.Id, Color = "All Black", SizeValueId = shoeSize42.Id, Price = 1399.99m, Stock = 15 },
                new ProductVariant { ProductId = shoes.Id, Color = "All Black", SizeValueId = shoeSize43.Id, Price = 1399.99m, Stock = 12 },
                new ProductVariant { ProductId = shoes.Id, Color = "All Black", SizeValueId = shoeSize44.Id, Price = 1399.99m, Stock = 8 },

                // Belt (no size)
                new ProductVariant { ProductId = belt.Id, Color = "Brown", SizeValueId = null, Price = 899.99m, Stock = 15 },
                new ProductVariant { ProductId = belt.Id, Color = "Black", SizeValueId = null, Price = 899.99m, Stock = 20 },
                new ProductVariant { ProductId = belt.Id, Color = "Tan", SizeValueId = null, Price = 899.99m, Stock = 12 },

                // Scarf (no size)
                new ProductVariant { ProductId = scarf.Id, Color = "Black Watch", SizeValueId = null, Price = 799.99m, Stock = 8 }
            };

            _db.ProductVariants.AddRange(variants);
            _db.SaveChanges();
        }
    }
}