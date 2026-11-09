using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using AutoPartsManagement.Desktop.Infrastructure;
using AutoPartsManagement.Desktop.Views;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Desktop.Pages;

public partial class CartPage : UserControl
{
    // Giống quy tắc trên trang thanh toán của web
    private static readonly Regex PhoneRegex = new(@"^(\+?84|0)[0-9]{8,10}$");

    public CartPage()
    {
        InitializeComponent();
        CartList.ItemsSource = Cart.Items;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Cart.Changed += Refresh;
        Refresh();

        // Điền sẵn thông tin giao hàng của lần đặt trước
        var customer = await AppHost.RunAsync(sp => sp.GetRequiredService<IOrderService>().GetCustomerByUserAsync(Session.UserId));
        if (customer != null)
        {
            if (NameBox.Text.Length == 0) NameBox.Text = customer.Name;
            if (PhoneBox.Text.Length == 0) PhoneBox.Text = customer.Phone;
            if (AddressBox.Text.Length == 0) AddressBox.Text = customer.Address ?? "";
            if (EmailBox.Text.Length == 0) EmailBox.Text = customer.Email ?? "";
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e) => Cart.Changed -= Refresh;

    private void Refresh()
    {
        var empty = Cart.Items.Count == 0;
        CartTitle.Text = empty ? "Giỏ hàng" : $"Giỏ hàng ({Cart.Count} sản phẩm)";
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
        SubtotalText.Text = Format.Money(Cart.Total);
        TotalText.Text = Format.Money(Cart.Total);
        PlaceOrderButton.IsEnabled = !empty;
    }

    private static CartItem? ItemOf(object sender) => (sender as Button)?.Tag as CartItem;

    private void Minus_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is { } item) Cart.SetQuantity(item, item.Quantity - 1);
    }

    private void Plus_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is not { } item) return;
        if (item.Quantity >= item.Stock)
        {
            MainWindow.Of(this)?.ShowToast($"\"{item.Name}\" chỉ còn {item.Stock} sản phẩm.", success: false);
            return;
        }
        Cart.SetQuantity(item, item.Quantity + 1);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is { } item) Cart.Remove(item);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Xóa tất cả sản phẩm khỏi giỏ hàng?", "3H AUTO",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            Cart.Clear();
    }

    private void ContinueShopping_Click(object sender, RoutedEventArgs e) => MainWindow.Of(this)?.NavigateTo("shop");

    private async void PlaceOrder_Click(object sender, RoutedEventArgs e)
    {
        ErrorBox.Visibility = Visibility.Collapsed;

        var info = new CheckoutInfo
        {
            Name = NameBox.Text.Trim(),
            Phone = PhoneBox.Text.Trim().Replace(" ", "").Replace(".", ""),
            Address = AddressBox.Text.Trim(),
            Email = string.IsNullOrWhiteSpace(EmailBox.Text) ? null : EmailBox.Text.Trim(),
            Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim()
        };

        var error = Validate(info);
        if (error != null)
        {
            ShowError(error);
            return;
        }

        var lines = Cart.Items.Select(i => new CartLine { ProductId = i.ProductId, Quantity = i.Quantity }).ToList();

        PlaceOrderButton.IsEnabled = false;
        try
        {
            // Dùng đúng OrderService của web: kiểm tra tồn kho, tính tiền theo giá DB, trừ kho trong 1 transaction
            var (result, orderId) = await AppHost.RunAsync(sp =>
                sp.GetRequiredService<IOrderService>().PlaceOrderAsync(Session.UserId, info, lines));

            if (!result.Succeeded)
            {
                ShowError(result.Message ?? "Không đặt được hàng, vui lòng thử lại.");
                return;
            }

            Cart.Clear();
            MessageBox.Show($"Đặt hàng thành công!\nMã đơn của bạn là #{orderId}.\n\nChúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.",
                "3H AUTO", MessageBoxButton.OK, MessageBoxImage.Information);
            MainWindow.Of(this)?.NavigateTo("myorders");
        }
        finally
        {
            PlaceOrderButton.IsEnabled = Cart.Items.Count > 0;
        }
    }

    private static string? Validate(CheckoutInfo info)
    {
        if (Cart.Items.Count == 0) return "Giỏ hàng đang trống.";
        if (info.Name.Length < 2) return "Vui lòng nhập họ tên người nhận.";
        if (!PhoneRegex.IsMatch(info.Phone)) return "Số điện thoại không hợp lệ (vd: 0912345678).";
        if (info.Address.Length < 5) return "Vui lòng nhập địa chỉ giao hàng đầy đủ.";
        if (info.Email != null && !Regex.IsMatch(info.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return "Email không hợp lệ.";
        return null;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBox.Visibility = Visibility.Visible;
    }
}
