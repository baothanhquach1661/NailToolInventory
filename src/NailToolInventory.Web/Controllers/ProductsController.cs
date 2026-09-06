using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NailToolInventory.Data;
using NailToolInventory.Models;
using NailToolInventory.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace NailToolInventory.Controllers;


[Authorize(Roles = "Admin,Staff")]

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _dbContext;


    public ProductsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    [HttpGet]
    public async Task<IActionResult> Index(
    string? searchTerm,
    ProductCategory? category,
    bool lowStockOnly = false,
    bool includeInactive = false)
    {
        var query = _dbContext.Products
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(product => product.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = searchTerm.Trim();

            query = query.Where(product =>
                EF.Functions.Like(product.Sku, $"%{keyword}%") ||
                EF.Functions.Like(product.Name, $"%{keyword}%"));
        }

        if (category.HasValue)
        {
            query = query.Where(product =>
                product.Category == category.Value);
        }

        if (lowStockOnly)
        {
            query = query.Where(product =>
                product.QuantityOnHand <= product.ReorderLevel);
        }

        var products = await query
            .OrderBy(product => product.Name)
            .ToListAsync();

        var model = new ProductListViewModel
        {
            SearchTerm = searchTerm?.Trim(),
            Category = category,
            LowStockOnly = lowStockOnly,
            IncludeInactive = includeInactive,
            Products = products
        };

        return View(model);
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View(new ProductCreateViewModel());
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedSku =
            model.Sku.Trim().ToUpperInvariant();

        var skuAlreadyExists =
            await _dbContext.Products
                .AnyAsync(product =>
                    product.Sku == normalizedSku);

        if (skuAlreadyExists)
        {
            ModelState.AddModelError(
                nameof(model.Sku),
                "A product with this SKU already exists.");

            return View(model);
        }

        var locationIds =
            await _dbContext.InventoryLocations
                .AsNoTracking()
                .Select(location => location.Id)
                .ToListAsync();

        if (locationIds.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "At least one inventory location " +
                "is required before creating a product.");

            return View(model);
        }

        Product product;

        try
        {
            product = new Product(
                sku: normalizedSku,
                name: model.Name,
                category: model.Category!.Value,
                costPrice: model.CostPrice,
                sellingPrice: model.SellingPrice,
                reorderLevel: model.ReorderLevel);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                string.Empty,
                exception.Message);

            return View(model);
        }

        await using var databaseTransaction =
            await _dbContext.Database
                .BeginTransactionAsync();

        try
        {
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var inventoryLevels =
                locationIds.Select(locationId =>
                    new InventoryLevel(
                        productId: product.Id,
                        inventoryLocationId: locationId,
                        initialQuantity: 0,
                        reorderLevel: model.ReorderLevel))
                .ToList();

            _dbContext.InventoryLevels.AddRange(
                inventoryLevels);

            await _dbContext.SaveChangesAsync();
            await databaseTransaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Product {product.Sku} was created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            await databaseTransaction.RollbackAsync();

            ModelState.AddModelError(
                nameof(model.Sku),
                "Unable to save the product. " +
                "The SKU may already exist.");

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ReceiveStock(int id)
    {
        var product =
            await _dbContext.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(product =>
                    product.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (!product.IsActive)
        {
            TempData["ErrorMessage"] =
                "Stock cannot be received for an inactive product.";

            return RedirectToAction(nameof(Index));
        }

        var inventoryLevels =
            await _dbContext.InventoryLevels
                .AsNoTracking()
                .Include(level => level.Location)
                .Where(level =>
                    level.ProductId == id &&
                    level.Location.IsActive)
                .OrderBy(level =>
                    level.Location.FulfillmentPriority)
                .ThenBy(level => level.Location.Name)
                .ToListAsync();

        if (inventoryLevels.Count == 0)
        {
            TempData["ErrorMessage"] =
                "This product has no active inventory locations.";

            return RedirectToAction(nameof(Index));
        }

        var defaultLevel = inventoryLevels[0];

        var model = new ReceiveStockViewModel
        {
            ProductId = product.Id,
            ProductSku = product.Sku,
            ProductName = product.Name,

            InventoryLocationId =
                defaultLevel.InventoryLocationId,

            CurrentQuantity =
                defaultLevel.QuantityOnHand,

            Locations = inventoryLevels
                .Select(level => new SelectListItem
                {
                    Value =
                        level.InventoryLocationId.ToString(),

                    Text =
                        $"{level.Location.Code} - " +
                        $"{level.Location.Name} " +
                        $"(On hand: {level.QuantityOnHand})"
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceiveStock(
        ReceiveStockViewModel model)
    {
        var product =
            await _dbContext.Products
                .SingleOrDefaultAsync(product =>
                    product.Id == model.ProductId);

        if (product is null)
        {
            return NotFound();
        }

        var inventoryLevels =
            await _dbContext.InventoryLevels
                .Include(level => level.Location)
                .Where(level =>
                    level.ProductId == model.ProductId &&
                    level.Location.IsActive)
                .OrderBy(level =>
                    level.Location.FulfillmentPriority)
                .ThenBy(level => level.Location.Name)
                .ToListAsync();

        var inventoryLevel =
            inventoryLevels.SingleOrDefault(level =>
                level.InventoryLocationId ==
                model.InventoryLocationId);

        model.ProductSku = product.Sku;
        model.ProductName = product.Name;

        model.CurrentQuantity =
            inventoryLevel?.QuantityOnHand ?? 0;

        model.Locations = inventoryLevels
            .Select(level => new SelectListItem
            {
                Value =
                    level.InventoryLocationId.ToString(),

                Text =
                    $"{level.Location.Code} - " +
                    $"{level.Location.Name} " +
                    $"(On hand: {level.QuantityOnHand})"
            })
            .ToList();

        if (inventoryLevel is null)
        {
            ModelState.AddModelError(
                nameof(model.InventoryLocationId),
                "Please select an active inventory location.");
        }

        if (!product.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "Stock cannot be received for an inactive product.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var auditInfo = GetCurrentUserAuditInfo();

        var quantityBefore =
            inventoryLevel!.QuantityOnHand;

        try
        {
            // Keep the old total inventory synchronized
            // during the multi-location migration.
            product.ReceiveStock(model.Quantity);

            // Update the selected warehouse.
            inventoryLevel.ReceiveStock(model.Quantity);

            var transaction = new InventoryTransaction(
                productId: product.Id,
                type: InventoryTransactionType.Receipt,
                quantity: model.Quantity,
                quantityBefore: quantityBefore,
                quantityAfter:
                    inventoryLevel.QuantityOnHand,
                reference: model.Reference,
                notes: model.Notes,
                performedByUserId: auditInfo.UserId,
                performedByName: auditInfo.DisplayName,
                inventoryLocationId:
                    inventoryLevel.InventoryLocationId);

            _dbContext.InventoryTransactions.Add(
                transaction);

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Received {model.Quantity} units of " +
                $"{product.Sku} into " +
                $"{inventoryLevel.Location.Code}.";

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
    public async Task<IActionResult> IssueStock(int id)
    {
        var product =
            await _dbContext.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(product =>
                    product.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (!product.IsActive)
        {
            TempData["ErrorMessage"] =
                "Stock cannot be issued for an inactive product.";

            return RedirectToAction(nameof(Index));
        }

        var inventoryLevels =
            await _dbContext.InventoryLevels
                .AsNoTracking()
                .Include(level => level.Location)
                .Where(level =>
                    level.ProductId == id &&
                    level.Location.IsActive &&
                    level.QuantityOnHand >
                        level.ReservedQuantity)
                .OrderBy(level =>
                    level.Location.FulfillmentPriority)
                .ThenBy(level => level.Location.Name)
                .ToListAsync();

        var totalAvailable =
            inventoryLevels.Sum(level =>
                level.AvailableQuantity);

        var model = new IssueStockViewModel
        {
            ProductId = product.Id,
            ProductSku = product.Sku,
            ProductName = product.Name,
            CurrentQuantity = totalAvailable,

            InventoryLocationId =
                inventoryLevels.FirstOrDefault()?
                    .InventoryLocationId ?? 0,

            Locations = inventoryLevels
                .Select(level => new SelectListItem
                {
                    Value =
                        level.InventoryLocationId.ToString(),

                    Text =
                        $"{level.Location.Code} - " +
                        $"{level.Location.Name} " +
                        $"(Available: {level.AvailableQuantity})"
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueStock(
        IssueStockViewModel model)
    {
        var product =
            await _dbContext.Products
                .SingleOrDefaultAsync(product =>
                    product.Id == model.ProductId);

        if (product is null)
        {
            return NotFound();
        }

        var activeLevels =
            await _dbContext.InventoryLevels
                .Include(level => level.Location)
                .Where(level =>
                    level.ProductId == model.ProductId &&
                    level.Location.IsActive)
                .OrderBy(level =>
                    level.Location.FulfillmentPriority)
                .ThenBy(level => level.Location.Name)
                .ToListAsync();

        var inventoryLevel =
            activeLevels.SingleOrDefault(level =>
                level.InventoryLocationId ==
                model.InventoryLocationId);

        var availableLevels =
            activeLevels
                .Where(level =>
                    level.AvailableQuantity > 0)
                .ToList();

        model.ProductSku = product.Sku;
        model.ProductName = product.Name;

        model.CurrentQuantity =
            availableLevels.Sum(level =>
                level.AvailableQuantity);

        model.Locations = availableLevels
            .Select(level => new SelectListItem
            {
                Value =
                    level.InventoryLocationId.ToString(),

                Text =
                    $"{level.Location.Code} - " +
                    $"{level.Location.Name} " +
                    $"(Available: {level.AvailableQuantity})"
            })
            .ToList();

        if (inventoryLevel is null)
        {
            ModelState.AddModelError(
                nameof(model.InventoryLocationId),
                "Please select an active inventory location.");
        }

        if (!product.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "Stock cannot be issued for an inactive product.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var auditInfo = GetCurrentUserAuditInfo();

        var quantityBefore =
            inventoryLevel!.QuantityOnHand;

        try
        {
            // Subtract from the selected warehouse.
            inventoryLevel.IssueStock(model.Quantity);

            // Keep the old total synchronized temporarily.
            product.IssueStock(model.Quantity);

            var transaction = new InventoryTransaction(
                productId: product.Id,
                type: InventoryTransactionType.Issue,
                quantity: model.Quantity,
                quantityBefore: quantityBefore,
                quantityAfter:
                    inventoryLevel.QuantityOnHand,
                reference: model.Reference,
                notes: model.Notes,
                performedByUserId: auditInfo.UserId,
                performedByName: auditInfo.DisplayName,
                inventoryLocationId:
                    inventoryLevel.InventoryLocationId);

            _dbContext.InventoryTransactions.Add(
                transaction);

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Issued {model.Quantity} units of " +
                $"{product.Sku} from " +
                $"{inventoryLevel.Location.Code}.";

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
            .Include(transaction => transaction.Location)
            .Where(transaction => transaction.ProductId == id)
            .OrderByDescending(
                transaction => transaction.CreatedAtUtc)
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

    [HttpGet]
    public async Task<IActionResult> InventoryByLocation(int id)
    {
        var product =
            await _dbContext.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(product =>
                    product.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        var inventoryLevels =
            await _dbContext.InventoryLevels
                .AsNoTracking()
                .Include(level => level.Location)
                .Where(level =>
                    level.ProductId == id)
                .OrderByDescending(level =>
                    level.Location.IsActive)
                .ThenBy(level =>
                    level.Location.FulfillmentPriority)
                .ThenBy(level => level.Location.Name)
                .ToListAsync();

        var model = new ProductLocationInventoryViewModel
        {
            ProductId = product.Id,
            ProductSku = product.Sku,
            ProductName = product.Name,

            TotalQuantityOnHand =
                inventoryLevels.Sum(level =>
                    level.QuantityOnHand),

            TotalReservedQuantity =
                inventoryLevels.Sum(level =>
                    level.ReservedQuantity),

            Locations = inventoryLevels
                .Select(level =>
                    new ProductLocationInventoryItemViewModel
                    {
                        InventoryLevelId = level.Id,

                        InventoryLocationId =
                            level.InventoryLocationId,

                        LocationCode =
                            level.Location.Code,

                        LocationName =
                            level.Location.Name,

                        IsLocationActive =
                            level.Location.IsActive,

                        QuantityOnHand =
                            level.QuantityOnHand,

                        ReservedQuantity =
                            level.ReservedQuantity,

                        AvailableQuantity =
                            level.AvailableQuantity,

                        ReorderLevel =
                            level.ReorderLevel,

                        IsLowStock =
                            level.IsLowStock()
                    })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReorderLevel(
        UpdateReorderLevelViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "Reorder level must be zero or greater.";

            return RedirectToAction(
                nameof(InventoryByLocation),
                new { id = model.ProductId });
        }

        var inventoryLevel =
            await _dbContext.InventoryLevels
                .Include(level => level.Location)
                .Include(level => level.Product)
                .SingleOrDefaultAsync(level =>
                    level.Id == model.InventoryLevelId &&
                    level.ProductId == model.ProductId);

        if (inventoryLevel is null)
        {
            return NotFound();
        }

        try
        {
            inventoryLevel.SetReorderLevel(
                model.ReorderLevel);

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Reorder level for " +
                $"{inventoryLevel.Product.Sku} at " +
                $"{inventoryLevel.Location.Code} was updated.";
        }
        catch (ArgumentException exception)
        {
            TempData["ErrorMessage"] =
                exception.Message;
        }

        return RedirectToAction(
            nameof(InventoryByLocation),
            new { id = model.ProductId });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id);

        if (product is null)
            return NotFound();

        var model = new ProductEditViewModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Category = product.Category,
            CostPrice = product.CostPrice,
            SellingPrice = product.SellingPrice,
            ReorderLevel = product.ReorderLevel,
            IsActive = product.IsActive
        };

        return View(model);
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ProductEditViewModel model)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(
                product => product.Id == model.Id);

        if (product is null)
            return NotFound();

        // SKU luôn được lấy từ database và không cho sửa.
        model.Sku = product.Sku;

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            product.UpdateDetails(
                name: model.Name,
                category: model.Category!.Value,
                costPrice: model.CostPrice,
                sellingPrice: model.SellingPrice,
                reorderLevel: model.ReorderLevel);

            if (model.IsActive)
                product.Activate();
            else
                product.Deactivate();

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Product {product.Sku} was updated successfully.";

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
                string.Empty,
                "Unable to update the product.");
        }

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdjustStock(
    int id,
    int? inventoryLocationId = null)
    {
        var product =
            await _dbContext.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(product =>
                    product.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (!product.IsActive)
        {
            TempData["ErrorMessage"] =
                "Inventory cannot be adjusted " +
                "for an inactive product.";

            return RedirectToAction(nameof(Index));
        }

        var inventoryLevels =
            await _dbContext.InventoryLevels
                .AsNoTracking()
                .Include(level => level.Location)
                .Where(level =>
                    level.ProductId == id &&
                    level.Location.IsActive)
                .OrderBy(level =>
                    level.Location.FulfillmentPriority)
                .ThenBy(level => level.Location.Name)
                .ToListAsync();

        if (inventoryLevels.Count == 0)
        {
            TempData["ErrorMessage"] =
                "This product has no active inventory locations.";

            return RedirectToAction(nameof(Index));
        }

        var selectedLevel =
            inventoryLocationId.HasValue
                ? inventoryLevels.SingleOrDefault(level =>
                    level.InventoryLocationId ==
                    inventoryLocationId.Value)
                : null;

        selectedLevel ??= inventoryLevels[0];

        var model = new AdjustStockViewModel
        {
            ProductId = product.Id,
            Sku = product.Sku,
            ProductName = product.Name,

            InventoryLocationId =
                selectedLevel.InventoryLocationId,

            SelectedLocationName =
                $"{selectedLevel.Location.Code} - " +
                selectedLevel.Location.Name,

            CurrentQuantity =
                selectedLevel.QuantityOnHand,

            ReservedQuantity =
                selectedLevel.ReservedQuantity,

            NewQuantity =
                selectedLevel.QuantityOnHand,

            Locations = inventoryLevels
                .Select(level => new SelectListItem
                {
                    Value =
                        level.InventoryLocationId.ToString(),

                    Text =
                        $"{level.Location.Code} - " +
                        $"{level.Location.Name} " +
                        $"(On hand: {level.QuantityOnHand}, " +
                        $"Reserved: {level.ReservedQuantity})"
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(
        AdjustStockViewModel model)
    {
        var product =
            await _dbContext.Products
                .SingleOrDefaultAsync(product =>
                    product.Id == model.ProductId);

        if (product is null)
        {
            return NotFound();
        }

        var inventoryLevels =
            await _dbContext.InventoryLevels
                .Include(level => level.Location)
                .Where(level =>
                    level.ProductId == model.ProductId &&
                    level.Location.IsActive)
                .OrderBy(level =>
                    level.Location.FulfillmentPriority)
                .ThenBy(level => level.Location.Name)
                .ToListAsync();

        var inventoryLevel =
            inventoryLevels.SingleOrDefault(level =>
                level.InventoryLocationId ==
                model.InventoryLocationId);

        model.Sku = product.Sku;
        model.ProductName = product.Name;

        model.Locations = inventoryLevels
            .Select(level => new SelectListItem
            {
                Value =
                    level.InventoryLocationId.ToString(),

                Text =
                    $"{level.Location.Code} - " +
                    $"{level.Location.Name} " +
                    $"(On hand: {level.QuantityOnHand}, " +
                    $"Reserved: {level.ReservedQuantity})"
            })
            .ToList();

        if (inventoryLevel is null)
        {
            model.CurrentQuantity = 0;
            model.ReservedQuantity = 0;
            model.SelectedLocationName = string.Empty;

            ModelState.AddModelError(
                nameof(model.InventoryLocationId),
                "Please select an active inventory location.");
        }
        else
        {
            model.CurrentQuantity =
                inventoryLevel.QuantityOnHand;

            model.ReservedQuantity =
                inventoryLevel.ReservedQuantity;

            model.SelectedLocationName =
                $"{inventoryLevel.Location.Code} - " +
                inventoryLevel.Location.Name;

            if (model.NewQuantity ==
                inventoryLevel.QuantityOnHand)
            {
                ModelState.AddModelError(
                    nameof(model.NewQuantity),
                    "Counted quantity is the same " +
                    "as current inventory.");
            }

            if (model.NewQuantity <
                inventoryLevel.ReservedQuantity)
            {
                ModelState.AddModelError(
                    nameof(model.NewQuantity),
                    "Counted quantity cannot be lower " +
                    "than the reserved quantity.");
            }
        }

        if (!product.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "Inventory cannot be adjusted " +
                "for an inactive product.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var auditInfo = GetCurrentUserAuditInfo();

        var quantityBefore =
            inventoryLevel!.QuantityOnHand;

        var quantityDifference =
            model.NewQuantity - quantityBefore;

        var newTotalQuantity =
            product.QuantityOnHand +
            quantityDifference;

        try
        {
            inventoryLevel.AdjustStock(
                model.NewQuantity);

            // Keep Product.QuantityOnHand synchronized
            // while the old total field still exists.
            product.AdjustStock(
                newTotalQuantity);

            var transaction = new InventoryTransaction(
                productId: product.Id,
                type: InventoryTransactionType.Adjustment,
                quantity: Math.Abs(quantityDifference),
                quantityBefore: quantityBefore,
                quantityAfter:
                    inventoryLevel.QuantityOnHand,
                reference: model.Reference,
                notes: model.Notes,
                performedByUserId: auditInfo.UserId,
                performedByName: auditInfo.DisplayName,
                inventoryLocationId:
                    inventoryLevel.InventoryLocationId);

            _dbContext.InventoryTransactions.Add(
                transaction);

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Inventory for {product.Sku} at " +
                $"{inventoryLevel.Location.Code} " +
                "was adjusted successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                nameof(model.NewQuantity),
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(
                nameof(model.NewQuantity),
                exception.Message);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Unable to save the inventory adjustment.");
        }

        return View(model);
    }


    private (string UserId, string DisplayName)
    GetCurrentUserAuditInfo()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "The current user could not be identified.");
        }

        var displayName = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = userId;
        }

        return (userId, displayName);
    }


}
