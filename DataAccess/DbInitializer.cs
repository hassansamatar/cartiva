using DataAccess;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext _db)
    {
        _db.Database.Migrate(); // ensure migrations are applied

        // ======================
        // Seed Categories
        // ======================
        if (!_db.Categories.Any())
        {
            _db.Categories.AddRange(
                new Category { Name = "Men" },
                new Category { Name = "Women" },
                new Category { Name = "Kids" }
            );
            _db.SaveChanges();
        }

        // ======================
        // Seed Products
        // ======================
        if (!_db.Products.Any())
        {
            var men = _db.Categories.First(c => c.Name == "Men");
            var women = _db.Categories.First(c => c.Name == "Women");

            var products = new List<Product>
            {
                new Product
                {
                    Name = "Smartphone",
                    Brand = "TechBrand",
                    CategoryId = men.Id,
                    Description = "Latest smartphone with amazing features",
                    ImageUrl = null
                },
                new Product
                {
                    Name = "T-Shirt",
                    Brand = "FashionCo",
                    CategoryId = women.Id,
                    Description = "Comfortable cotton t-shirt",
                    ImageUrl = null
                }
            };

            _db.Products.AddRange(products);
            _db.SaveChanges();
        }

        // ======================
        // Seed ProductVariants
        // ======================
        if (!_db.ProductVariants.Any())
        {
            var smartphone = _db.Products.First(p => p.Name == "Smartphone");
            var tshirt = _db.Products.First(p => p.Name == "T-Shirt");

            _db.ProductVariants.AddRange(
                // Smartphone variants
                new ProductVariant { ProductId = smartphone.Id, Color = "Black", Size = "128GB", Price = 699, Stock = 50 },
                new ProductVariant { ProductId = smartphone.Id, Color = "Silver", Size = "256GB", Price = 899, Stock = 30 },

                // T-Shirt variants
                new ProductVariant { ProductId = tshirt.Id, Color = "Red", Size = "M", Price = 25, Stock = 100 },
                new ProductVariant { ProductId = tshirt.Id, Color = "Blue", Size = "L", Price = 25, Stock = 80 }
            );

            _db.SaveChanges();
        }
    }
}