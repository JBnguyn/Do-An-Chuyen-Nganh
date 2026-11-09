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
