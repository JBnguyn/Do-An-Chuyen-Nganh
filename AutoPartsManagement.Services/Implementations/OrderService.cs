using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

/// <summary>
/// Nghiệp vụ đơn hàng: đặt hàng từ giỏ, xem đơn, hủy đơn, đổi trạng thái.
/// Tồn kho bị trừ khi đặt hàng và được cộng lại khi đơn bị hủy.
/// </summary>
public class OrderService : IOrderService
{
    private const int MaxLines = 50;
    private const int MaxQuantityPerLine = 999;

    private readonly ApplicationDbContext _db;

    public OrderService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(ServiceResult Result, int OrderId)> PlaceOrderAsync(string userId, CheckoutInfo info, IReadOnlyList<CartLine> lines)
    {
        // Gộp các dòng trùng sản phẩm, bỏ dòng số lượng không hợp lệ
        var wanted = lines
            .Where(l => l.ProductId > 0 && l.Quantity > 0)
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => Math.Min(g.Sum(x => x.Quantity), MaxQuantityPerLine));

        if (wanted.Count == 0)
            return (ServiceResult.Failure("Giỏ hàng đang trống."), 0);
        if (wanted.Count > MaxLines)
            return (ServiceResult.Failure($"Mỗi đơn tối đa {MaxLines} sản phẩm khác nhau."), 0);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var ids = wanted.Keys.ToList();
        var products = await _db.Products
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // Kiểm tra từng sản phẩm
        foreach (var (productId, qty) in wanted)
        {
            if (!products.TryGetValue(productId, out var p) || !p.IsActive)
                return (ServiceResult.Failure("Có sản phẩm trong giỏ không còn kinh doanh. Vui lòng xóa khỏi giỏ và thử lại."), 0);
            if (p.Stock <= 0)
                return (ServiceResult.Failure($"\"{p.Name}\" đã hết hàng."), 0);
            if (p.Stock < qty)
                return (ServiceResult.Failure($"\"{p.Name}\" chỉ còn {p.Stock} sản phẩm."), 0);
        }

        // Khách hàng gắn với tài khoản: tạo mới hoặc cập nhật thông tin giao hàng
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null)
        {
            customer = new Customer { UserId = userId, CreatedAt = DateTime.Now };
            _db.Customers.Add(customer);
        }
        customer.Name = info.Name.Trim();
        customer.Phone = info.Phone.Trim();
        customer.Address = info.Address.Trim();
        customer.Email = string.IsNullOrWhiteSpace(info.Email) ? null : info.Email.Trim();

        var order = new Order
        {
            Customer = customer,
            OrderDate = DateTime.Now,
            CreatedAt = DateTime.Now,
            Status = OrderStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(info.Notes) ? null : info.Notes.Trim()
        };

        foreach (var (productId, qty) in wanted)
        {
            var p = products[productId];
            order.OrderDetails.Add(new OrderDetail { ProductId = p.Id, Quantity = qty, UnitPrice = p.Price });
            p.Stock -= qty;
            p.UpdatedAt = DateTime.Now;
        }
        order.TotalAmount = order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (ServiceResult.Success($"Đặt hàng thành công! Mã đơn của bạn là #{order.Id}."), order.Id);
    }

    public Task<Customer?> GetCustomerByUserAsync(string userId) =>
        _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);

    public Task<List<Order>> GetByUserAsync(string userId) =>
        _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Where(o => o.Customer!.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public Task<Order?> GetByIdAsync(int id) =>
        _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public Task<PagedResult<Order>> GetPagedAsync(OrderStatus? status, string? search, int page, int pageSize)
    {
        IQueryable<Order> query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().TrimStart('#');
            if (int.TryParse(s, out var orderId))
                query = query.Where(o => o.Id == orderId || o.Customer!.Phone.Contains(s));
            else
                query = query.Where(o => o.Customer!.Name.Contains(s) || o.Customer.Phone.Contains(s));
        }

        return query.OrderByDescending(o => o.OrderDate).ToPagedResultAsync(page, pageSize);
    }

    public async Task<Dictionary<OrderStatus, int>> CountByStatusAsync()
    {
        var counts = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return Enum.GetValues<OrderStatus>()
            .ToDictionary(s => s, s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);
    }

    public async Task<ServiceResult> UpdateStatusAsync(int id, OrderStatus newStatus)
    {
        var order = await _db.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return ServiceResult.Failure("Không tìm thấy đơn hàng.");

        if (order.Status == newStatus)
            return ServiceResult.Success();

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
            return ServiceResult.Failure("Đơn đã hoàn tất hoặc đã hủy, không thể đổi trạng thái.");

        if (newStatus == OrderStatus.Cancelled)
            await RestoreStockAsync(order);

        order.Status = newStatus;
        await _db.SaveChangesAsync();
        return ServiceResult.Success($"Đã cập nhật đơn #{order.Id}.");
    }

    public async Task<ServiceResult> CancelByUserAsync(int id, string userId)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null || order.Customer?.UserId != userId)
            return ServiceResult.Failure("Không tìm thấy đơn hàng.");

        if (order.Status != OrderStatus.Pending)
            return ServiceResult.Failure("Chỉ có thể hủy đơn khi đơn còn ở trạng thái Chờ xác nhận.");

        await RestoreStockAsync(order);
        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        return ServiceResult.Success($"Đã hủy đơn #{order.Id}.");
    }

    /// <summary>Cộng lại tồn kho cho các sản phẩm trong đơn bị hủy.</summary>
    private async Task RestoreStockAsync(Order order)
    {
        var ids = order.OrderDetails.Select(d => d.ProductId).ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        foreach (var d in order.OrderDetails)
        {
            if (products.TryGetValue(d.ProductId, out var p))
            {
                p.Stock += d.Quantity;
                p.UpdatedAt = DateTime.Now;
            }
        }
    }
}
