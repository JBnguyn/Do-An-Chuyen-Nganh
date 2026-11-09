using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoPartsManagement.Data.Models;

namespace AutoPartsManagement.Desktop.Infrastructure;

/// <summary>Một dòng trong giỏ hàng.</summary>
public class CartItem : INotifyPropertyChanged
{
    private int _quantity;

    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PartNumber { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity == value) return;
            _quantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
        }
    }

    public decimal LineTotal => Price * Quantity;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Giỏ hàng lưu trong bộ nhớ của app. Giá và tồn kho được kiểm tra lại
/// trong database khi đặt hàng (OrderService), nên không sợ giá cũ.
/// </summary>
public static class Cart
{
    public static ObservableCollection<CartItem> Items { get; } = new();

    /// <summary>Báo cho header cập nhật số lượng trên biểu tượng giỏ hàng.</summary>
    public static event Action? Changed;

    public static int Count => Items.Sum(i => i.Quantity);
    public static decimal Total => Items.Sum(i => i.LineTotal);

    /// <returns>null nếu thêm được, ngược lại là thông báo lỗi.</returns>
    public static string? Add(Product p, int quantity = 1)
    {
        if (p.Stock <= 0) return $"\"{p.Name}\" đã hết hàng.";

        var item = Items.FirstOrDefault(i => i.ProductId == p.Id);
        var current = item?.Quantity ?? 0;
        if (current + quantity > p.Stock)
            return $"\"{p.Name}\" chỉ còn {p.Stock} sản phẩm (giỏ đã có {current}).";

        if (item == null)
        {
            Items.Add(new CartItem
            {
                ProductId = p.Id,
                Name = p.Name,
                PartNumber = p.PartNumber,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                Stock = p.Stock,
                Quantity = quantity
            });
        }
        else
        {
            item.Quantity += quantity;
        }
        Changed?.Invoke();
        return null;
    }

    public static void SetQuantity(CartItem item, int quantity)
    {
        if (quantity <= 0) { Remove(item); return; }
        item.Quantity = Math.Min(quantity, item.Stock);
        Changed?.Invoke();
    }

    public static void Remove(CartItem item)
    {
        Items.Remove(item);
        Changed?.Invoke();
    }

    public static void Clear()
    {
        Items.Clear();
        Changed?.Invoke();
    }
}
