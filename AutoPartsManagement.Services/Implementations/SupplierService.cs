using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

public class SupplierService : ISupplierService
{
    private readonly ApplicationDbContext _db;

    public SupplierService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<SupplierListItem>> GetPagedAsync(string? search, int page, int pageSize)
    {
        var query = _db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.Name.Contains(s)
                                     || x.Phone.Contains(s)
                                     || (x.Email != null && x.Email.Contains(s))
                                     || (x.Address != null && x.Address.Contains(s)));
        }

        return query
            .OrderBy(x => x.Name)
            .Select(x => new SupplierListItem
            {
                Id = x.Id,
                Name = x.Name,
                Phone = x.Phone,
                Email = x.Email,
                Address = x.Address,
                CreatedAt = x.CreatedAt,
                ImportCount = x.Imports.Count
            })
            .ToPagedResultAsync(page, pageSize);
    }

    public Task<List<Supplier>> GetAllAsync() =>
        _db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();

    public Task<Supplier?> GetByIdAsync(int id) =>
        _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public Task<int> CountImportsAsync(int id) =>
        _db.Imports.CountAsync(i => i.SupplierId == id);

    public async Task<ServiceResult> CreateAsync(Supplier supplier)
    {
        Normalize(supplier);

        if (await _db.Suppliers.AnyAsync(x => x.Phone == supplier.Phone))
            return ServiceResult.Failure("Số điện thoại này đã được dùng cho nhà cung cấp khác.", nameof(Supplier.Phone));

        supplier.CreatedAt = DateTime.Now;
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(Supplier supplier)
    {
        var entity = await _db.Suppliers.FindAsync(supplier.Id);
        if (entity == null)
            return ServiceResult.Failure("Không tìm thấy nhà cung cấp.");

        Normalize(supplier);
        if (await _db.Suppliers.AnyAsync(x => x.Phone == supplier.Phone && x.Id != supplier.Id))
            return ServiceResult.Failure("Số điện thoại này đã được dùng cho nhà cung cấp khác.", nameof(Supplier.Phone));

        entity.Name = supplier.Name;
        entity.Phone = supplier.Phone;
        entity.Email = supplier.Email;
        entity.Address = supplier.Address;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await _db.Suppliers.FindAsync(id);
        if (entity == null)
            return ServiceResult.Failure("Không tìm thấy nhà cung cấp.");

        var importCount = await CountImportsAsync(id);
        if (importCount > 0)
            return ServiceResult.Failure(
                $"Không thể xóa: nhà cung cấp \"{entity.Name}\" đã có {importCount} phiếu nhập.");

        _db.Suppliers.Remove(entity);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    private static void Normalize(Supplier s)
    {
        s.Name = s.Name.Trim();
        s.Phone = s.Phone.Trim();
        s.Email = string.IsNullOrWhiteSpace(s.Email) ? null : s.Email.Trim();
        s.Address = string.IsNullOrWhiteSpace(s.Address) ? null : s.Address.Trim();
    }
}
