using System.Globalization;

namespace AutoPartsManagement.Web.Helpers;

/// <summary>
/// Định dạng hiển thị theo kiểu Việt Nam, không phụ thuộc ngôn ngữ cài trên máy chủ.
/// </summary>
public static class Format
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Money(decimal value) => string.Format(Vi, "{0:N0} ₫", value);

    public static string Number(int value) => value.ToString("N0", Vi);

    public static string ShortDate(System.DateTime value) => value.ToString("dd/MM/yyyy");

    public static string DateAndTime(System.DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm") ?? "—";
}
