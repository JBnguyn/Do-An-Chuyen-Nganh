using AutoPartsManagement.Data.Models.Enums;

namespace AutoPartsManagement.Web.Helpers;

/// <summary>Hiển thị trạng thái phiếu nhập bằng tiếng Việt.</summary>
public static class ImportUi
{
    public static string Label(ImportStatus s) => s switch
    {
        ImportStatus.Pending => "Chờ duyệt",
        ImportStatus.Approved => "Đã duyệt",
        ImportStatus.Received => "Đã nhận hàng",
        ImportStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };

    public static string Badge(ImportStatus s) => s switch
    {
        ImportStatus.Pending => "bg-warning text-dark",
        ImportStatus.Approved => "bg-info text-dark",
        ImportStatus.Received => "bg-success",
        ImportStatus.Cancelled => "bg-secondary",
        _ => "bg-light text-dark"
    };
}
