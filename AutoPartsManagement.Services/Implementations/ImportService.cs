using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

/// <summary>
/// Nghiệp vụ nhập kho (phiếu nhập hàng từ nhà cung cấp).
/// Quy trình: Chờ duyệt -> Đã duyệt -> Đã nhận hàng (cộng tồn kho). Phiếu chưa nhận hàng có thể hủy.
/// </summary>
public class ImportService : IImportService
{
    private const int MaxLines = 100;
    private const int MaxQuantity = 10_000;

    private readonly ApplicationDbContext _db;

    public ImportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<Import>> GetPagedAsync(ImportStatus? status, int? supplierId, int page, int pageSize)
    {
        IQueryable<Import> query = _db.Imports
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.ImportDetails);

        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (supplierId.HasValue) query = query.Where(i => i.SupplierId == supplierId.Value);

        return query.OrderByDescending(i => i.ImportDate).ThenByDescending(i => i.Id).ToPagedResultAsync(page, pageSize);
    }

    public Task<Import?> GetByIdAsync(int id) =>
        _db.Imports
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.ImportDetails).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Dictionary<ImportStatus, int>> CountByStatusAsync()
    {
        var counts = await _db.Imports
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        return Enum.GetValues<ImportStatus>()
            .ToDictionary(s => s, s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);
    }

    public async Task<(ServiceResult Result, int ImportId)> CreateAsync(int supplierId, string? notes, IReadOnlyList<ImportLine> lines, bool receiveNow)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.Id == supplierId))
            return (ServiceResult.Failure("Vui lòng chọn nhà cung cấp.", "SupplierId"), 0);

        var valid = lines.Where(l => l.ProductId > 0).ToList();
        if (valid.Count == 0)
            return (ServiceResult.Failure("Phiếu nhập phải có ít nhất 1 sản phẩm."), 0);
        if (valid.Count > MaxLines)
            return (ServiceResult.Failure($"Mỗi phiếu tối đa {MaxLines} dòng."), 0);
        if (valid.Any(l => l.Quantity < 1 || l.Quantity > MaxQuantity))
            return (ServiceResult.Failure("Số lượng mỗi dòng phải từ 1 đến 10.000."), 0);
        if (valid.Any(l => l.UnitPrice < 0))
            return (ServiceResult.Failure("Giá nhập không được âm."), 0);
        if (valid.GroupBy(l => l.ProductId).Any(g => g.Count() > 1))
            return (ServiceResult.Failure("Mỗi sản phẩm chỉ nên xuất hiện 1 dòng trong phiếu. Vui lòng gộp lại."), 0);

        var ids = valid.Select(l => l.ProductId).ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        if (products.Count != ids.Count)
            return (ServiceResult.Failure("Có sản phẩm không tồn tại."), 0);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var import = new Import
        {
            SupplierId = supplierId,
            ImportDate = DateTime.Now,
            CreatedAt = DateTime.Now,
            Status = ImportStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
        foreach (var l in valid)
            import.ImportDetails.Add(new ImportDetail { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice });
        import.TotalAmount = import.ImportDetails.Sum(d => d.Quantity * d.UnitPrice);

        if (receiveNow)
            ApplyReceive(import, products);

        _db.Imports.Add(import);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (ServiceResult.Success(receiveNow
            ? $"Đã tạo phiếu nhập #{import.Id} và cộng tồn kho."
            : $"Đã tạo phiếu nhập #{import.Id} (chờ duyệt)."), import.Id);
    }

    public async Task<ServiceResult> ApproveAsync(int id)
    {
        var import = await _db.Imports.FindAsync(id);
        if (import == null) return ServiceResult.Failure("Không tìm thấy phiếu nhập.");
        if (import.Status != ImportStatus.Pending)
            return ServiceResult.Failure("Chỉ duyệt được phiếu đang chờ duyệt.");

        import.Status = ImportStatus.Approved;
        await _db.SaveChangesAsync();
        return ServiceResult.Success($"Đã duyệt phiếu nhập #{id}.");
    }

    public async Task<ServiceResult> ReceiveAsync(int id)
    {
        var import = await _db.Imports.Include(i => i.ImportDetails).FirstOrDefaultAsync(i => i.Id == id);
        if (import == null) return ServiceResult.Failure("Không tìm thấy phiếu nhập.");
        if (import.Status is ImportStatus.Received or ImportStatus.Cancelled)
            return ServiceResult.Failure("Phiếu đã nhận hàng hoặc đã hủy.");

        var ids = import.ImportDetails.Select(d => d.ProductId).ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        ApplyReceive(import, products);

        await _db.SaveChangesAsync();
        return ServiceResult.Success($"Đã nhận hàng phiếu #{id}, tồn kho đã được cộng.");
    }

    public async Task<ServiceResult> CancelAsync(int id)
    {
        var import = await _db.Imports.FindAsync(id);
        if (import == null) return ServiceResult.Failure("Không tìm thấy phiếu nhập.");
        if (import.Status is ImportStatus.Received or ImportStatus.Cancelled)
            return ServiceResult.Failure("Không thể hủy phiếu đã nhận hàng hoặc đã hủy.");

        import.Status = ImportStatus.Cancelled;
        await _db.SaveChangesAsync();
        return ServiceResult.Success($"Đã hủy phiếu nhập #{id}.");
    }

    public Task<List<Product>> GetLowStockAsync() =>
        _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.Stock <= InventoryRules.LowStockThreshold)
            .OrderBy(p => p.Stock).ThenBy(p => p.Name)
            .ToListAsync();

    /// <summary>Cộng tồn kho theo phiếu và chuyển trạng thái sang Đã nhận hàng.</summary>
    private static void ApplyReceive(Import import, Dictionary<int, Product> products)
    {
        foreach (var d in import.ImportDetails)
        {
            if (products.TryGetValue(d.ProductId, out var p))
            {
                p.Stock += d.Quantity;
                p.UpdatedAt = DateTime.Now;
            }
        }
        import.Status = ImportStatus.Received;
    }
}
