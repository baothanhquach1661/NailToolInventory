using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NailToolInventory.Data;
using NailToolInventory.Models;
using NailToolInventory.ViewModels;

namespace NailToolInventory.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class StockTransfersController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;


    public StockTransfersController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }


    public async Task<IActionResult> Index()
    {
        var transfers =
            await _dbContext.StockTransfers
                .AsNoTracking()
                .OrderByDescending(
                    transfer => transfer.CreatedAtUtc)
                .ThenByDescending(transfer => transfer.Id)
                .Select(transfer =>
                    new StockTransferListItemViewModel
                    {
                        Id = transfer.Id,

                        TransferNumber =
                            transfer.TransferNumber,

                        SourceLocationName =
                            transfer.SourceLocation.Code +
                            " — " +
                            transfer.SourceLocation.Name,

                        DestinationLocationName =
                            transfer.DestinationLocation.Code +
                            " — " +
                            transfer.DestinationLocation.Name,

                        Status = transfer.Status,

                        ProductCount =
                            transfer.Items.Count,

                        TotalQuantity =
                            transfer.Items
                                .Select(item =>
                                    (int?)item.Quantity)
                                .Sum() ?? 0,

                        TotalReceived =
                            transfer.Items
                                .Select(item =>
                                    (int?)item.QuantityReceived)
                                .Sum() ?? 0,

                        CreatedByName =
                            transfer.CreatedByName,

                        CreatedAtUtc =
                            transfer.CreatedAtUtc
                    })
                .ToListAsync();

        var model = new StockTransferListViewModel
        {
            Transfers = transfers
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var transfer =
            await _dbContext.StockTransfers
                .AsNoTracking()
                .Include(transfer =>
                    transfer.SourceLocation)
                .Include(transfer =>
                    transfer.DestinationLocation)
                .Include(transfer => transfer.Items)
                    .ThenInclude(item => item.Product)
                .SingleOrDefaultAsync(
                    transfer => transfer.Id == id);

        if (transfer is null)
        {
            return NotFound();
        }

        var model = new StockTransferDetailsViewModel
        {
            Id = transfer.Id,

            TransferNumber =
                transfer.TransferNumber,

            SourceLocationName =
                transfer.SourceLocation.Code +
                " — " +
                transfer.SourceLocation.Name,

            DestinationLocationName =
                transfer.DestinationLocation.Code +
                " — " +
                transfer.DestinationLocation.Name,

            Status = transfer.Status,
            Reference = transfer.Reference,
            Notes = transfer.Notes,
            CreatedByName = transfer.CreatedByName,
            CreatedAtUtc = transfer.CreatedAtUtc,
            ShippedAtUtc = transfer.ShippedAtUtc,
            CompletedAtUtc = transfer.CompletedAtUtc,
            CancelledAtUtc = transfer.CancelledAtUtc,

            Items = transfer.Items
                .OrderBy(item => item.Product.Sku)
                .Select(item =>
                    new StockTransferDetailsItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductSku = item.Product.Sku,
                        ProductName = item.Product.Name,
                        Quantity = item.Quantity,
                        QuantityReceived =
                            item.QuantityReceived
                    })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Ship(int id)
    {
        var transfer =
            await _dbContext.StockTransfers
                .Include(transfer =>
                    transfer.SourceLocation)
                .Include(transfer =>
                    transfer.DestinationLocation)
                .Include(transfer => transfer.Items)
                    .ThenInclude(item => item.Product)
                .SingleOrDefaultAsync(
                    transfer => transfer.Id == id);

        if (transfer is null)
        {
            return NotFound();
        }

        if (transfer.Status !=
            StockTransferStatus.Draft)
        {
            TempData["ErrorMessage"] =
                "Only draft transfers can be shipped.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        if (transfer.Items.Count == 0)
        {
            TempData["ErrorMessage"] =
                "The transfer does not contain any products.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        var productIds =
            transfer.Items
                .Select(item => item.ProductId)
                .ToList();

        var sourceLevels =
            await _dbContext.InventoryLevels
                .Include(level => level.Product)
                .Where(level =>
                    level.InventoryLocationId ==
                    transfer.SourceLocationId &&
                    productIds.Contains(level.ProductId))
                .ToDictionaryAsync(
                    level => level.ProductId);


        foreach (var item in transfer.Items)
        {
            if (!sourceLevels.TryGetValue(
                    item.ProductId,
                    out var inventoryLevel))
            {
                TempData["ErrorMessage"] =
                    $"{item.Product.Sku} has no inventory " +
                    "record at the source location.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (item.Quantity >
                inventoryLevel.AvailableQuantity)
            {
                TempData["ErrorMessage"] =
                    $"Cannot ship {item.Product.Sku}. " +
                    $"Only {inventoryLevel.AvailableQuantity} " +
                    "units are currently available.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }


        var currentUser =
            await _userManager.GetUserAsync(User);

        var performedByName =
            currentUser?.FullName
            ?? currentUser?.Email
            ?? User.Identity?.Name;


        try
        {
            transfer.MarkInTransit();

            foreach (var item in transfer.Items)
            {
                var inventoryLevel =
                    sourceLevels[item.ProductId];

                var quantityBefore =
                    inventoryLevel.QuantityOnHand;

                inventoryLevel.IssueStock(
                    item.Quantity);

                // Đồng bộ cột tồn kho tổng cũ.
                inventoryLevel.Product.IssueStock(
                    item.Quantity);

                var inventoryTransaction =
                    new InventoryTransaction(
                        productId: item.ProductId,
                        type:
                            InventoryTransactionType.TransferOut,
                        quantity: item.Quantity,
                        quantityBefore: quantityBefore,
                        quantityAfter:
                            inventoryLevel.QuantityOnHand,
                        reference:
                            transfer.TransferNumber,
                        notes:
                            $"Transfer from " +
                            $"{transfer.SourceLocation.Code} " +
                            $"to " +
                            $"{transfer.DestinationLocation.Code}.",
                        performedByUserId:
                            currentUser?.Id,
                        performedByName:
                            performedByName,
                        inventoryLocationId:
                            transfer.SourceLocationId,
                        stockTransferId:
                            transfer.Id);

                _dbContext.InventoryTransactions.Add(
                    inventoryTransaction);
            }

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Transfer {transfer.TransferNumber} " +
                "is now in transit.";
        }
        catch (ArgumentException exception)
        {
            TempData["ErrorMessage"] =
                exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] =
                exception.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "Unable to ship the stock transfer.";
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Receive(int id)
    {
        var transfer =
            await _dbContext.StockTransfers
                .Include(transfer =>
                    transfer.SourceLocation)
                .Include(transfer =>
                    transfer.DestinationLocation)
                .Include(transfer => transfer.Items)
                    .ThenInclude(item => item.Product)
                .SingleOrDefaultAsync(
                    transfer => transfer.Id == id);

        if (transfer is null)
        {
            return NotFound();
        }

        if (transfer.Status !=
                StockTransferStatus.InTransit &&
            transfer.Status !=
                StockTransferStatus.PartiallyReceived)
        {
            TempData["ErrorMessage"] =
                "Only transfers in transit can be received.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        var remainingItems =
            transfer.Items
                .Where(item =>
                    item.RemainingQuantity > 0)
                .ToList();

        if (remainingItems.Count == 0)
        {
            TempData["ErrorMessage"] =
                "This transfer has no remaining products to receive.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        var productIds =
            remainingItems
                .Select(item => item.ProductId)
                .ToList();

        var destinationLevels =
            await _dbContext.InventoryLevels
                .Include(level => level.Product)
                .Where(level =>
                    level.InventoryLocationId ==
                    transfer.DestinationLocationId &&
                    productIds.Contains(level.ProductId))
                .ToDictionaryAsync(
                    level => level.ProductId);


        foreach (var item in remainingItems)
        {
            if (!destinationLevels.ContainsKey(
                    item.ProductId))
            {
                TempData["ErrorMessage"] =
                    $"{item.Product.Sku} has no inventory " +
                    "record at the destination location.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }


        var currentUser =
            await _userManager.GetUserAsync(User);

        var performedByName =
            currentUser?.FullName
            ?? currentUser?.Email
            ?? User.Identity?.Name;


        try
        {
            foreach (var item in remainingItems)
            {
                var quantityToReceive =
                    item.RemainingQuantity;

                var inventoryLevel =
                    destinationLevels[item.ProductId];

                var quantityBefore =
                    inventoryLevel.QuantityOnHand;


                inventoryLevel.ReceiveStock(
                    quantityToReceive);

                // Đồng bộ lại cột tồn kho tổng cũ.
                inventoryLevel.Product.ReceiveStock(
                    quantityToReceive);

                transfer.RecordReceipt(
                    item.ProductId,
                    quantityToReceive);


                var inventoryTransaction =
                    new InventoryTransaction(
                        productId: item.ProductId,
                        type:
                            InventoryTransactionType.TransferIn,
                        quantity: quantityToReceive,
                        quantityBefore: quantityBefore,
                        quantityAfter:
                            inventoryLevel.QuantityOnHand,
                        reference:
                            transfer.TransferNumber,
                        notes:
                            $"Transfer received from " +
                            $"{transfer.SourceLocation.Code} " +
                            $"into " +
                            $"{transfer.DestinationLocation.Code}.",
                        performedByUserId:
                            currentUser?.Id,
                        performedByName:
                            performedByName,
                        inventoryLocationId:
                            transfer.DestinationLocationId,
                        stockTransferId:
                            transfer.Id);

                _dbContext.InventoryTransactions.Add(
                    inventoryTransaction);
            }

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Transfer {transfer.TransferNumber} " +
                "was received successfully.";
        }
        catch (ArgumentException exception)
        {
            TempData["ErrorMessage"] =
                exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] =
                exception.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "Unable to receive the stock transfer.";
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(int id)
    {
        var transfer =
            await _dbContext.StockTransfers
                .SingleOrDefaultAsync(
                    transfer => transfer.Id == id);

        if (transfer is null)
        {
            return NotFound();
        }

        if (transfer.Status !=
            StockTransferStatus.Draft)
        {
            TempData["ErrorMessage"] =
                "Only draft transfers can be cancelled.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        try
        {
            transfer.Cancel();

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Transfer {transfer.TransferNumber} " +
                "was cancelled.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] =
                exception.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "Unable to cancel the stock transfer.";
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var model = new CreateStockTransferViewModel();

        await PopulateSelectionsAsync(model);

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        CreateStockTransferViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectionsAsync(model);

            return View(model);
        }

        var sourceLocationId =
            model.SourceLocationId!.Value;

        var destinationLocationId =
            model.DestinationLocationId!.Value;


        var selectedLocationIds = new[]
        {
            sourceLocationId,
            destinationLocationId
        };

        var activeLocationIds =
            await _dbContext.InventoryLocations
                .AsNoTracking()
                .Where(location =>
                    location.IsActive &&
                    selectedLocationIds.Contains(location.Id))
                .Select(location => location.Id)
                .ToListAsync();

        if (!activeLocationIds.Contains(sourceLocationId))
        {
            ModelState.AddModelError(
                nameof(model.SourceLocationId),
                "The selected source location is unavailable.");
        }

        if (!activeLocationIds.Contains(destinationLocationId))
        {
            ModelState.AddModelError(
                nameof(model.DestinationLocationId),
                "The selected destination location is unavailable.");
        }


        var duplicateProducts =
            model.Items
                .Where(item => item.ProductId.HasValue)
                .GroupBy(item => item.ProductId!.Value)
                .Any(group => group.Count() > 1);

        if (duplicateProducts)
        {
            ModelState.AddModelError(
                nameof(model.Items),
                "The same product cannot be added more than once.");
        }


        var productIds =
            model.Items
                .Where(item => item.ProductId.HasValue)
                .Select(item => item.ProductId!.Value)
                .Distinct()
                .ToList();

        var activeProductIds =
            await _dbContext.Products
                .AsNoTracking()
                .Where(product =>
                    product.IsActive &&
                    productIds.Contains(product.Id))
                .Select(product => product.Id)
                .ToListAsync();

        for (var index = 0;
             index < model.Items.Count;
             index++)
        {
            var item = model.Items[index];

            if (item.ProductId.HasValue &&
                !activeProductIds.Contains(
                    item.ProductId.Value))
            {
                ModelState.AddModelError(
                    $"Items[{index}].ProductId",
                    "The selected product is unavailable.");
            }
        }


        var sourceLevels =
            await _dbContext.InventoryLevels
                .AsNoTracking()
                .Where(level =>
                    level.InventoryLocationId ==
                    sourceLocationId &&
                    productIds.Contains(level.ProductId))
                .ToDictionaryAsync(
                    level => level.ProductId);

        for (var index = 0;
             index < model.Items.Count;
             index++)
        {
            var item = model.Items[index];

            if (!item.ProductId.HasValue)
            {
                continue;
            }

            if (!sourceLevels.TryGetValue(
                    item.ProductId.Value,
                    out var inventoryLevel))
            {
                ModelState.AddModelError(
                    $"Items[{index}].Quantity",
                    "This product has no inventory at the source location.");

                continue;
            }

            if (item.Quantity >
                inventoryLevel.AvailableQuantity)
            {
                ModelState.AddModelError(
                    $"Items[{index}].Quantity",
                    $"Only {inventoryLevel.AvailableQuantity} units are available at the source location.");
            }
        }


        if (!ModelState.IsValid)
        {
            await PopulateSelectionsAsync(model);

            return View(model);
        }


        var currentUser =
            await _userManager.GetUserAsync(User);

        var createdByName =
            currentUser?.FullName
            ?? currentUser?.Email
            ?? User.Identity?.Name;


        var transfer = new StockTransfer(
            transferNumber: GenerateTransferNumber(),
            sourceLocationId: sourceLocationId,
            destinationLocationId: destinationLocationId,
            reference: model.Reference,
            notes: model.Notes,
            createdByUserId: currentUser?.Id,
            createdByName: createdByName);

        foreach (var item in model.Items)
        {
            transfer.AddItem(
                item.ProductId!.Value,
                item.Quantity);
        }

        _dbContext.StockTransfers.Add(transfer);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Transfer {transfer.TransferNumber} was created.";

        return RedirectToAction(nameof(Index));
    }


    private async Task PopulateSelectionsAsync(
        CreateStockTransferViewModel model)
    {
        var locations =
            await _dbContext.InventoryLocations
                .AsNoTracking()
                .Where(location => location.IsActive)
                .OrderBy(location =>
                    location.FulfillmentPriority)
                .ThenBy(location => location.Code)
                .Select(location => new
                {
                    location.Id,
                    location.Code,
                    location.Name
                })
                .ToListAsync();

        model.Locations =
            locations
                .Select(location =>
                    new SelectListItem
                    {
                        Value = location.Id.ToString(),
                        Text =
                            location.Code +
                            " — " +
                            location.Name
                    })
                .ToList();


        var products =
            await _dbContext.Products
                .AsNoTracking()
                .Where(product => product.IsActive)
                .OrderBy(product => product.Sku)
                .Select(product => new
                {
                    product.Id,
                    product.Sku,
                    product.Name
                })
                .ToListAsync();

        model.Products =
            products
                .Select(product =>
                    new SelectListItem
                    {
                        Value = product.Id.ToString(),
                        Text =
                            product.Sku +
                            " — " +
                            product.Name
                    })
                .ToList();
    }


    private static string GenerateTransferNumber()
    {
        var randomPart =
            Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

        return
            $"TR-{DateTime.UtcNow:yyyyMMdd}-{randomPart}";
    }
}
