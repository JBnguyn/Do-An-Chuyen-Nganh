using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Seed;
using AutoPartsManagement.Services.Implementations;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Identity;
using AutoPartsManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// RequireConfirmedAccount = false: đăng ký xong đăng nhập được ngay (đồ án không có gửi email xác nhận)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()                    // bật phân quyền theo role (Admin / User)
    .AddUserManager<AppUserManager>()            // tài khoản mới tự được gán role "User"
    .AddErrorDescriber<VietnameseIdentityErrorDescriber>() // thông báo lỗi tiếng Việt
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// Bắt buộc đăng nhập cho MỌI trang. Trang nào cho phép vào tự do thì gắn [AllowAnonymous]
// (Đăng nhập, Đăng ký, trang lỗi, ...). Chưa đăng nhập sẽ tự chuyển sang trang Đăng nhập.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// === Tầng Service (nghiệp vụ) ===
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// === Trợ lý hỏi đáp (khung chat ở góc màn hình) ===
// Muốn dùng AI: đặt khóa Anthropic:ApiKey bằng User Secrets (không ghi khóa vào appsettings.json).
builder.Services.AddHttpClient<IChatAssistant, ChatAssistant>(c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<IImageStorage, ImageStorage>();

var app = builder.Build();

// === Tự áp dụng Migrations và tạo dữ liệu mẫu (chỉ khi database còn trống) ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
        await IdentitySeeder.SeedAsync(scope.ServiceProvider); // tạo role Admin/User + tài khoản mẫu
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Lỗi khi khởi tạo database / dữ liệu mẫu.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // phục vụ ảnh upload trong wwwroot/uploads (tạo lúc chạy, không có trong static assets manifest)
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous(); // css, js, ảnh luôn tải được (kể cả trang đăng nhập)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Shop}/{action=Index}/{id?}") // trang mặc định: cửa hàng
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
