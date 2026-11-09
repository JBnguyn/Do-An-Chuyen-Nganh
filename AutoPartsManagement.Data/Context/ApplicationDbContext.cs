using AutoPartsManagement.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Data.Context;

/// <summary>
/// DbContext chính của ứng dụng quản lý phụ tùng ô tô.
/// Kế thừa IdentityDbContext để tích hợp xác thực người dùng (ASP.NET Identity).
/// </summary>
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // === DbSet cho các entity nghiệp vụ ===
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Import> Imports { get; set; }
    public DbSet<ImportDetail> ImportDetails { get; set; }
    public DbSet<UserVehicle> UserVehicles { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === Cấu hình bảng Product ===
        modelBuilder.Entity<Product>(entity =>
        {
            // Quan hệ: Product -> Category (nhiều-một)
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ: Product -> Brand (nhiều-một)
            entity.HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình cột giá tiền (decimal)
            entity.Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // === Cấu hình bảng Order ===
        modelBuilder.Entity<Order>(entity =>
        {
            // Quan hệ: Order -> Customer (nhiều-một)
            entity.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(o => o.OrderDate)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(o => o.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // === Cấu hình bảng OrderDetail ===
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            // Quan hệ: OrderDetail -> Order (nhiều-một)
            entity.HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ: OrderDetail -> Product (nhiều-một)
            entity.HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(od => od.UnitPrice)
                .HasColumnType("decimal(18,2)");
        });

        // === Cấu hình bảng Import ===
        modelBuilder.Entity<Import>(entity =>
        {
            // Quan hệ: Import -> Supplier (nhiều-một)
            entity.HasOne(im => im.Supplier)
                .WithMany(s => s.Imports)
                .HasForeignKey(im => im.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(im => im.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(im => im.ImportDate)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(im => im.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // === Cấu hình bảng ImportDetail ===
        modelBuilder.Entity<ImportDetail>(entity =>
        {
            // Quan hệ: ImportDetail -> Import (nhiều-một)
            entity.HasOne(id => id.Import)
                .WithMany(im => im.ImportDetails)
                .HasForeignKey(id => id.ImportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ: ImportDetail -> Product (nhiều-một)
            entity.HasOne(id => id.Product)
                .WithMany(p => p.ImportDetails)
                .HasForeignKey(id => id.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(id => id.UnitPrice)
                .HasColumnType("decimal(18,2)");
        });

        // === Cấu hình bảng Supplier ===
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(s => s.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // === Cấu hình bảng Customer ===
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Tìm khách theo tài khoản đăng nhập
            entity.HasIndex(c => c.UserId);
        });

        // === Cấu hình bảng UserVehicle (xe của khách) ===
        modelBuilder.Entity<UserVehicle>(entity =>
        {
            entity.HasIndex(v => v.UserId);
        });

        // === Cấu hình bảng MaintenanceRecord (lịch sử bảo dưỡng) ===
        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            // Xóa xe thì xóa luôn lịch sử bảo dưỡng của xe đó
            entity.HasOne(r => r.UserVehicle)
                .WithMany(v => v.Records)
                .HasForeignKey(r => r.UserVehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === Đặt tên bảng theo snake_case / quy ước ===
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Brand>().ToTable("Brands");
        modelBuilder.Entity<Product>().ToTable("Products");
        modelBuilder.Entity<Supplier>().ToTable("Suppliers");
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");
        modelBuilder.Entity<Import>().ToTable("Imports");
        modelBuilder.Entity<ImportDetail>().ToTable("ImportDetails");
        modelBuilder.Entity<UserVehicle>().ToTable("UserVehicles");
        modelBuilder.Entity<MaintenanceRecord>().ToTable("MaintenanceRecords");
    }
}
