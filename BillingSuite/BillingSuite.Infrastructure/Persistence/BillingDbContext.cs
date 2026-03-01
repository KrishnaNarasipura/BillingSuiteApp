using BillingSuite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BillingSuite.Infrastructure.Persistence;


public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<TaxSettings> TaxSettings => Set<TaxSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Gstin).HasMaxLength(30);
        });

        modelBuilder.Entity<CompanySettings>(b =>
        {
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<TaxSettings>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.TaxPercent).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.InvoiceNumber).HasMaxLength(30).IsRequired();
            b.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.NetAmount).HasColumnType("decimal(18,2)");
            b.HasOne(x => x.Customer).WithMany(v => v.Invoices).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<InvoiceItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            b.HasOne(x => x.Invoice).WithMany(i => i.Items).HasForeignKey(x => x.InvoiceId);
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired();
            b.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.NetAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.AdvanceReceived).HasColumnType("decimal(18,2)");
            b.HasOne(x => x.Customer).WithMany(v => v.Orders).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            b.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId);
        });
    }
}

