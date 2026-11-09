using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoPartsManagement.Web.Areas.Identity.Pages.Account.Manage;

/// <summary>Trang Hồ sơ tài khoản (thay trang "Profile" mặc định tiếng Anh).</summary>
public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public IndexModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string Username { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [RegularExpression(@"^(\+?84|0)[0-9]{8,10}$", ErrorMessage = "Số điện thoại không hợp lệ (vd: 0912345678).")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound("Không tìm thấy tài khoản.");

        Username = user.UserName ?? string.Empty;
        Input = new InputModel { PhoneNumber = await _userManager.GetPhoneNumberAsync(user) };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound("Không tìm thấy tài khoản.");

        if (!ModelState.IsValid)
        {
            Username = user.UserName ?? string.Empty;
            return Page();
        }

        var phone = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();
        if (phone != await _userManager.GetPhoneNumberAsync(user))
        {
            var result = await _userManager.SetPhoneNumberAsync(user, phone);
            if (!result.Succeeded)
            {
                StatusMessage = "Lỗi: không lưu được số điện thoại, vui lòng thử lại.";
                return RedirectToPage();
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Đã lưu hồ sơ.";
        return RedirectToPage();
    }
}
