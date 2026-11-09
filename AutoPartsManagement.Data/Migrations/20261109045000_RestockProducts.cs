using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsManagement.Data.Migrations
{
    /// <summary>
    /// Migration dữ liệu (không đổi cấu trúc bảng): nhập lại hàng một lần cho các phụ tùng
    /// đang hết hoặc sắp hết, để tất cả sản phẩm đều còn hàng khi demo.
    /// Sau đó tồn kho chỉ thay đổi khi khách đặt hàng (trừ) hoặc hủy đơn (cộng lại).
    /// Chỉ chạy 1 lần trên mỗi database.
    /// </summary>
    public partial class RestockProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Products SET Stock = 30, UpdatedAt = GETDATE() WHERE Stock < 10;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không hoàn tác: không biết số tồn kho cũ của từng sản phẩm.
        }
    }
}
