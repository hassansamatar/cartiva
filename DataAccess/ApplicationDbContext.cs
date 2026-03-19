using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Models;

namespace DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
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

        // Size management tables
        public DbSet<SizeSystem> SizeSystems { get; set; }
        public DbSet<SizeValue> SizeValues { get; set; }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // ======================
        // Configure relationships
        // ======================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ======================
            // Table Mappings
            // ======================
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<ProductVariant>().ToTable("ProductVariants");
            modelBuilder.Entity<SizeSystem>().ToTable("SizeSystems");
            modelBuilder.Entity<SizeValue>().ToTable("SizeValues");

            // ======================
            // Relationships
            // ======================

            // Product → ProductVariants (one-to-many)
            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // delete variants if product is deleted

            // SizeSystem → SizeValues (one-to-many)
            modelBuilder.Entity<SizeValue>()
                .HasOne(sv => sv.SizeSystem)
                .WithMany(ss => ss.SizeValues)
                .HasForeignKey(sv => sv.SizeSystemId)
                .OnDelete(DeleteBehavior.Cascade); // delete size values if size system is deleted

            // ProductVariant → SizeValue (many-to-one)
            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.SizeValue)
                .WithMany(sv => sv.ProductVariants)
                .HasForeignKey(v => v.SizeValueId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting sizes that are in use

            // FIXED: Category → SizeSystem relationship (many-to-one, not one-to-one)
            modelBuilder.Entity<Category>()
                .HasOne(c => c.DefaultSizeSystem)
                .WithMany(ss => ss.Categories)  // SizeSystem can have many Categories
                .HasForeignKey(c => c.SizeSystemId)
                .OnDelete(DeleteBehavior.SetNull); // If size system deleted, set SizeSystemId to null

            // ======================
            // Indexes for Performance
            // ======================

            // Prevent duplicate variants (same product, size, color)
            modelBuilder.Entity<ProductVariant>()
                .HasIndex(v => new { v.ProductId, v.SizeValueId, v.Color })
                .IsUnique();

            // Optimize size value lookups
            modelBuilder.Entity<SizeValue>()
                .HasIndex(sv => new { sv.SizeSystemId, sv.SortOrder });

            modelBuilder.Entity<SizeValue>()
                .HasIndex(sv => sv.Value);

            // ======================
            // Property Configurations
            // ======================

            // Product configuration
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<Product>()
                .Property(p => p.Brand)
                .IsRequired();

            // ProductVariant configuration
            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Color)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Price)
                .HasPrecision(18, 2); // For currency

            // Make SizeValueId nullable for products without sizes (like accessories)
            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.SizeValueId)
                .IsRequired(false);  // Allow null for products without sizes

            // SizeSystem configuration
            modelBuilder.Entity<SizeSystem>()
                .Property(ss => ss.Name)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SizeSystem>()
                .Property(ss => ss.SizeType)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<SizeSystem>()
                .Property(ss => ss.IconClass)
                .HasMaxLength(50);

            modelBuilder.Entity<SizeSystem>()
                .Property(ss => ss.AlertClass)
                .HasMaxLength(50);

            // SizeValue configuration
            modelBuilder.Entity<SizeValue>()
                .Property(sv => sv.Value)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SizeValue>()
                .Property(sv => sv.DisplayText)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SizeValue>()
                .Property(sv => sv.Description)
                .HasMaxLength(200);

            // Category configuration
            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(30)
                .IsRequired();

            // ======================
            // REMOVED ALL SEED DATA
            // ======================
            // Seed data is now handled by DbInitializer class
        }
    }
}