using AutoPartsManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>API cho khung chat trợ lý ở góc màn hình cửa hàng.</summary>
public class ChatController : Controller
{
    private readonly IChatAssistant _assistant;

    public ChatController(IChatAssistant assistant)
    {
        _assistant = assistant;
    }

    public class AskRequest
    {
        public List<ChatMessage> Messages { get; set; } = new();
    }

    // POST: /Chat/Ask   body: { "messages": [ { "role": "user", "content": "..." } ] }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        var result = await _assistant.AskAsync(request.Messages ?? new(), ct);
        return ToJson(result);
    }

    private const long MaxImageBytes = 5 * 1024 * 1024;

    // POST: /Chat/AskImage   (multipart/form-data: image, message)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8_000_000)]
    public async Task<IActionResult> AskImage(IFormFile? image, string? message, CancellationToken ct)
    {
        if (image == null || image.Length == 0)
            return ToJson(new ChatReply("Bạn chọn ảnh giúp mình nha.", new(), false));
        if (image.Length > MaxImageBytes)
            return ToJson(new ChatReply("Ảnh lớn quá (tối đa 5MB), bạn chụp lại hoặc chọn ảnh nhỏ hơn nha.", new(), false));

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var mediaType = DetectImageType(bytes);
        if (mediaType == null)
            return ToJson(new ChatReply("Mình chỉ đọc được ảnh JPG, PNG, WEBP hoặc GIF thôi bạn ơi.", new(), false));

        var note = message?.Length > 300 ? message[..300] : message;
        var result = await _assistant.IdentifyImageAsync(bytes, mediaType, note, ct);
        return ToJson(result);
    }

    /// <summary>Xác định loại ảnh theo vài byte đầu file (không tin phần mở rộng / Content-Type).</summary>
    private static string? DetectImageType(byte[] b)
    {
        if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
        if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
        if (b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return "image/webp";
        if (b.Length > 6 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F') return "image/gif";
        return null;
    }

    private IActionResult ToJson(ChatReply result)
    {
        return Json(new
        {
            reply = result.Reply,
            usedAi = result.UsedAi,
            products = result.Products.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                partNumber = p.PartNumber,
                price = p.Price,
                stock = p.Stock,
                imageUrl = p.ImageUrl,
                url = Url.Action("Detail", "Shop", new { id = p.Id })
            })
        });
    }
}
