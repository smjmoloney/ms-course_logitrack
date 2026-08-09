using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ms_course_logitrack.Models;
using ms_course_logitrack.Auth;

namespace ms_course_logitrack.Data
{
    public class LogiTrackContext : IdentityDbContext<ApplicationUser>
    {
        public LogiTrackContext(DbContextOptions<LogiTrackContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}