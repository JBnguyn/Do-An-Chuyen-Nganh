using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Desktop.Infrastructure;
using AutoPartsManagement.Desktop.Views;
using AutoPartsManagement.Services.Interfaces;

namespace AutoPartsManagement.Desktop.Pages;

public partial class MyOrdersPage : UserControl
{
    private Order? _current;

    public MyOrdersPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e) => await LoadAsync(selectFirst: true);

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync(selectFirst: false);

    private void Shop_Click(object sender, RoutedEventArgs e) => MainWindow.Of(this)?.NavigateTo("shop");

    private async Task LoadAsync(bool selectFirst)
    {
        var keepId = _current?.Id;
        var orders = await AppHost.RunAsync(sp => sp.GetRequiredService<IOrderService>().GetByUserAsync(Session.UserId));

        OrdersGrid.ItemsSource = orders;
        ListTitle.Text = $"Đơn hàng ({orders.Count})";
        EmptyState.Visibility = orders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        OrdersGrid.Visibility = orders.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        var select = orders.FirstOrDefault(o => o.Id == keepId) ?? (selectFirst ? orders.FirstOrDefault() : null);
        OrdersGrid.SelectedItem = select;
        if (select == null) ShowDetail(null);
    }

    private async void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not Order selected) return;
        // Lấy lại đầy đủ (kèm thông tin người nhận)
        var order = await AppHost.RunAsync(sp => sp.GetRequiredService<IOrderService>().GetByIdAsync(selected.Id));
        if (OrdersGrid.SelectedItem is Order still && still.Id == selected.Id)
            ShowDetail(order);
    }

    private void ShowDetail(Order? order)
    {
        _current = order;
        NoSelection.Visibility = order == null ? Visibility.Visible : Visibility.Collapsed;
        DetailPanel.Visibility = order == null ? Visibility.Collapsed : Visibility.Visible;
        if (order == null) return;

        DetailTitle.Text = $"Đơn hàng #{order.Id}";
        DetailDate.Text = "Đặt lúc " + order.OrderDate.ToString("HH:mm, dd/MM/yyyy");
        StatusText.Text = Format.StatusLabel(order.Status);
        StatusPill.Background = new SolidColorBrush(Format.StatusColor(order.Status));

        ReceiverText.Text = order.Customer == null ? "" : $"{order.Customer.Name} · {order.Customer.Phone}";
        AddressText.Text = order.Customer?.Address ?? "";
        NotesText.Text = string.IsNullOrWhiteSpace(order.Notes) ? "" : "Ghi chú: " + order.Notes;
        NotesText.Visibility = string.IsNullOrWhiteSpace(order.Notes) ? Visibility.Collapsed : Visibility.Visible;

        LinesList.ItemsSource = order.OrderDetails.ToList();
        DetailTotal.Text = Format.Money(order.TotalAmount);

        var canCancel = order.Status == OrderStatus.Pending;
        CancelButton.Visibility = canCancel ? Visibility.Visible : Visibility.Collapsed;
        CancelHint.Text = canCancel
            ? "Bạn có thể hủy khi đơn còn ở trạng thái Chờ xác nhận."
            : order.Status == OrderStatus.Cancelled ? "Đơn đã được hủy, tồn kho đã được hoàn lại." : "Đơn đã được xác nhận nên không thể tự hủy.";
    }

    private async void CancelOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        if (MessageBox.Show($"Bạn chắc chắn muốn hủy đơn #{_current.Id}?", "Hủy đơn hàng",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var id = _current.Id;
        var result = await AppHost.RunAsync(sp => sp.GetRequiredService<IOrderService>().CancelByUserAsync(id, Session.UserId));
        MainWindow.Of(this)?.ShowToast(result.Message ?? (result.Succeeded ? "Đã hủy đơn." : "Không hủy được đơn."), result.Succeeded);
        await LoadAsync(selectFirst: false);
        if (OrdersGrid.SelectedItem is Order o && o.Id == id)
            ShowDetail(await AppHost.RunAsync(sp => sp.GetRequiredService<IOrderService>().GetByIdAsync(id)));
    }
}
