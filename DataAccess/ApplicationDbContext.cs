using Microsoft.EntityFrameworkCore;
using Models;
using Models;
using Models;

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Producties { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure EF treats Product.Id as database-generated (IDENTITY)
            modelBuilder.Entity<Product>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Books", DisplayOrder = 2 },
                new Category { Id = 3, Name = "Clothing", DisplayOrder = 3 }
                );
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Shirt", Colour="White" , Size ="M", Description="Good Quality", Price = 799, ImageUrl ="\root/shart.png"},
               new Product { Id = 2, Name = "T-Shirt", Colour = "Black", Size = "L", Description = "Good Quality", Price = 299, ImageUrl = "\root/Tshart.png" }
                );
        }

    }
}
