using Microsoft.AspNetCore.Identity;

namespace AutoPartsManagement.Web.Identity;

/// <summary>
/// Tạo sẵn role và tài khoản khi chạy ứng dụng.
/// - Admin: cố định 6 tài khoản admin, admin1 → admin5. Chỉ các tài khoản này có quyền Admin
///   (toàn quyền: thêm / sửa / xóa phụ tùng, danh mục, hãng, nhà cung cấp).
/// - User: không giới hạn, ai đăng ký cũng tự động là User (xem AppUserManager).
/// </summary>
public static class IdentitySeeder
{
    public const int AdminCount = 5;
    public const string AdminPassword = "Admin@123"; // mật khẩu ban đầu của các tài khoản Admin

    public const string UserUserName = "user";
    public const string UserPassword = "User@123";

    /// <summary>Danh sách tên Admin cố định: admin, admin1, admin2, ..., admin5.</summary>
    public static readonly string[] AdminUserNames =
        new[] { "admin" }
        .Concat(Enumerable.Range(1, AdminCount).Select(i => $"admin{i}"))
        .ToArray();

    // Bản trước dùng email làm tên đăng nhập -> đổi sang tên mới nếu còn
    private const string OldUserEmail = "user@3hauto.com";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // 1. Tạo các role
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. Tạo các tài khoản Admin cố định
        var adminIds = new HashSet<string>();
        foreach (var userName in AdminUserNames)
        {
            var admin = await EnsureUserAsync(userManager, userName, AdminPassword);
            if (admin == null) continue;

            adminIds.Add(admin.Id);
            if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }

        // 3. Tài khoản User mẫu để test
        await EnsureUserAsync(userManager, UserUserName, UserPassword, OldUserEmail);

        // 4. Rà soát: ngoài danh sách Admin cố định không ai có quyền Admin; ai cũng có role User
        foreach (var user in userManager.Users.ToList())
        {
            if (!adminIds.Contains(user.Id) && await userManager.IsInRoleAsync(user, AppRoles.Admin))
                await userManager.RemoveFromRoleAsync(user, AppRoles.Admin);

            if (!await userManager.IsInRoleAsync(user, AppRoles.User))
                await userManager.AddToRoleAsync(user, AppRoles.User);
        }
    }

    private static async Task<IdentityUser?> EnsureUserAsync(
        UserManager<IdentityUser> userManager, string userName, string password, string? oldUserName = null)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user != null) return user;

        if (oldUserName != null)
        {
            user = await userManager.FindByNameAsync(oldUserName);
            if (user != null)
            {
                await userManager.SetUserNameAsync(user, userName);
                return user;
            }
        }

        user = new IdentityUser { UserName = userName };
        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded ? user : null;
    }
}
