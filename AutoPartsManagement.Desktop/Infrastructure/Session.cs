using AutoPartsManagement.Web.Identity;

namespace AutoPartsManagement.Desktop.Infrastructure;

/// <summary>Người dùng đang đăng nhập trên app.</summary>
public static class Session
{
    public static string UserId { get; private set; } = string.Empty;
    public static string UserName { get; private set; } = string.Empty;
    public static IReadOnlyList<string> Roles { get; private set; } = Array.Empty<string>();

    public static bool IsAdmin => Roles.Contains(AppRoles.Admin);
    public static bool IsSignedIn => UserId.Length > 0;

    public static void SignIn(string userId, string userName, IEnumerable<string> roles)
    {
        UserId = userId;
        UserName = userName;
        Roles = roles.ToList();
    }

    public static void SignOut()
    {
        UserId = string.Empty;
        UserName = string.Empty;
        Roles = Array.Empty<string>();
        Cart.Clear();
    }
}
