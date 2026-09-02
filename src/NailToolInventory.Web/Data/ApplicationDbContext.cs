using Microsoft.EntityFrameworkCore;
using NailToolInventory.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace NailToolInventory.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }


    public DbSet<Product> Products => Set<Product>();

    public DbSet<InventoryTransaction> InventoryTransactions =>
        Set<InventoryTransaction>();


    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
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


        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);

            entity.Property(transaction => transaction.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(transaction => transaction.Reference)
                .HasMaxLength(100);

            entity.Property(transaction => transaction.Notes)
                .HasMaxLength(500);

            entity.HasOne(transaction => transaction.Product)
                .WithMany()
                .HasForeignKey(transaction => transaction.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(transaction => new
            {
                transaction.ProductId,
                transaction.CreatedAtUtc
            });
        });
    }
}