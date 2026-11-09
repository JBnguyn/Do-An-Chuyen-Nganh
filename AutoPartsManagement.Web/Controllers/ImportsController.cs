using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using AutoPartsManagement.Web.Identity;
using AutoPartsManagement.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Nhập kho (chỉ Admin): lập phiếu nhập từ nhà cung cấp, duyệt, xác nhận nhận hàng (cộng tồn kho), hủy.
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
public class ImportsController : Controller
{
    private const int PageSize = 15;
    private const int TargetStock = 20; // gợi ý nhập lên mức này cho hàng sắp hết

    private readonly IImportService _imports;
    private readonly ISupplierService _suppliers;
    private readonly IProductService _products;

    public ImportsController(IImportService imports, ISupplierService suppliers, IProductService products)
    {
        _imports = imports;
        _suppliers = suppliers;
        _products = products;
    }

    // GET: /Imports?status=&supplierId=&page=
    public async Task<IActionResult> Index(ImportStatus? status, int? supplierId, int page = 1)
    {
        ViewData["Status"] = status;
        ViewData["SupplierId"] = supplierId;
        ViewData["Counts"] = await _imports.CountByStatusAsync();
        ViewData["Suppliers"] = await _suppliers.GetAllAsync();
        ViewData["LowStock"] = await _imports.GetLowStockAsync();
        return View(await _imports.GetPagedAsync(status, supplierId, page, PageSize));
    }

    // GET: /Imports/Create?productId=5   hoặc   /Imports/Create?lowStock=true
    public async Task<IActionResult> Create(int? productId, bool lowStock = false)
    {
        var vm = await BuildFormAsync(new ImportCreateViewModel());

        if (lowStock)
        {
            foreach (var p in await _imports.GetLowStockAsync())
                vm.Lines.Add(new ImportLine { ProductId = p.Id, Quantity = Math.Max(1, TargetStock - p.Stock), UnitPrice = 0 });
        }
        else if (productId.HasValue && vm.Products.Any(p => p.Id == productId.Value))
        {
            vm.Lines.Add(new ImportLine { ProductId = productId.Value, Quantity = 10, UnitPrice = 0 });
        }

        if (vm.Lines.Count == 0)
            vm.Lines.Add(new ImportLine { Quantity = 1 });

        return View(vm);
    }

    // POST: /Imports/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ImportCreateViewModel vm)
    {
        vm.Lines = vm.Lines.Where(l => l.ProductId > 0).ToList();
        if (vm.Lines.Count == 0)
            ModelState.AddModelError(string.Empty, "Phiếu nhập phải có ít nhất 1 sản phẩm.");

        if (!ModelState.IsValid)
            return View(await BuildFormAsync(vm));

        var (result, id) = await _imports.CreateAsync(vm.SupplierId, vm.Notes, vm.Lines, vm.ReceiveNow);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return View(await BuildFormAsync(vm));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // GET: /Imports/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var import = await _imports.GetByIdAsync(id);
        if (import == null) return NotFound();
        return View(import);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id) => Done(await _imports.ApproveAsync(id), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id) => Done(await _imports.ReceiveAsync(id), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id) => Done(await _imports.CancelAsync(id), id);

    private IActionResult Done(ServiceResult result, int id)
    {
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task<ImportCreateViewModel> BuildFormAsync(ImportCreateViewModel vm)
    {
        vm.Suppliers = await _suppliers.GetAllAsync();
        var all = await _products.GetPagedAsync(new ProductFilter { PageSize = QueryableExtensions.MaxPageSize, SortBy = null });
        vm.Products = all.Items.ToList();
        return vm;
    }
}
