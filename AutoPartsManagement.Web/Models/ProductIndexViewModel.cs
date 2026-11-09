using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoPartsManagement.Web.Models;

public class ProductIndexViewModel
{
    public PagedResult<Product> Result { get; set; } = default!;
    public ProductFilter Filter { get; set; } = new();
    public ProductStats Stats { get; set; } = new();
    public SelectList Categories { get; set; } = default!;
    public SelectList Brands { get; set; } = default!;
}
