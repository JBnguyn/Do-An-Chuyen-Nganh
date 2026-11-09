namespace AutoPartsManagement.Web.Services;

/// <summary>
/// Lưu / xóa file ảnh upload trong thư mục wwwroot/uploads.
/// </summary>
public interface IImageStorage
{
    /// <summary>Kiểm tra file ảnh. Trả về thông báo lỗi, hoặc null nếu hợp lệ.</summary>
    string? Validate(IFormFile file);

    /// <summary>Lưu file vào wwwroot/uploads/{folder} và trả về đường dẫn web (vd: /uploads/products/abc.jpg).</summary>
    Task<string> SaveAsync(IFormFile file, string folder);

    /// <summary>Xóa file nếu đường dẫn nằm trong /uploads/ (bỏ qua link ảnh bên ngoài).</summary>
    void Delete(string? url);
}

public class ImageStorage : IImageStorage
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxFileSize = 2 * 1024 * 1024; // 2MB

    private readonly IWebHostEnvironment _env;

    public ImageStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string? Validate(IFormFile file)
    {
        if (file.Length == 0)
            return "File ảnh rỗng.";
        if (file.Length > MaxFileSize)
            return "Ảnh không được vượt quá 2MB.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return "Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .webp, .gif.";

        return null;
    }

    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        await using (var stream = System.IO.File.Create(Path.Combine(dir, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{folder}/{fileName}";
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));
        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));

        // Chặn đường dẫn kiểu /uploads/../../ ra ngoài thư mục uploads
        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            return;

        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }
}
