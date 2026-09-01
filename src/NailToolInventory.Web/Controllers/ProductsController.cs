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


    [HttpGet]
    public async Task<IActionResult> ReceiveStock(int id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id);

        if (product is null)
            return NotFound();

        var model = new ReceiveStockViewModel
        {
            ProductId = product.Id,
            ProductSku = product.Sku,
            ProductName = product.Name,
            CurrentQuantity = product.QuantityOnHand
        };

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceiveStock(
        ReceiveStockViewModel model)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(
                product => product.Id == model.ProductId);

        if (product is null)
            return NotFound();

        // Những giá trị này được lấy lại từ database,
        // không tin dữ liệu do trình duyệt gửi lên.
        model.ProductSku = product.Sku;
        model.ProductName = product.Name;
        model.CurrentQuantity = product.QuantityOnHand;

        if (!ModelState.IsValid)
            return View(model);

        var quantityBefore = product.QuantityOnHand;

        try
        {
            product.ReceiveStock(model.Quantity);

            var transaction = new InventoryTransaction(
                productId: product.Id,
                type: InventoryTransactionType.Receipt,
                quantity: model.Quantity,
                quantityBefore: quantityBefore,
                quantityAfter: product.QuantityOnHand,
                reference: model.Reference,
                notes: model.Notes);

            _dbContext.InventoryTransactions.Add(transaction);

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Received {model.Quantity} units of {product.Sku}.";

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                nameof(model.Quantity),
                exception.Message);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Unable to save the inventory transaction.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> IssueStock(int id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id);

        if (product is null)
            return NotFound();

        var model = new IssueStockViewModel
        {
            ProductId = product.Id,
            ProductSku = product.Sku,
            ProductName = product.Name,
            CurrentQuantity = product.QuantityOnHand
        };

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueStock(
        IssueStockViewModel model)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(
                product => product.Id == model.ProductId);

        if (product is null)
            return NotFound();

        model.ProductSku = product.Sku;
        model.ProductName = product.Name;
        model.CurrentQuantity = product.QuantityOnHand;

        if (!ModelState.IsValid)
            return View(model);

        var quantityBefore = product.QuantityOnHand;

        try
        {
            product.IssueStock(model.Quantity);

            var transaction = new InventoryTransaction(
                productId: product.Id,
                type: InventoryTransactionType.Issue,
                quantity: model.Quantity,
                quantityBefore: quantityBefore,
                quantityAfter: product.QuantityOnHand,
                reference: model.Reference,
                notes: model.Notes);

            _dbContext.InventoryTransactions.Add(transaction);

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Issued {model.Quantity} units of {product.Sku}.";

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                nameof(model.Quantity),
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(
                nameof(model.Quantity),
                exception.Message);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Unable to save the inventory transaction.");
        }

        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> History(int id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id);

        if (product is null)
            return NotFound();

        var transactions = await _dbContext.InventoryTransactions
            .AsNoTracking()
            .Where(transaction => transaction.ProductId == id)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync();

        var model = new ProductHistoryViewModel
        {
            ProductId = product.Id,
            ProductSku = product.Sku,
            ProductName = product.Name,
            CurrentQuantity = product.QuantityOnHand,
            Transactions = transactions
        };

        return View(model);
    }
}