using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AutoPartsManagement.Web.Identity;

/// <summary>
/// UserManager mở rộng: mọi tài khoản mới tạo (kể cả qua trang Đăng ký) tự động được gán role "User".
/// </summary>
public class AppUserManager : UserManager<IdentityUser>
{
    public AppUserManager(
        IUserStore<IdentityUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<IdentityUser> passwordHasher,
        IEnumerable<IUserValidator<IdentityUser>> userValidators,
        IEnumerable<IPasswordValidator<IdentityUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<IdentityUser>> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }

    public override async Task<IdentityResult> CreateAsync(IdentityUser user)
    {
        var result = await base.CreateAsync(user);
        if (result.Succeeded)
            await AddToRoleAsync(user, AppRoles.User);
        return result;
    }
}
