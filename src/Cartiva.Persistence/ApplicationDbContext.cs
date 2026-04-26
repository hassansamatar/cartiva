using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Cartiva.Domain;

namespace Cartiva.Persistence
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole, string>
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

        public DbSet<SizeSystem> SizeSystems { get; set; }
        public DbSet<SizeValue> SizeValues { get; set; }

        public DbSet<Company> Companies { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<ProcessedStripeEvent> ProcessedStripeEvents { get; set; }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLine> InvoiceLines { get; set; }
        public DbSet<InvoicePayment> InvoicePayments { get; set; }
        public DbSet<CreditNote> CreditNotes { get; set; }
        public DbSet<CreditNoteLine> CreditNoteLines { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // ======================
        // Model configuration
        // ======================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ======================
            // IMPORTANT FIX HERE 🔥
            // ======================
            modelBuilder.Entity<ProductVariant>(e =>
            {
                // ❌ DO NOT map Price anymore (this caused your error)
                e.Ignore(p => p.Price);

                e.Property(p => p.PriceExVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.VatRate).HasColumnType("decimal(5,2)");
                e.Property(p => p.DiscountPercent).HasColumnType("decimal(5,2)");
            });

            modelBuilder.Entity<OrderDetail>(e =>
            {
                e.Property(p => p.Price).HasColumnType("decimal(18,2)");
                e.Property(p => p.PriceExVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.PriceIncVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.VatRate).HasColumnType("decimal(5,2)");
                e.Property(p => p.DiscountPercent).HasColumnType("decimal(5,2)");
                e.Property(p => p.UnitDiscountAmount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<OrderHeader>(e =>
            {
                e.Property(p => p.OrderTotal).HasColumnType("decimal(18,2)");
                e.Property(p => p.SubtotalExVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.TotalVatAmount).HasColumnType("decimal(18,2)");
                e.Property(p => p.TotalDiscountAmount).HasColumnType("decimal(18,2)");
                e.Property(p => p.ShippingCostExVat).HasColumnType("decimal(18,2)");
                e.Property(p => p.ShippingVatAmount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<ReturnRequest>()
                .Property(p => p.RefundAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Shipment>()
                .Property(p => p.Weight)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<OrderHeader>(e =>
            {
                e.Property(p => p.OrderStatus).HasConversion<string>();
                e.Property(p => p.PaymentStatus).HasConversion<string>();
            });

            modelBuilder.Entity<Shipment>(e =>
            {
                e.Property(p => p.ShipmentStatus).HasConversion<string>();
            });

            modelBuilder.Entity<ReturnRequest>(e =>
            {
                e.Property(p => p.Status).HasConversion<string>();
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

            modelBuilder.Entity<InvoicePayment>()
                .HasIndex(p => p.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            modelBuilder.Entity<CreditNote>()
                .HasIndex(c => c.CreditNoteNumber)
                .IsUnique();

            modelBuilder.Entity<CreditNoteLine>()
                .HasOne(cl => cl.OriginalInvoiceLine)
                .WithMany()
                .HasForeignKey(cl => cl.OriginalInvoiceLineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.ProductVariant)
                .WithMany()
                .HasForeignKey(il => il.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);

            // ======================
            // NOTIFICATION
            // ======================
            modelBuilder.Entity<Notification>(e =>
            {
                e.HasKey(n => n.Id);
                e.Property(n => n.Recipient).IsRequired().HasMaxLength(256);
                e.Property(n => n.Subject).HasMaxLength(500);
                e.Property(n => n.ErrorMessage).HasMaxLength(2000);
                e.Property(n => n.UserId).HasMaxLength(450);
                e.Property(n => n.ReferenceId).HasMaxLength(100);
                e.Property(n => n.ReferenceType).HasMaxLength(100);

                e.HasIndex(n => n.Status);
                e.HasIndex(n => n.UserId);
                e.HasIndex(n => new { n.ReferenceId, n.ReferenceType });
                e.HasIndex(n => n.CreatedAt);
            });
        }
    }
}