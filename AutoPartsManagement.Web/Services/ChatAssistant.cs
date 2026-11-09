using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Web.Services;

/// <summary>Một tin nhắn trong cuộc trò chuyện (role = "user" hoặc "assistant").</summary>
public record ChatMessage(string Role, string Content);

/// <summary>Sản phẩm gợi ý hiển thị dạng thẻ trong khung chat.</summary>
public record ChatProduct(int Id, string Name, string PartNumber, decimal Price, int Stock, string? ImageUrl);

public record ChatReply(string Reply, List<ChatProduct> Products, bool UsedAi);

public interface IChatAssistant
{
    Task<ChatReply> AskAsync(IReadOnlyList<ChatMessage> history, CancellationToken ct = default);
}

/// <summary>
/// Trợ lý hỏi đáp của cửa hàng.
/// - Tìm sản phẩm liên quan trong database theo câu hỏi (không phân biệt dấu).
/// - Nếu đã cấu hình Anthropic:ApiKey: gửi câu hỏi + dữ liệu sản phẩm cho Claude để trả lời tự nhiên.
/// - Nếu chưa có key hoặc gọi API lỗi: trả lời đơn giản bằng danh sách sản phẩm tìm được.
/// </summary>
public class ChatAssistant : IChatAssistant
{
    private const int MaxContextProducts = 8;
    private const int MaxHistory = 10;
    private const int MaxMessageLength = 1000;

    private readonly ApplicationDbContext _db;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ChatAssistant> _logger;

    public ChatAssistant(ApplicationDbContext db, HttpClient http, IConfiguration config, ILogger<ChatAssistant> logger)
    {
        _db = db;
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<ChatReply> AskAsync(IReadOnlyList<ChatMessage> history, CancellationToken ct = default)
    {
        // Làm sạch lịch sử: chỉ giữ user/assistant, cắt ngắn, tối đa MaxHistory tin, bắt đầu bằng user
        var messages = history
            .Where(m => (m.Role == "user" || m.Role == "assistant") && !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => new ChatMessage(m.Role, m.Content.Length > MaxMessageLength ? m.Content[..MaxMessageLength] : m.Content.Trim()))
            .TakeLast(MaxHistory)
            .SkipWhile(m => m.Role != "user")
            .ToList();

        var question = messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(question))
            return new ChatReply("Bạn cần tìm phụ tùng gì nè? Cho mình biết tên phụ tùng hoặc dòng xe nha.", new(), false);

        // Tìm theo câu hỏi mới nhất; nếu không ra thì ghép thêm các câu trước (vd: "bugi denso" -> "giá bao nhiêu")
        var products = await FindProductsAsync(question, ct);
        var fromContext = false;
        if (products.Count == 0)
        {
            var recent = string.Join(" ", messages.Where(m => m.Role == "user").TakeLast(3).Select(m => m.Content));
            products = await FindProductsAsync(recent, ct);
            fromContext = products.Count > 0;
        }

        var apiKey = _config["Anthropic:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var reply = await CallClaudeAsync(apiKey, messages, products, ct);
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    // Chỉ hiện thẻ sản phẩm mà AI có nhắc tới (theo mã hoặc tên) -> chào hỏi thì không hiện thẻ
                    var mentioned = products
                        .Where(p => reply.Contains(p.PartNumber, StringComparison.OrdinalIgnoreCase)
                                 || reply.Contains(p.Name, StringComparison.OrdinalIgnoreCase))
                        .Take(4)
                        .ToList();
                    return new ChatReply(reply, mentioned, true);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Gọi Claude API thất bại, chuyển sang trả lời đơn giản.");
            }
        }

        return FallbackReply(question, products, fromContext, messages.Count(m => m.Role == "user"));
    }

    // ===== Tìm sản phẩm liên quan =====

    private async Task<List<ChatProduct>> FindProductsAsync(string question, CancellationToken ct)
    {
        var all = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id, p.Name, p.PartNumber, p.Price, p.Stock, p.ImageUrl,
                Category = p.Category!.Name,
                Brand = p.Brand!.Name
            })
            .Take(1000)
            .ToListAsync(ct);

        var words = Normalize(question)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 2 && !StopWords.Contains(w))
            .Distinct()
            .ToList();
        if (words.Count == 0) return new();

        var scored = all
            .Select(p =>
            {
                var text = Normalize($"{p.Name} {p.PartNumber} {p.Category} {p.Brand}");
                var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                var score = 0;
                foreach (var w in words)
                {
                    if (tokens.Contains(w)) score += 2;                     // khớp nguyên từ
                    else if (w.Length >= 4 && text.Contains(w)) score += 1; // khớp một phần
                }
                // Khớp mã phụ tùng (vd "ik20tt", "d2228") được ưu tiên cao
                var code = Normalize(p.PartNumber).Replace(" ", "");
                if (words.Any(w => w.Length >= 4 && code.Contains(w))) score += 6;
                return (p, score);
            })
            .Where(x => x.score > 0)
            .ToList();
        if (scored.Count == 0) return new();

        // Bỏ các kết quả kém liên quan hơn hẳn kết quả tốt nhất
        var best = scored.Max(x => x.score);
        return scored
            .Where(x => x.score * 10 >= best * 6)
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.p.Stock > 0)
            .Take(MaxContextProducts)
            .Select(x => new ChatProduct(x.p.Id, x.p.Name, x.p.PartNumber, x.p.Price, x.p.Stock, x.p.ImageUrl))
            .ToList();
    }

    /// <summary>Chữ thường, bỏ dấu tiếng Việt, bỏ ký tự đặc biệt - để "ma phanh" khớp "Má phanh".</summary>
    private static string Normalize(string s)
    {
        var d = s.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return sb.ToString();
    }

    private static readonly HashSet<string> StopWords = new()
    {
        "cho", "toi", "minh", "ban", "can", "mua", "tim", "co", "khong", "ko", "gia", "bao", "nhieu",
        "xe", "cua", "la", "va", "voi", "nao", "loai", "hang", "con", "nhe", "a", "oi", "shop", "di",
        "the", "gi", "duoc", "dung", "o", "to", "em", "anh", "chi", "hoi", "muon", "xin", "nay", "do",
        "alo", "chao", "hello", "hi", "hey", "ad", "admin", "cam", "on", "ok", "oke", "vang", "da", "uk", "u",
        "nha", "nhe", "vay", "ha", "a", "ah", "thi", "sao", "the", "nua", "them", "ve", "hien", "tai", "dang",
        "lam", "biet", "giup", "tu", "van", "ban", "ai", "tien", "re", "dat", "mac", "tot", "nhat", "cai", "bo"
    };

    // ===== Gọi Claude API =====

    private async Task<string?> CallClaudeAsync(string apiKey, List<ChatMessage> messages, List<ChatProduct> products, CancellationToken ct)
    {
        var model = _config["Anthropic:Model"] ?? "claude-haiku-5-5";
        var version = _config["Anthropic:Version"] ?? "2026-10-21";

        var body = new
        {
            model,
            max_tokens = 600,
            system = BuildSystemPrompt(products),
            messages = messages.Select(m => new { role = m.Role, content = m.Content })
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", version);

        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("Claude API trả về {Status}: {Body}", (int)res.StatusCode, await res.Content.ReadAsStringAsync(ct));
            return null;
        }

        var data = await res.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: ct);
        return data?.Content?
            .Where(c => c.Type == "text")
            .Select(c => c.Text)
            .FirstOrDefault()?
            .Trim();
    }

    private string BuildSystemPrompt(List<ChatProduct> products)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là nhân viên tư vấn của cửa hàng phụ tùng ô tô 3H AUTO, đang nhắn tin trực tiếp với khách.");
        sb.AppendLine("Cách nói chuyện:");
        sb.AppendLine("- Nói như người thật nhắn tin: tự nhiên, thân thiện, xưng \"mình\" và gọi khách là \"bạn\"; dùng từ đệm nhẹ như \"nha\", \"nè\", \"ạ\" vừa phải.");
        sb.AppendLine("- Câu ngắn, thường 1-3 câu; không gạch đầu dòng, không markdown, không lặp lại câu hỏi của khách.");
        sb.AppendLine("- Khách chào hỏi, cảm ơn, nói chuyện phiếm thì đáp lại tự nhiên rồi hỏi khách đang cần phụ tùng gì; không liệt kê sản phẩm khi khách chưa hỏi.");
        sb.AppendLine("- Khi tư vấn, hỏi lại một câu cần thiết (dòng xe, đời xe) thay vì hỏi nhiều thứ cùng lúc.");
        sb.AppendLine("- Nhắc tên sản phẩm kèm mã phụ tùng khi giới thiệu để khách dễ tìm.");
        sb.AppendLine("Quy tắc:");
        sb.AppendLine("- Chỉ nêu giá, mã và tồn kho có trong DANH SÁCH SẢN PHẨM bên dưới. Không tự bịa sản phẩm, giá hay thông số.");
        sb.AppendLine("- Nếu không có sản phẩm phù hợp, nói rõ là cửa hàng chưa có trên web và mời khách gọi hotline " + Helpers.ShopUi.Hotline + ".");
        sb.AppendLine("- Nếu khách chưa nói dòng xe / đời xe mà điều đó quan trọng, hãy hỏi lại để tư vấn đúng phụ tùng.");
        sb.AppendLine("- Không khẳng định phụ tùng chắc chắn lắp vừa xe nếu tên sản phẩm không ghi rõ dòng xe đó.");
        sb.AppendLine("- Cách mua: bấm 'Thêm vào giỏ', mở Giỏ hàng, bấm 'Đặt hàng', điền thông tin nhận hàng; thanh toán khi nhận hàng (COD).");
        sb.AppendLine("- Khách xem và hủy đơn (khi còn Chờ xác nhận) tại mục 'Đơn hàng của tôi'.");
        sb.AppendLine("- Chỉ trả lời các câu hỏi liên quan đến phụ tùng ô tô và mua hàng tại cửa hàng; câu hỏi ngoài lề thì lịch sự từ chối.");
        sb.AppendLine();
        sb.AppendLine("Thông tin cửa hàng: giờ làm việc 8:00 - 17:30, Thứ 2 - Thứ 7; giao hàng toàn quốc; bảo hành theo chính sách hãng; hotline " + Helpers.ShopUi.Hotline + ".");
        sb.AppendLine();
        sb.AppendLine("DANH SÁCH SẢN PHẨM LIÊN QUAN (dữ liệu thật từ hệ thống):");
        if (products.Count == 0)
        {
            sb.AppendLine("(không tìm thấy sản phẩm nào khớp câu hỏi)");
        }
        foreach (var p in products)
        {
            var stock = p.Stock <= 0 ? "hết hàng" : p.Stock <= InventoryRules.LowStockThreshold ? $"còn {p.Stock} (sắp hết)" : "còn hàng";
            sb.AppendLine($"- {p.Name} | mã {p.PartNumber} | giá {p.Price.ToString("N0", vi)} đ | {stock}");
        }
        return sb.ToString();
    }

    private sealed class ClaudeResponse
    {
        [JsonPropertyName("content")] public List<ClaudeContent>? Content { get; set; }
    }

    private sealed class ClaudeContent
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    // ===== Trả lời khi không dùng AI =====

    private static ChatReply FallbackReply(string question, List<ChatProduct> products, bool fromContext, int turn)
    {
        var q = " " + Normalize(question).Trim() + " ";
        bool Has(params string[] phrases) => phrases.Any(p => q.Contains(" " + p + " "));
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var hotline = Helpers.ShopUi.Hotline;

        var greeting = Has("alo", "chao", "xin chao", "hello", "hi", "hey", "shop oi", "ad oi", "ban oi", "em oi");
        var thanks = Has("cam on", "thanks", "thank", "thank you", "tks", "camon");
        var bye = Has("tam biet", "bye", "bai", "hen gap lai");

        // --- Chào hỏi / cảm ơn / tạm biệt (khi không kèm câu hỏi sản phẩm) ---
        if (products.Count == 0 || fromContext)
        {
            if (thanks) return Text(Pick("Không có gì nè! Bạn cần thêm gì cứ nhắn mình nha.", "Dạ không có chi ạ, chúc bạn chạy xe êm nha!", "Rất vui được giúp bạn! Có gì cứ hỏi mình nhé."));
            if (bye) return Text(Pick("Chào bạn nha, hẹn gặp lại!", "Tạm biệt bạn, chúc bạn một ngày vui vẻ!"));
            if (greeting && Normalize(question).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 4)
                return Text(turn <= 1
                    ? Pick("Chào bạn! Mình là trợ lý của 3H AUTO đây. Bạn đang tìm phụ tùng gì, cho xe nào vậy?",
                           "Alo, mình nghe nè! Bạn cần tìm phụ tùng gì cho xe của mình ạ?",
                           "Chào bạn nha! Hôm nay xe bạn cần thay gì nè?")
                    : Pick("Mình vẫn ở đây nè! Bạn cần hỏi thêm gì không?", "Dạ mình nghe đây ạ, bạn cứ hỏi nha."));
            if (Has("ok", "oke", "okela", "uk", "u", "vang", "da", "duoc", "ok roi"))
                return Text(Pick("Dạ vâng! Bạn cần mình tìm thêm gì không?", "Oke bạn! Có gì cứ nhắn mình nha."));
        }

        // --- Câu hỏi thường gặp ---
        if (Has("ban la ai", "bot", "nguoi that", "ai vay"))
            return Text("Mình là trợ lý tự động của 3H AUTO. Mình giúp bạn tìm phụ tùng, xem giá, tồn kho và hướng dẫn đặt hàng. Cần gặp nhân viên thì bạn gọi " + hotline + " nha.");
        if (Has("dat hang", "cach mua", "mua the nao", "mua sao", "thanh toan", "tra tien", "cod"))
            return Text("Dễ lắm bạn: bấm \"Thêm vào giỏ\" ở sản phẩm, mở Giỏ hàng rồi bấm \"Đặt hàng\", điền tên, số điện thoại với địa chỉ là xong. Bạn trả tiền khi nhận hàng (COD) nha.");
        if (Has("giao hang", "ship", "van chuyen", "phi ship", "bao lau", "may ngay"))
            return Text("Bên mình giao hàng toàn quốc nha bạn. Sau khi đặt, cửa hàng sẽ gọi xác nhận và báo phí ship, thời gian giao luôn.");
        if (Has("bao hanh", "doi tra", "tra hang", "loi"))
            return Text("Phụ tùng được bảo hành theo chính sách của hãng nha bạn. Thời gian bảo hành từng món có ghi trong trang chi tiết sản phẩm, có vấn đề gì bạn gọi " + hotline + " là được hỗ trợ liền.");
        if (Has("may gio", "mo cua", "gio mo cua", "gio lam viec", "lam viec", "dong cua", "chu nhat", "nghi le"))
            return Text("Cửa hàng làm việc từ 8:00 đến 17:30, Thứ 2 đến Thứ 7, Chủ nhật nghỉ nha bạn. Đặt hàng trên web thì lúc nào cũng được.");
        if (Has("hotline", "so dien thoai", "sdt", "lien he", "dia chi", "zalo"))
            return Text("Bạn gọi hotline " + hotline + " để gặp nhân viên nha. Còn tìm phụ tùng thì cứ hỏi mình ở đây cũng được.");
        if (Has("don hang", "don cua toi", "huy don", "kiem tra don"))
            return Text("Bạn vào mục \"Đơn hàng của tôi\" (bấm vào tên tài khoản ở góc trên) để xem đơn nha. Đơn còn \"Chờ xác nhận\" thì bạn tự hủy được luôn.");

        // --- Có sản phẩm phù hợp ---
        if (products.Count > 0)
        {
            var top = products[0];
            var stock = top.Stock <= 0 ? "đang tạm hết hàng"
                      : top.Stock <= InventoryRules.LowStockThreshold ? $"chỉ còn {top.Stock} cái thôi"
                      : "đang còn hàng";
            var price = top.Price.ToString("N0", vi) + "đ";
            var lead = greeting ? "Chào bạn! " : "";

            string reply;
            if (products.Count == 1)
                reply = lead + Pick(
                    $"Có nè bạn! {top.Name} (mã {top.PartNumber}) giá {price}, {stock}.",
                    $"Bên mình có {top.Name}, mã {top.PartNumber}, giá {price} và {stock} nha.");
            else
                reply = lead + Pick(
                    $"Có nè bạn! Mình tìm được {products.Count} món phù hợp. Ví dụ {top.Name} giá {price}, {stock}. Bạn xem thêm bên dưới nha.",
                    $"Bên mình đang có {products.Count} lựa chọn cho bạn, nổi bật là {top.Name} (mã {top.PartNumber}) giá {price}. Bạn xem danh sách bên dưới nhé.");

            reply += top.Stock <= 0
                ? " Món này hết rồi, bạn xem thử các lựa chọn khác hoặc gọi " + hotline + " để đặt trước nha."
                : " " + Pick("Bạn đi xe gì, đời nào để mình kiểm tra cho chắc nha?", "Bạn cần mình tư vấn thêm gì không?", "Muốn mua thì bấm \"Thêm vào giỏ\" là được nha.");
            return new ChatReply(reply, products.Take(5).ToList(), false);
        }

        // --- Không hiểu / không có sản phẩm ---
        if (greeting)
            return Text("Chào bạn! Bạn đang tìm phụ tùng gì nè? Ví dụ \"má phanh Vios\" hay \"bugi Toyota\" là mình tìm được liền.");

        return Text(Pick(
            "Hmm, mình chưa tìm thấy món này trên web. Bạn ghi rõ tên phụ tùng với dòng xe giúp mình nha, vd \"má phanh Fortuner\".",
            "Mình chưa hiểu ý bạn lắm. Bạn đang cần phụ tùng gì, cho xe nào vậy?",
            $"Món này hiện chưa có trên web bạn ơi. Bạn gọi {hotline} để bên mình kiểm tra kho giúp nha."));
    }

    private static ChatReply Text(string reply) => new(reply, new(), false);

    private static string Pick(params string[] options) => options[Random.Shared.Next(options.Length)];
}
