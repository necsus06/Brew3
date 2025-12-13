using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Brew3.Models;

public partial class Database : DbContext
{
    public Database()
    {
    }

    public Database(DbContextOptions<Database> options)
        : base(options)
    {
    }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Modifier> Modifiers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductIngredient> ProductIngredients { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // TODO: Move connection string to configuration file for production
        // See: https://go.microsoft.com/fwlink/?linkid=2131148
        optionsBuilder.UseSqlServer("Server=.;Database=Brew_db;Trusted_Connection=True; TrustServerCertificate=True");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ingredie__3214EC07A88E46AF");

            entity.Property(e => e.CurrentStock)
                .HasDefaultValueSql("((0.00))")
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MinStockThreshold)
                .HasDefaultValueSql("((0.00))")
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Unit).HasMaxLength(50);
        });

        modelBuilder.Entity<Modifier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modifier__3214EC0781717093");

            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.PriceAddition)
                .HasDefaultValueSql("((0.00))")
                .HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC075A1F3E40");

            entity.HasIndex(e => e.OrderNumber, "UQ__Orders__CAC5E74353B99A7B").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OrderNumber).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("New");
            entity.Property(e => e.Total)
                .HasDefaultValueSql("((0.00))")
                .HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC07311D12EB");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Products");

            entity.HasMany(d => d.Modifiers).WithMany(p => p.OrderItems)
                .UsingEntity<Dictionary<string, object>>(
                    "OrderItemModifier",
                    r => r.HasOne<Modifier>().WithMany()
                        .HasForeignKey("ModifierId")
                        .HasConstraintName("FK_OrderItemModifiers_Modifiers"),
                    l => l.HasOne<OrderItem>().WithMany()
                        .HasForeignKey("OrderItemId")
                        .HasConstraintName("FK_OrderItemModifiers_OrderItems"),
                    j =>
                    {
                        j.HasKey("OrderItemId", "ModifierId").HasName("PK__OrderIte__F5422053B112C2BB");
                        j.ToTable("OrderItemModifiers");
                    });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC072173325D");

            entity.Property(e => e.Category).HasMaxLength(255);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasMany(d => d.Modifiers).WithMany(p => p.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductModifier",
                    r => r.HasOne<Modifier>().WithMany()
                        .HasForeignKey("ModifierId")
                        .HasConstraintName("FK_ProductModifiers_Modifiers"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .HasConstraintName("FK_ProductModifiers_Products"),
                    j =>
                    {
                        j.HasKey("ProductId", "ModifierId").HasName("PK__ProductM__16A3E01FBFD6818A");
                        j.ToTable("ProductModifiers");
                    });
        });

        modelBuilder.Entity<ProductIngredient>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.IngredientId }).HasName("PK__ProductI__0FE62DE8CAE2E140");

            entity.Property(e => e.AmountPerUnit).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.ProductIngredients)
                .HasForeignKey(d => d.IngredientId)
                .HasConstraintName("FK_ProductIngredients_Ingredients");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductIngredients)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductIngredients_Products");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC074189AB96");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F6D15593CF").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07F2117BD7");

            entity.HasIndex(e => e.Login, "UQ__Users__5E55825B6F44FEF6").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Login).HasMaxLength(255);

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.Role)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
