using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Identity;

namespace AutoPartsManagement.Desktop.Infrastructure;

/// <summary>Đăng nhập / đăng ký bằng ASP.NET Identity (cùng tài khoản với web).</summary>
public static class AuthService
{
    /// <returns>null nếu thành công, ngược lại là thông báo lỗi.</returns>
    public static Task<string?> LoginAsync(string userName, string password) =>
        AppHost.RunAsync<string?>(async sp =>
        {
            var users = sp.GetRequiredService<UserManager<IdentityUser>>();
            var user = await users.FindByNameAsync(userName);
            if (user == null || !await users.CheckPasswordAsync(user, password))
                return "Tên đăng nhập hoặc mật khẩu không đúng.";
            if (await users.IsLockedOutAsync(user))
                return "Tài khoản đang bị khóa, vui lòng thử lại sau.";

            Session.SignIn(user.Id, user.UserName ?? userName, await users.GetRolesAsync(user));
            return null;
        });

    /// <summary>Tạo tài khoản mới, luôn là quyền User (Admin là tài khoản cố định).</summary>
    public static Task<string?> RegisterAsync(string userName, string password) =>
        AppHost.RunAsync<string?>(async sp =>
        {
            var users = sp.GetRequiredService<UserManager<IdentityUser>>();
            var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync(AppRoles.User))
                await roles.CreateAsync(new IdentityRole(AppRoles.User));

            var user = new IdentityUser { UserName = userName };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
                return string.Join(Environment.NewLine, result.Errors.Select(e => e.Description));

            await users.AddToRoleAsync(user, AppRoles.User);
            Session.SignIn(user.Id, userName, new[] { AppRoles.User });
            return null;
        });
}
