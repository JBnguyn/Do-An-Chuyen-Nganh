using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Desktop.Infrastructure;
using AutoPartsManagement.Desktop.Views;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Desktop.Pages;

/// <summary>Thẻ sản phẩm hiển thị trên lưới (bọc Product để có sẵn chuỗi hiển thị).</summary>
public class ProductCard
{
    private static readonly Brush Green = new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D));
    private static readonly Brush Orange = new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));
    private static readonly Brush Gray = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));

    public ProductCard(Product p) => Product = p;

    public Product Product { get; }
    public string Name => Product.Name;
    public string? Description => string.IsNullOrWhiteSpace(Product.Description) ? null : Product.Description;
    public string? ImageUrl => Product.ImageUrl;
    public bool IsGenuine => Product.IsGenuine;
    public string PriceText => Format.Money(Product.Price);
    public bool CanBuy => Product.Stock > 0;

    public string Codes => string.IsNullOrWhiteSpace(Product.OemNumber) || Product.OemNumber == Product.PartNumber
        ? $"Mã: {Product.PartNumber}"
        : $"Mã: {Product.PartNumber} · OEM: {Product.OemNumber}";

    public string Meta => string.Join(" · ", new[] { Product.Brand?.Name, Product.Origin }
        .Where(s => !string.IsNullOrWhiteSpace(s)));

    public string StockText => Product.Stock switch
    {
        <= 0 => "Hết hàng",
        <= InventoryRules.LowStockThreshold => $"Sắp hết - còn {Product.Stock}",
        _ => $"Còn hàng ({Product.Stock})"
    };

    public Brush StockBrush => Product.Stock switch
    {
        <= 0 => Gray,
        <= InventoryRules.LowStockThreshold => Orange,
        _ => Green
    };
}

public partial class ShopPage : UserControl
{
    private const int PageSize = 12;
    private int _page = 1;
    private int _totalPages = 1;
    private bool _ready;
    private int _loadVersion;   // bỏ kết quả cũ nếu người dùng đổi bộ lọc liên tục

    public ShopPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_ready) return;

        var categories = await AppHost.RunAsync(sp => sp.GetRequiredService<ICategoryService>().GetAllAsync());
        CategoryBox.ItemsSource = new List<Category> { new() { Id = 0, Name = "Tất cả danh mục" } }
            .Concat(categories.OrderBy(c => c.Name))
            .ToList();
        CategoryBox.SelectedIndex = 0;
        SortBox.SelectedIndex = 0;

        _ready = true;
        await LoadAsync();
        SearchBox.Focus();
    }

    private async Task LoadAsync()
    {
        var version = ++_loadVersion;
        var filter = new ProductFilter
        {
            Search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text.Trim(),
            CategoryId = CategoryBox.SelectedValue is int id && id > 0 ? id : null,
            Genuine = GenuineBox.IsChecked == true ? true : null,
            IsActive = true,   // khách chỉ thấy hàng đang kinh doanh
            SortBy = (SortBox.SelectedItem as ComboBoxItem)?.Tag as string,
            Page = _page,
            PageSize = PageSize
        };

        LoadingText.Visibility = Visibility.Visible;
        try
        {
            var result = await AppHost.RunAsync(sp => sp.GetRequiredService<IProductService>().GetPagedAsync(filter));
            if (version != _loadVersion) return;

            ProductList.ItemsSource = result.Items.Select(p => new ProductCard(p)).ToList();
            _page = result.PageIndex;
            _totalPages = Math.Max(1, result.TotalPages);

            PageText.Text = $"Trang {_page} / {_totalPages}";
            PrevButton.IsEnabled = result.HasPrevious;
            NextButton.IsEnabled = result.HasNext;
            ResultText.Text = $"{result.TotalCount} sản phẩm";
            EmptyState.Visibility = result.TotalCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            Scroller.ScrollToTop();
        }
        finally
        {
            if (version == _loadVersion) LoadingText.Visibility = Visibility.Collapsed;
        }
    }

    private async void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        _page = 1;
        await LoadAsync();
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        _page = 1;
        await LoadAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _page = 1;
        await LoadAsync();
    }

    private async void Prev_Click(object sender, RoutedEventArgs e)
    {
        if (_page <= 1) return;
        _page--;
        await LoadAsync();
    }

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_page >= _totalPages) return;
        _page++;
        await LoadAsync();
    }

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not ProductCard card) return;
        var error = Cart.Add(card.Product);
        MainWindow.Of(this)?.ShowToast(error ?? $"Đã thêm \"{card.Name}\" vào giỏ hàng.", success: error == null);
    }
}
