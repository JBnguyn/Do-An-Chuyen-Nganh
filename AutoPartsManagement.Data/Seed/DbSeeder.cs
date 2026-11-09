using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Data.Seed;

/// <summary>
/// Tạo dữ liệu mẫu để demo / kiểm thử.
/// - Dữ liệu ban đầu: chỉ tạo khi database chưa có danh mục nào.
/// - Sản phẩm bổ sung: thêm những sản phẩm chưa có (kiểm tra theo mã phụ tùng), chạy mỗi lần khởi động.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (!await db.Categories.AnyAsync())
            await SeedInitialAsync(db);

        await SeedExtraProductsAsync(db);
    }

    private static async Task SeedInitialAsync(ApplicationDbContext db)
    {

        // --- Danh mục ---
        var phanh = new Category { Name = "Hệ thống phanh", Description = "Má phanh, đĩa phanh, heo dầu, dầu phanh" };
        var treo = new Category { Name = "Hệ thống treo - giảm xóc", Description = "Phuộc nhún, rotuyn, cao su chân máy" };
        var dongCo = new Category { Name = "Động cơ", Description = "Bugi, dây curoa, bơm nước, gioăng" };
        var lyHop = new Category { Name = "Ly hợp - truyền động", Description = "Bộ côn, bi tê, láp" };
        var loc = new Category { Name = "Lọc - dầu nhớt", Description = "Lọc gió, lọc dầu, lọc nhiên liệu, dầu máy" };
        var dien = new Category { Name = "Hệ thống điện", Description = "Ắc quy, đèn, máy phát, củ đề" };
        db.Categories.AddRange(phanh, treo, dongCo, lyHop, loc, dien);

        // --- Hãng sản xuất ---
        var bosch = new Brand { Name = "Bosch", Country = "Đức" };
        var denso = new Brand { Name = "Denso", Country = "Nhật Bản" };
        var kyb = new Brand { Name = "KYB", Country = "Nhật Bản" };
        var aisin = new Brand { Name = "Aisin", Country = "Nhật Bản" };
        var ngk = new Brand { Name = "NGK", Country = "Nhật Bản" };
        var brembo = new Brand { Name = "Brembo", Country = "Ý" };
        var toyota = new Brand { Name = "Toyota Genuine Parts", Country = "Nhật Bản" };
        db.Brands.AddRange(bosch, denso, kyb, aisin, ngk, brembo, toyota);

        // --- Nhà cung cấp (dữ liệu giả lập) ---
        db.Suppliers.AddRange(
            new Supplier { Name = "Công ty TNHH Phụ tùng Minh Phát", Phone = "0281234567", Email = "lienhe@minhphat.example", Address = "Quận 10, TP. HCM" },
            new Supplier { Name = "Công ty CP Ô tô Nam Á", Phone = "0287654321", Email = "sales@namaauto.example", Address = "TP. Thủ Đức, TP. HCM" },
            new Supplier { Name = "Đại lý phụ tùng Hưng Thịnh", Phone = "0909000111", Address = "Quận Bình Tân, TP. HCM" });

        // --- Phụ tùng ---
        db.Products.AddRange(
            new Product { Name = "Phuộc nhún trước Toyota Vios 2014-2021 KYB Gas Shock", PartNumber = "333338-KYB", Category = treo, Brand = kyb, Price = 1350000, Stock = 12, WarrantyMonths = 12 },
            new Product { Name = "Bộ ly hợp mâm ép lá côn Toyota Innova 2.0", PartNumber = "DTX-136", Category = lyHop, Brand = aisin, Price = 3200000, Stock = 4, WarrantyMonths = 12 },
            new Product { Name = "Má phanh trước Toyota Vios / Yaris", PartNumber = "04465-0D020", Category = phanh, Brand = toyota, Price = 850000, Stock = 25, WarrantyMonths = 6 },
            new Product { Name = "Đĩa phanh trước Brembo Mazda 3", PartNumber = "09.A968.11", Category = phanh, Brand = brembo, Price = 1650000, Stock = 0, WarrantyMonths = 12 },
            new Product { Name = "Bugi Iridium NGK ILKAR7B11", PartNumber = "ILKAR7B11", Category = dongCo, Brand = ngk, Price = 320000, Stock = 60, WarrantyMonths = 6 },
            new Product { Name = "Bugi Iridium Denso IK20TT", PartNumber = "IK20TT", Category = dongCo, Brand = denso, Price = 280000, Stock = 3, WarrantyMonths = 6 },
            new Product { Name = "Lọc gió động cơ Bosch Honda City", PartNumber = "F026400358", Category = loc, Brand = bosch, Price = 210000, Stock = 40, WarrantyMonths = 3 },
            new Product { Name = "Lọc dầu Toyota chính hãng", PartNumber = "90915-YZZE1", Category = loc, Brand = toyota, Price = 120000, Stock = 80, WarrantyMonths = 3 },
            new Product { Name = "Bơm nước động cơ Aisin Toyota Fortuner", PartNumber = "WPT-190", Category = dongCo, Brand = aisin, Price = 1450000, Stock = 2, WarrantyMonths = 12 },
            new Product { Name = "Ắc quy Bosch 12V 60Ah", PartNumber = "S4-005", Category = dien, Brand = bosch, Price = 1900000, Stock = 8, WarrantyMonths = 18 },
            new Product { Name = "Máy phát điện Denso Toyota Corolla Altis", PartNumber = "104210-3470", Category = dien, Brand = denso, Price = 5600000, Stock = 0, WarrantyMonths = 12 },
            new Product { Name = "Phuộc nhún sau KYB Ford Ranger", PartNumber = "344455-KYB", Category = treo, Brand = kyb, Price = 1550000, Stock = 6, WarrantyMonths = 12 });

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Bổ sung sản phẩm mẫu. Tên, mã và giá tham khảo giá niêm yết của các cửa hàng phụ tùng
    /// tại Việt Nam (11/2026). Tồn kho là số liệu giả lập để demo.
    /// Sản phẩm nào đã có (trùng mã phụ tùng) thì bỏ qua.
    /// </summary>
    private static async Task SeedExtraProductsAsync(ApplicationDbContext db)
    {
        // --- Danh mục / hãng: lấy nếu đã có, chưa có thì tạo ---
        var phanh = await CategoryAsync(db, "Hệ thống phanh", "Má phanh, đĩa phanh, heo dầu, dầu phanh");
        var treo = await CategoryAsync(db, "Hệ thống treo - giảm xóc", "Phuộc nhún, rotuyn, cao su chân máy");
        var dongCo = await CategoryAsync(db, "Động cơ", "Bugi, dây curoa, bơm nước, gioăng");
        var lyHop = await CategoryAsync(db, "Ly hợp - truyền động", "Bộ côn, bi tê, láp");
        var loc = await CategoryAsync(db, "Lọc - dầu nhớt", "Lọc gió, lọc dầu, lọc nhiên liệu, dầu máy");
        var dien = await CategoryAsync(db, "Hệ thống điện", "Ắc quy, đèn, máy phát, củ đề");
        var lamMat = await CategoryAsync(db, "Nước làm mát - phụ gia", "Nước làm mát động cơ, phụ gia vệ sinh động cơ");

        var aisin = await BrandAsync(db, "Aisin", "Nhật Bản");
        var kyb = await BrandAsync(db, "KYB", "Nhật Bản");
        var bosch = await BrandAsync(db, "Bosch", "Đức");
        var mk = await BrandAsync(db, "MK Kashiyama", "Nhật Bản");
        var gsp = await BrandAsync(db, "GSP", null);
        var nisto = await BrandAsync(db, "NISTO", null);

        var products = new List<Product>
        {
            // Ly hợp - truyền động
            P("Mâm ép ly hợp Toyota Vios đời mới", "CTX-175A", lyHop, aisin, 1240000, 10, "Mâm ép (bàn ép) ly hợp Aisin dùng cho Toyota Vios đời mới."),
            P("Mâm ép ly hợp Toyota Fortuner", "CTX-160A", lyHop, aisin, 1890000, 6, "Mâm ép (bàn ép) ly hợp Aisin dùng cho Toyota Fortuner."),
            P("Mâm ép ly hợp Toyota Hiace đời 2007 trở lên", "CTX-106A", lyHop, aisin, 2380000, 0, "Mâm ép (bàn ép) ly hợp Aisin dùng cho Toyota Hiace từ đời 2007."),

            // Động cơ
            P("Bơm nước động cơ Toyota Vios Aisin", "WPT-111V", dongCo, aisin, 550000, 15, "Bơm nước làm mát động cơ Aisin dùng cho Toyota Vios."),
            P("Cao su chân máy Toyota Camry ACV40 2007", "EMTT-0015", dongCo, aisin, 1050000, 5, "Cao su chân máy (giảm chấn động cơ) Aisin cho Toyota Camry ACV40."),
            P("Gioăng nắp dàn cò Honda Civic / CR-V", "VGH-5001", dongCo, aisin, 230000, 20, "Gioăng nắp dàn cò (nắp quy lát) Aisin dùng cho Honda Civic, CR-V."),

            // Hệ thống treo - gầm
            P("Rô tuyn cân bằng trước Aisin", "JRST-4003", treo, aisin, 320000, 18, "Rô tuyn thanh cân bằng trước Aisin."),
            P("Bạc đạn bánh trước Toyota Fortuner (có ABS)", "HUBT-0004A", treo, aisin, 1210000, 4, "Bạc đạn (moay ơ) bánh trước Aisin cho Toyota Fortuner có ABS."),
            P("Cặp phuộc dầu KYB Toyota Camry 2008-2011", "665021JAPAN", treo, kyb, 3100000, 3, "Cặp phuộc nhún dầu KYB sản xuất tại Nhật Bản, dùng cho Toyota Camry 2008-2011. Giá cho 1 cặp."),
            P("Bộ chụp bụi + giảm chấn phuộc trước Toyota Camry", "5408450PK", treo, gsp, 420000, 12, "Bộ gồm 2 chụp bụi và 2 cao su giảm chấn phuộc trước, dùng cho Toyota Camry."),
            P("Cao su cân bằng trước Mazda 3 / Mazda 6 / CX-5", "BJS7-34-156A", treo, nisto, 80000, 30, "Cao su thanh cân bằng trước cho Mazda 3 (2014-2018), Mazda 6 (2014 trở lên), Mazda CX-5."),
            P("Cao su càng A trên Toyota Hiace", "48632-26010", treo, nisto, 150000, 14, "Cao su càng A trên (loại nhỏ) dùng cho Toyota Hiace."),

            // Hệ thống phanh
            P("Má phanh trước Toyota Fortuner MK Kashiyama", "D-2228", phanh, mk, 620000, 16, "Bố thắng (má phanh) trước MK Kashiyama cho Toyota Fortuner. Giá cho 1 bộ."),
            P("Má phanh trước Toyota Wigo MK Kashiyama", "D-0034", phanh, mk, 570000, 10, "Bố thắng (má phanh) trước MK Kashiyama cho Toyota Wigo. Giá cho 1 bộ."),
            P("Má phanh trước Mitsubishi Xpander / Suzuki Vitara MK Kashiyama", "D-6168", phanh, mk, 600000, 8, "Bố thắng trước MK Kashiyama cho Mitsubishi Xpander, Suzuki Vitara (đời 2015 trở lên). Giá cho 1 bộ."),
            P("Má phanh sau Mitsubishi Outlander (phanh tay điện) MK Kashiyama", "D-6161", phanh, mk, 730000, 5, "Bố thắng sau MK Kashiyama cho Mitsubishi Outlander loại phanh tay điện. Giá cho 1 bộ."),

            // Lọc - dầu nhớt
            P("Dầu hộp số tự động Aisin AFW-VI (can 4L)", "ATFDVI4S", loc, aisin, 1300000, 0, "Dầu hộp số tự động Aisin AFW-VI, can 4 lít."),
            P("Dầu hộp số thường Aisin GL-5 75W-90 (can 4L)", "GSL57594P", loc, aisin, 1485000, 0, "Dầu hộp số thường / cầu Aisin GL-5 75W-90, can 4 lít."),

            // Nước làm mát - phụ gia
            P("Nước làm mát Aisin Super Long Life xanh dương 1L", "SCPM50A1LB", lamMat, aisin, 275000, 40, "Nước làm mát pha sẵn Aisin Super Long Life màu xanh dương, can 1 lít."),
            P("Nước làm mát Aisin Super Long Life màu hồng 1L", "SCPM50A1LPK", lamMat, aisin, 275000, 40, "Nước làm mát pha sẵn Aisin Super Long Life màu hồng, can 1 lít."),
            P("Nước làm mát Aisin màu xanh (can 4L)", "LCPM20A4LG", lamMat, aisin, 440000, 25, "Nước làm mát Aisin màu xanh lá, can 4 lít."),
            P("Phụ gia vệ sinh động cơ Aisin", "ADEAZ-4004", lamMat, aisin, 330000, 22, "Phụ gia vệ sinh động cơ xăng cao cấp Aisin."),

            // Hệ thống điện
            P("Ắc quy Bosch 55D23L 12V 60Ah", "55D23L", dien, bosch, 1690000, 7, "Ắc quy khô (miễn bảo dưỡng) Bosch 12V 60Ah, cọc trái.")
        };

        var existing = await db.Products.Select(p => p.PartNumber).ToListAsync();
        var toAdd = products.Where(p => !existing.Contains(p.PartNumber, StringComparer.OrdinalIgnoreCase)).ToList();
        if (toAdd.Count == 0)
            return;

        db.Products.AddRange(toAdd);
        await db.SaveChangesAsync();
    }

    private static Product P(string name, string partNumber, Category category, Brand brand, decimal price, int stock, string description) =>
        new()
        {
            Name = name,
            PartNumber = partNumber,
            Category = category,
            Brand = brand,
            Price = price,
            Stock = stock,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

    private static async Task<Category> CategoryAsync(ApplicationDbContext db, string name, string description)
    {
        var c = await db.Categories.FirstOrDefaultAsync(x => x.Name == name);
        if (c != null) return c;
        c = new Category { Name = name, Description = description };
        db.Categories.Add(c);
        return c;
    }

    private static async Task<Brand> BrandAsync(ApplicationDbContext db, string name, string? country)
    {
        var b = await db.Brands.FirstOrDefaultAsync(x => x.Name == name);
        if (b != null) return b;
        b = new Brand { Name = name, Country = country };
        db.Brands.Add(b);
        return b;
    }
}
