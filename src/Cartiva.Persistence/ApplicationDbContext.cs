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

        // Invoice system
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLine> InvoiceLines { get; set; }
        public DbSet<InvoicePayment> InvoicePayments { get; set; }
        public DbSet<CreditNote> CreditNotes { get; set; }
        public DbSet<CreditNoteLine> CreditNoteLines { get; set; }

        // ======================
        // Configure relationships
        // ======================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ======================
            // DECIMAL PRECISION CONFIGURATION
            // ======================

            // ProductVariant pricing
            modelBuilder.Entity<ProductVariant>(e =>
            {
                e.Property(p => p.Price).HasColumnType("decimal(18,2)");
                e.Property(p => p.PriceExVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.VatRate).HasColumnType("decimal(5,2)");
                e.Property(p => p.DiscountPercent).HasColumnType("decimal(5,2)");
            });

            // OrderDetail pricing
            modelBuilder.Entity<OrderDetail>(e =>
            {
                e.Property(p => p.Price).HasColumnType("decimal(18,2)");
                e.Property(p => p.PriceExVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.PriceIncVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.VatRate).HasColumnType("decimal(5,2)");
                e.Property(p => p.DiscountPercent).HasColumnType("decimal(5,2)");
                e.Property(p => p.UnitDiscountAmount).HasColumnType("decimal(18,2)");
            });

            // OrderHeader totals
            modelBuilder.Entity<OrderHeader>(e =>
            {
                e.Property(p => p.OrderTotal).HasColumnType("decimal(18,2)");
                e.Property(p => p.SubtotalExVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.TotalVatAmount).HasColumnType("decimal(18,2)");
                e.Property(p => p.TotalDiscountAmount).HasColumnType("decimal(18,2)");
                e.Property(p => p.ShippingCostExVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.ShippingVatAmount).HasColumnType("decimal(18,2)");
            });

            // ReturnRequest
            modelBuilder.Entity<ReturnRequest>(e =>
            {
                e.Property(p => p.RefundAmount).HasColumnType("decimal(18,2)");
            });

            // Shipment
            modelBuilder.Entity<Shipment>(e =>
            {
                e.Property(p => p.Weight).HasColumnType("decimal(10,2)");
            });

            // ======================
            // RELATIONSHIPS
            // ======================

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

            // Invoice configurations
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.OrderHeader)
                .WithMany()
                .HasForeignKey(i => i.OrderHeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.Lines)
                .WithOne(l => l.Invoice)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.CreditNotes)
                .WithOne(c => c.OriginalInvoice)
                .HasForeignKey(c => c.OriginalInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CreditNote>()
                .HasMany(c => c.Lines)
                .WithOne(l => l.CreditNote)
                .HasForeignKey(l => l.CreditNoteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CreditNote>()
                .HasOne(c => c.ReturnRequest)
                .WithMany()
                .HasForeignKey(c => c.ReturnRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            // Invoice payment idempotency key index
            modelBuilder.Entity<InvoicePayment>()
                .HasIndex(p => p.IdempotencyKey)
                .IsUnique();

            // Invoice number unique index
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // Credit note number unique index
            modelBuilder.Entity<CreditNote>()
                .HasIndex(c => c.CreditNoteNumber)
                .IsUnique();

            // CreditNoteLine to InvoiceLine relationship
            modelBuilder.Entity<CreditNoteLine>()
                .HasOne(cl => cl.OriginalInvoiceLine)
                .WithMany()
                .HasForeignKey(cl => cl.OriginalInvoiceLineId)
                .OnDelete(DeleteBehavior.Restrict);

            // InvoiceLine to ProductVariant relationship (optional)
            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.ProductVariant)
                .WithMany()
                .HasForeignKey(il => il.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}