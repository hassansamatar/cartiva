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
                new Category { Id = 15, Name = "Electronics", DisplayOrder = 1 },
                new Category { Id = 19, Name = "Books", DisplayOrder = 2 }
                );
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 10, Name = "Shirt", Colour="White" , Size ="M", Description="Good Quality", Price = 799, ImageUrl ="\root/shart.png", CategoryId =15},
               new Product { Id = 20, Name = "T-Shirt", Colour = "Black", Size = "L", Description = "Good Quality", Price = 299, ImageUrl = "\root/Tshart.png", CategoryId =19 }
                );
        }

    }
}
