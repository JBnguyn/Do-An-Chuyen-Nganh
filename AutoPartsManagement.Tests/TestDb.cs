using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoPartsManagement.Tests;

/// <summary>
/// Tạo database ảo (InMemory) riêng cho từng test, có sẵn dữ liệu mẫu nhỏ.
/// Mỗi test dùng một tên DB khác nhau nên không ảnh hưởng lẫn nhau
/// và không đụng tới SQL Server thật.
/// </summary>
public sealed class TestDb
{
    public const int OilVios = 1;      // Dầu động cơ cho Vios, giá 300.000, tồn 10
    public const int BrakePad = 2;     // Má phanh, giá 500.000, tồn 5
    public const int Inactive = 3;     // Sản phẩm ngừng kinh doanh
    public const int OilGeneric = 4;   // Dầu động cơ loại chung, giá 200.000, tồn 20
    public const int SupplierId = 1;

    private readonly string _name = "test-" + Guid.NewGuid();

    public TestDb()
    {
        using var db = NewContext();
        var cat = new Category { Id = 1, Name = "Dầu nhớt" };
        var brand = new Brand { Id = 1, Name = "Toyota" };
        db.Categories.Add(cat);
        db.Brands.Add(brand);
        db.Suppliers.Add(new Supplier { Id = SupplierId, Name = "NCC Test", Phone = "0900000000" });
        db.Products.AddRange(
            new Product { Id = OilVios, Name = "Dầu động cơ Toyota cho Vios 4L", PartNumber = "OIL-VIOS", CategoryId = 1, BrandId = 1, Price = 300_000m, Stock = 10 },
            new Product { Id = BrakePad, Name = "Má phanh trước", PartNumber = "BRK-01", CategoryId = 1, BrandId = 1, Price = 500_000m, Stock = 5 },
            new Product { Id = Inactive, Name = "Sản phẩm cũ", PartNumber = "OLD-01", CategoryId = 1, BrandId = 1, Price = 100_000m, Stock = 50, IsActive = false },
            new Product { Id = OilGeneric, Name = "Dầu động cơ 5W-30 4L", PartNumber = "OIL-GEN", CategoryId = 1, BrandId = 1, Price = 200_000m, Stock = 20 });
        db.SaveChanges();
    }

    /// <summary>Mở một DbContext mới trỏ vào cùng database ảo (giống như một request mới).</summary>
    public ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(_name)
            // InMemory không hỗ trợ transaction -> bỏ qua cảnh báo thay vì ném lỗi
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    public int StockOf(int productId)
    {
        using var db = NewContext();
        return db.Products.Single(p => p.Id == productId).Stock;
    }
}
