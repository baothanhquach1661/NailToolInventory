using Microsoft.EntityFrameworkCore;
using NailToolInventory.Models;

namespace NailToolInventory.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);

            entity.HasIndex(product => product.Sku)
                .IsUnique();

            entity.Property(product => product.Sku)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(product => product.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(product => product.Category)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(product => product.CostPrice)
                .HasPrecision(10, 2);

            entity.Property(product => product.SellingPrice)
                .HasPrecision(10, 2);
        });
    }
}