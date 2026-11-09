using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AutoPartsManagement.Desktop.Infrastructure;
using AutoPartsManagement.Desktop.Pages;

namespace AutoPartsManagement.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.8) };

    public MainWindow()
    {
        InitializeComponent();
        _toastTimer.Tick += (_, _) => { Toast.Visibility = Visibility.Collapsed; _toastTimer.Stop(); };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UserNameText.Text = Session.UserName;
        AvatarText.Text = Session.UserName.Length > 0 ? Session.UserName[..1].ToUpperInvariant() : "?";
        RoleText.Text = Session.IsAdmin ? "Quản trị viên" : "Khách hàng";
        AdminNav.Visibility = Session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        Title = $"3H AUTO - {Session.UserName}";

        Cart.Changed += UpdateCartBadge;
        UpdateCartBadge();

        // Admin vào thẳng trang Tổng quan, khách vào Cửa hàng
        if (Session.IsAdmin) NavDashboard.IsChecked = true;
        else NavShop.IsChecked = true;
    }

    private void Window_Closed(object? sender, EventArgs e) => Cart.Changed -= UpdateCartBadge;

    // ===== Điều hướng =====

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton nav) ShowPage(nav);
    }

    private void ShowPage(RadioButton nav)
    {
        (string Title, string Subtitle, object Page) target = nav.Name switch
        {
            nameof(NavShop) => ("Cửa hàng", "Tìm phụ tùng theo tên, mã phụ tùng hoặc mã OEM", new ShopPage()),
            nameof(NavCart) => ("Giỏ hàng", "Kiểm tra sản phẩm và điền thông tin giao hàng", new CartPage()),
            nameof(NavMyOrders) => ("Đơn hàng của tôi", "Theo dõi trạng thái và hủy đơn khi còn chờ xác nhận", new MyOrdersPage()),
            nameof(NavMaintenance) => ("Nhắc bảo dưỡng", "Lịch thay dầu, lọc, má phanh... theo số km", new PlaceholderPage("Nhắc bảo dưỡng", "Phần C")),
            nameof(NavDashboard) => ("Tổng quan", "Doanh thu, đơn hàng và tồn kho", new PlaceholderPage("Tổng quan", "Phần B")),
            nameof(NavOrders) => ("Quản lý đơn hàng", "Xác nhận, giao hàng và hủy đơn", new PlaceholderPage("Quản lý đơn hàng", "Phần B")),
            nameof(NavProducts) => ("Sản phẩm & tồn kho", "Danh sách phụ tùng và số lượng tồn", new PlaceholderPage("Sản phẩm & tồn kho", "Phần B")),
            nameof(NavImports) => ("Nhập kho", "Tạo phiếu nhập hàng từ nhà cung cấp", new PlaceholderPage("Nhập kho", "Phần B")),
            _ => ("", "", new PlaceholderPage("", ""))
        };
        PageTitle.Text = target.Title;
        PageSubtitle.Text = target.Subtitle;
        PageHost.Content = target.Page;
    }

    /// <summary>Cho các trang con chuyển trang (vd đặt hàng xong -> "Đơn hàng của tôi").</summary>
    public void NavigateTo(string page)
    {
        var nav = page switch
        {
            "cart" => NavCart,
            "myorders" => NavMyOrders,
            _ => NavShop
        };
        if (nav.IsChecked == true) ShowPage(nav);   // đang ở trang đó thì tải lại
        else nav.IsChecked = true;
    }

    private void CartButton_Click(object sender, RoutedEventArgs e) => NavigateTo("cart");

    // ===== Giỏ hàng & thông báo =====

    private void UpdateCartBadge()
    {
        var count = Cart.Count;
        CartCount.Text = count > 99 ? "99+" : count.ToString();
        CartBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ShowToast(string message, bool success = true)
    {
        ToastIcon.Text = success ? "" : "";
        ToastIcon.Foreground = new SolidColorBrush(success ? Color.FromRgb(0x4A, 0xDE, 0x80) : Color.FromRgb(0xFC, 0xA5, 0xA5));
        ToastText.Text = message;
        Toast.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    /// <summary>Lấy cửa sổ chính từ một trang con.</summary>
    public static MainWindow? Of(DependencyObject child) => GetWindow(child) as MainWindow;

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Bạn muốn đăng xuất?", "3H AUTO", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        Session.SignOut();
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }
}
