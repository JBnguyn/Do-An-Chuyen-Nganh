namespace AutoPartsManagement.Services.Common;

/// <summary>
/// Kết quả trả về từ tầng Service: thành công hoặc thất bại kèm thông báo lỗi.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; private init; }

    /// <summary>Thông báo hiển thị cho người dùng (lỗi hoặc thông tin thêm khi thành công).</summary>
    public string? Message { get; private init; }

    /// <summary>Tên thuộc tính bị lỗi (để gắn lỗi vào đúng ô nhập trên form). Null = lỗi chung.</summary>
    public string? Field { get; private init; }

    public static ServiceResult Success(string? message = null) => new() { Succeeded = true, Message = message };

    public static ServiceResult Failure(string message, string? field = null) =>
        new() { Succeeded = false, Message = message, Field = field };
}
