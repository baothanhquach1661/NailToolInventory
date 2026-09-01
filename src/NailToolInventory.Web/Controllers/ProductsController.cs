using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NailToolInventory.Data;
using NailToolInventory.Models;
using NailToolInventory.ViewModels;

namespace NailToolInventory.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _dbContext;


    public ProductsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync();

        return View(products);
    }


    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProductCreateViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var normalizedSku = model.Sku
            .Trim()
            .ToUpperInvariant();

        var skuAlreadyExists = await _dbContext.Products
            .AnyAsync(product => product.Sku == normalizedSku);

        if (skuAlreadyExists)
        {
            ModelState.AddModelError(
                nameof(model.Sku),
                "A product with this SKU already exists.");

            return View(model);
        }

        try
        {
            var product = new Product(
                sku: normalizedSku,
                name: model.Name,
                category: model.Category!.Value,
                costPrice: model.CostPrice,
                sellingPrice: model.SellingPrice,
                reorderLevel: model.ReorderLevel);

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Product {product.Sku} was created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                string.Empty,
                exception.Message);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(model.Sku),
                "Unable to save the product. The SKU may already exist.");
        }

        return View(model);
    }
}