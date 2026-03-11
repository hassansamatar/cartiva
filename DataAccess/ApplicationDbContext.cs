using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;

namespace DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ======================
        // DbSets
        // ======================
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }

        // ======================
        // Configure relationships
        // ======================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map table names explicitly (optional)
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<ProductVariant>().ToTable("ProductVariants");

            // Product → ProductVariants (one-to-many)
            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // delete variants if product is deleted

            // Optional: set string lengths & required fields
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<Product>()
                .Property(p => p.Brand)
                .IsRequired();

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Color)
                .IsRequired();

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Size)
                .IsRequired();

            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .IsRequired();
        }
    }
}