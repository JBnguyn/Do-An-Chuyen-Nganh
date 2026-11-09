namespace AutoPartsManagement.Web.Identity;

/// <summary>
/// Tên các vai trò (role) trong hệ thống.
/// Admin: toàn quyền thêm / sửa / xóa. User: chỉ xem.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly string[] All = { Admin, User };
}
