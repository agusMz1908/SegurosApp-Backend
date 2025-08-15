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

    }
}