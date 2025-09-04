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
        public DbSet<DailyMetrics> DailyMetrics { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PricingTiers> PricingTiers { get; set; }
        public DbSet<MonthlyBilling> MonthlyBilling { get; set; }
        public DbSet<BillingItems> BillingItems { get; set; }
        public DbSet<TenantConfiguration> TenantConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(u => u.TenantConfiguration)
                      .WithMany()
                      .HasForeignKey(u => u.TenantId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TenantConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(t => t.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(t => t.CreatedBy)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(t => t.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(t => t.UpdatedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}