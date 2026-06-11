using CompiledQueriesDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace CompiledQueriesDemo.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("customers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Age).IsRequired();

                // 为 Name + Age 创建复合索引
                entity.HasIndex(e => new { e.Name, e.Age });
            });
        }
    }
}
