using Microsoft.EntityFrameworkCore;
using InventoryApi.Models;

namespace InventoryApi.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options) { }

    public DbSet<InventorySite> InventorySites { get; set; }
    public DbSet<ItemInventory> ItemInventories { get; set; }
    public DbSet<ItemSite> ItemSites { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<TxnItemLineDetail> TxnItemLineDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ItemInventory>(entity =>
        {
            entity.Property(e => e.SalesPrice).HasPrecision(16, 6);
            entity.Property(e => e.PurchaseCost).HasPrecision(16, 6);
            entity.Property(e => e.QuantityOnHand).HasPrecision(16, 6);
            entity.Property(e => e.AverageCost).HasPrecision(16, 6);
            entity.Property(e => e.QuantityOnOrder).HasPrecision(16, 6);
            entity.Property(e => e.QuantityOnSalesOrder).HasPrecision(16, 6);
        });

        modelBuilder.Entity<ItemSite>(entity =>
        {
            entity.Property(e => e.QuantityOnHand).HasPrecision(16, 6);
            entity.Property(e => e.QuantityOnPurchaseOrders).HasPrecision(16, 6);
            entity.Property(e => e.QuantityOnSalesOrders).HasPrecision(16, 6);
            entity.Property(e => e.QuantityOnPendingTransfers).HasPrecision(16, 6);
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.Property(e => e.CreditLimit).HasPrecision(16, 6);
            entity.Property(e => e.Balance).HasPrecision(16, 6);
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.Property(e => e.AmountDue).HasPrecision(16, 6);
            entity.Property(e => e.ExchangeRate).HasPrecision(16, 6);
            entity.Property(e => e.AmountDueInHomeCurrency).HasPrecision(16, 6);
            entity.Property(e => e.OpenAmount).HasPrecision(16, 6);
        });

        modelBuilder.Entity<TxnItemLineDetail>(entity =>
        {
            entity.Property(e => e.Quantity).HasPrecision(16, 6);
            entity.Property(e => e.Amount).HasPrecision(16, 6);

            entity.HasOne<Bill>()
                  .WithMany(b => b.LineItems)
                  .HasForeignKey(l => l.IDKEY)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
