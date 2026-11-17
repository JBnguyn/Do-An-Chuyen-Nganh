using System.ComponentModel.DataAnnotations;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Cấu hình trợ lý chat của cửa hàng. Bảng chỉ có 1 dòng, Admin sửa ở trang Cấu hình bot.
/// </summary>
public class BotSetting
{
    public int Id { get; set; }

    /// <summary>Tắt thì khung chat chỉ trả lời bằng danh sách sản phẩm tìm được, không gọi AI.</summary>
    public bool AiEnabled { get; set; } = true;

    [Required(ErrorMessage = "Vui lòng nhập tên bot")]
    [StringLength(50)]
    [Display(Name = "Tên bot")]
    public string BotName { get; set; } = "Trợ lý 3H AUTO";

    [Required(ErrorMessage = "Vui lòng nhập cách bot xưng hô")]
    [StringLength(20)]
    [Display(Name = "Bot xưng là")]
    public string BotPronoun { get; set; } = "mình";

    [Required(ErrorMessage = "Vui lòng nhập cách gọi khách")]
    [StringLength(20)]
    [Display(Name = "Gọi khách là")]
    public string CustomerPronoun { get; set; } = "bạn";

    [StringLength(200)]
    [Display(Name = "Giọng điệu")]
    public string Tone { get; set; } = "thân thiện, ngắn gọn";

    [Display(Name = "Mức dùng emoji")]
    public EmojiLevel EmojiLevel { get; set; } = EmojiLevel.Few;

    [StringLength(200)]
    [Display(Name = "Giờ làm việc")]
    public string WorkingHours { get; set; } = "8:00 - 17:30, Thứ 2 - Thứ 7";

    [StringLength(2000)]
    [Display(Name = "Chính sách đổi trả, bảo hành, giao hàng")]
    public string? Policies { get; set; } = "Giao hàng toàn quốc; bảo hành theo chính sách hãng.";

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public enum EmojiLevel
{
    [Display(Name = "Không dùng")] None = 0,
    [Display(Name = "Ít")] Few = 1,
    [Display(Name = "Nhiều")] Many = 2
}