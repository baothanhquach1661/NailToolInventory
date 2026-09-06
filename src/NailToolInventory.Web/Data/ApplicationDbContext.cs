using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NailToolInventory.Models;

namespace NailToolInventory.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products =>
        Set<Product>();

    public DbSet<InventoryTransaction> InventoryTransactions =>
        Set<InventoryTransaction>();

    public DbSet<InventoryLocation> InventoryLocations =>
        Set<InventoryLocation>();

    public DbSet<InventoryLevel> InventoryLevels =>
        Set<InventoryLevel>();

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

        modelBuilder.Entity<InventoryLocation>(entity =>
        {
            entity.HasKey(location => location.Id);

            entity.HasIndex(location => location.Code)
                .IsUnique();

            entity.Property(location => location.Code)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(location => location.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(location => location.Address)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<InventoryLevel>(entity =>
        {
            entity.HasKey(level => level.Id);

            entity.HasIndex(level => new
            {
                level.ProductId,
                level.InventoryLocationId
            })
            .IsUnique();

            entity.HasOne(level => level.Product)
                .WithMany()
                .HasForeignKey(level => level.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(level => level.Location)
                .WithMany()
                .HasForeignKey(
                    level => level.InventoryLocationId)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.Property(
                    transaction =>
                        transaction.PerformedByUserId)
                .HasMaxLength(450);

            entity.Property(
                    transaction =>
                        transaction.PerformedByName)
                .HasMaxLength(256);

            entity.HasOne(transaction => transaction.Product)
                .WithMany()
                .HasForeignKey(
                    transaction => transaction.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(transaction => transaction.Location)
                .WithMany()
                .HasForeignKey(
                    transaction =>
                        transaction.InventoryLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(
                    transaction =>
                        transaction.PerformedByUser)
                .WithMany()
                .HasForeignKey(
                    transaction =>
                        transaction.PerformedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(transaction => new
            {
                transaction.ProductId,
                transaction.CreatedAtUtc
            });

            entity.HasIndex(transaction => new
            {
                transaction.InventoryLocationId,
                transaction.CreatedAtUtc
            });
        });
    }
}