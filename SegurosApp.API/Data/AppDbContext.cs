using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Models;

namespace SegurosApp.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<DocumentScan> DocumentScans { get; set; }
        public DbSet<PricingTier> PricingTiers { get; set; }
        public DbSet<MonthlyBilling> MonthlyBilling { get; set; }
        public DbSet<BillingItem> BillingItems { get; set; }
        public DbSet<DailyMetrics> DailyMetrics { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.PasswordHash).HasMaxLength(255);
                entity.Property(e => e.CompanyName).HasMaxLength(200);
            });

            modelBuilder.Entity<DocumentScan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => e.FileMd5Hash);

                entity.Property(e => e.SuccessRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.ExtractedData).HasColumnType("TEXT");
                entity.Property(e => e.ErrorMessage).HasColumnType("TEXT");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.DocumentScans)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PricingTier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PricePerPoliza).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TierName).HasMaxLength(100);
            });

            modelBuilder.Entity<MonthlyBilling>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.BillingYear, e.BillingMonth }).IsUnique();

                entity.Property(e => e.PricePerPoliza).HasColumnType("decimal(10,2)");
                entity.Property(e => e.SubTotal).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.MonthlyBillings)
                    .HasForeignKey(e => e.UserId);

                entity.HasOne(e => e.AppliedTier)
                    .WithMany(t => t.MonthlyBillings)
                    .HasForeignKey(e => e.AppliedTierId);
            });

            modelBuilder.Entity<BillingItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PricePerPoliza).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");

                // Relaciones
                entity.HasOne(e => e.MonthlyBilling)
                    .WithMany(b => b.BillingItems)
                    .HasForeignKey(e => e.MonthlyBillingId);

                entity.HasOne(e => e.DocumentScan)
                    .WithOne(d => d.BillingItem)
                    .HasForeignKey<BillingItem>(e => e.DocumentScanId);
            });

            modelBuilder.Entity<DailyMetrics>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();

                entity.Property(e => e.AvgSuccessRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.EstimatedRevenue).HasColumnType("decimal(10,2)");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.DailyMetrics)
                    .HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).HasMaxLength(100);
                entity.Property(e => e.Details).HasColumnType("TEXT");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}