using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Seed;
using AutoPartsManagement.Services.Implementations;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AutoPartsManagement.Desktop.Infrastructure;

/// <summary>
/// Khởi tạo "bộ máy" của app: đọc cấu hình, kết nối database, đăng ký tầng Service.
/// Dùng CHUNG tầng Services + Data với web -> cùng một database, cùng quy tắc nghiệp vụ.
/// </summary>
public static class AppHost
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    public static void Build()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection trong appsettings.json.");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));

        // Tài khoản: dùng chung bảng AspNetUsers với web (đăng ký trên app thì đăng nhập web được và ngược lại)
        services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddErrorDescriber<VietnameseIdentityErrorDescriber>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Tầng Service - giống hệt Program.cs của web
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IDashboardService, DashboardService>();

        Services = services.BuildServiceProvider();
    }

    /// <summary>Tạo bảng (nếu chưa có), dữ liệu mẫu và tài khoản admin - giống lúc web khởi động.</summary>
    public static bool DatabaseReady { get; private set; }

    public static async Task InitializeDatabaseAsync()
    {
        if (DatabaseReady) return;   // đăng xuất rồi đăng nhập lại thì không cần chạy lại
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        DatabaseReady = true;
    }

    /// <summary>
    /// Chạy 1 thao tác với database trong 1 "scope" riêng (mỗi lần bấm nút = 1 DbContext mới,
    /// giống mỗi request trên web) để dữ liệu luôn mới và không bị giữ kết nối.
    /// </summary>
    public static async Task<T> RunAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = Services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    public static async Task RunAsync(Func<IServiceProvider, Task> work)
    {
        using var scope = Services.CreateScope();
        await work(scope.ServiceProvider);
    }
}
