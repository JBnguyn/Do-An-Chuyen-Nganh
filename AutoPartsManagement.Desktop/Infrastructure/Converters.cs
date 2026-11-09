using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AutoPartsManagement.Data.Models.Enums;

namespace AutoPartsManagement.Desktop.Infrastructure;

public static class Format
{
    public static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Money(decimal value) => value.ToString("N0", Vi) + " ₫";

    public static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Processing => "Đang chuẩn bị",
        OrderStatus.Shipped => "Đang giao",
        OrderStatus.Delivered => "Đã giao",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    public static Color StatusColor(OrderStatus status) => status switch
    {
        OrderStatus.Pending => Color.FromRgb(0xB4, 0x53, 0x09),
        OrderStatus.Processing => Color.FromRgb(0x03, 0x69, 0xA1),
        OrderStatus.Shipped => Color.FromRgb(0x1D, 0x4E, 0xD8),
        OrderStatus.Delivered => Color.FromRgb(0x15, 0x80, 0x3D),
        _ => Color.FromRgb(0x6B, 0x72, 0x80)
    };
}

/// <summary>decimal -> "1.250.000 ₫"</summary>
public class MoneyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is decimal d ? Format.Money(d) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>OrderStatus -> chữ tiếng Việt (parameter "color" -> màu chữ).</summary>
public class OrderStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not OrderStatus s) return "";
        if (parameter as string == "color") return new SolidColorBrush(Format.StatusColor(s));
        return Format.StatusLabel(s);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>null / chuỗi rỗng -> Collapsed (parameter "invert" để đảo lại).</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hasValue = value is not null && (value is not string s || s.Length > 0);
        if (parameter as string == "invert") hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Đường dẫn ảnh của web (vd "/uploads/products/a.jpg") -> ảnh hiển thị trên app.
/// Ảnh nằm trong thư mục wwwroot của project Web nên app đọc trực tiếp từ đó.
/// </summary>
public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ImagePaths.Load(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public static class ImagePaths
{
    private static string? _webRoot;
    private static bool _searched;

    /// <summary>Thư mục wwwroot của web: lấy từ appsettings "WebRootPath", không có thì tự dò ngược lên các thư mục cha.</summary>
    public static string? WebRoot
    {
        get
        {
            if (_searched) return _webRoot;
            _searched = true;

            var configured = AppHost.Configuration?["WebRootPath"];
            if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
                return _webRoot = configured;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "AutoPartsManagement.Web", "wwwroot");
                if (Directory.Exists(candidate)) return _webRoot = candidate;
                dir = dir.Parent;
            }
            return null;
        }
    }

    public static ImageSource? Load(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        try
        {
            Uri uri;
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                uri = new Uri(url);
            }
            else
            {
                if (WebRoot == null) return null;
                var path = Path.Combine(WebRoot, url.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) return null;
                uri = new Uri(path);
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;   // đọc xong nhả file, web vẫn ghi đè ảnh được
            bmp.DecodePixelWidth = 320;                    // thu nhỏ cho nhẹ
            bmp.EndInit();
            if (bmp.CanFreeze) bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;  // ảnh lỗi -> hiện biểu tượng thay thế
        }
    }
}
