using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cartiva.Domain;

namespace Cartiva.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
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

        // The ApplicationUser DbSet is inherited from IdentityDbContext, so no need to declare it here.
        // public DbSet<ApplicationUser> ApplicationUsers { get; set; } // REMOVED

        public DbSet<Company> Companies { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<Cartiva.Domain.ProcessedStripeEvent> ProcessedStripeEvents { get; set; }

        // ======================
        // Configure relationships
        // ======================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.OrderDetail)
                .WithMany()
                .HasForeignKey(r => r.OrderDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.ApplicationUser)
                .WithMany()
                .HasForeignKey(r => r.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}