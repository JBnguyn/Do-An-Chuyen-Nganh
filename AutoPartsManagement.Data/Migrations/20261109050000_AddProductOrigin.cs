using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsManagement.Data.Migrations
{
    /// <summary>
    /// Thêm Xuất xứ, Mã OEM, Loại hàng (chính hãng xe / thay thế) cho phụ tùng,
    /// đồng thời chuẩn hóa lại một số mã và tên sản phẩm mẫu.
    /// </summary>
    public partial class AddProductOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OemNumber",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGenuine",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // ===== Cập nhật dữ liệu mẫu đang có (chạy 1 lần) =====

            // Phụ tùng chính hãng Toyota: mã của sản phẩm chính là mã OEM
            migrationBuilder.Sql(@"UPDATE p SET p.IsGenuine = 1, p.OemNumber = p.PartNumber
FROM Products p JOIN Brands b ON b.Id = p.BrandId
WHERE b.Name = N'Toyota Genuine Parts';");

            // NISTO bán theo mã OEM của hãng xe (Mazda / Toyota)
            migrationBuilder.Sql("UPDATE Products SET OemNumber = PartNumber WHERE PartNumber IN ('BJS7-34-156A', '48632-26010');");

            // Xuất xứ có nguồn xác nhận
            migrationBuilder.Sql("UPDATE Products SET Origin = N'Thái Lan' WHERE PartNumber IN ('XM2-185/60R15', '90915-YZZE1');");
            migrationBuilder.Sql("UPDATE Products SET Origin = N'Nhật Bản' WHERE PartNumber = '665021JAPAN';");

            // Chuẩn hóa mã / tên
            migrationBuilder.Sql(@"UPDATE Products SET PartNumber = '185/60R15 88H XM2+',
    Name = N'Lốp Michelin Energy XM2+ 185/60R15 88H (Vios, Yaris, City)'
WHERE PartNumber = 'XM2-185/60R15';");
            migrationBuilder.Sql(@"UPDATE Products SET PartNumber = 'AR24U',
    Name = N'Gạt mưa Bosch AeroTwin Retrofit AR24U (24 inch / 600 mm)',
    Description = N'Lưỡi gạt mưa thân mềm Bosch AeroTwin Retrofit, dài 600 mm (24 inch), mã Bosch 3 397 008 538. Giá cho 1 chiếc, chọn cỡ theo xe.'
WHERE PartNumber = 'AEROTWIN-24';");
            migrationBuilder.Sql("UPDATE Products SET PartNumber = '12342 XV' WHERE PartNumber = '12342XV';");
            migrationBuilder.Sql(@"UPDATE Products SET Name = N'Bơm nước động cơ Aisin cho động cơ 2ZR-FXE (Toyota hybrid)'
WHERE PartNumber = 'WPT-190';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Origin", table: "Products");
            migrationBuilder.DropColumn(name: "OemNumber", table: "Products");
            migrationBuilder.DropColumn(name: "IsGenuine", table: "Products");
        }
    }
}
