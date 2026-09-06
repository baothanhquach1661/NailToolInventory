using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NailToolInventory.Data;
using NailToolInventory.Models;
using NailToolInventory.ViewModels;

namespace NailToolInventory.Controllers;

[Authorize(Roles = IdentitySeeder.AdminRole)]
public class InventoryLocationsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public InventoryLocationsController(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var locations =
            await _dbContext.InventoryLocations
                .AsNoTracking()
                .OrderBy(location =>
                    location.FulfillmentPriority)
                .ThenBy(location => location.Name)
                .ToListAsync();

        var inventorySummaries =
            await _dbContext.InventoryLevels
                .AsNoTracking()
                .GroupBy(level =>
                    level.InventoryLocationId)
                .Select(group => new
                {
                    LocationId = group.Key,
                    ProductCount = group.Count(),

                    QuantityOnHand = group.Sum(
                        level => level.QuantityOnHand),

                    ReservedQuantity = group.Sum(
                        level => level.ReservedQuantity)
                })
                .ToDictionaryAsync(
                    summary => summary.LocationId);

        var model = new InventoryLocationListViewModel();

        foreach (var location in locations)
        {
            inventorySummaries.TryGetValue(
                location.Id,
                out var summary);

            model.Locations.Add(
                new InventoryLocationListItemViewModel
                {
                    Id = location.Id,
                    Code = location.Code,
                    Name = location.Name,
                    Address = location.Address,
                    IsActive = location.IsActive,

                    CanFulfillOnlineOrders =
                        location.CanFulfillOnlineOrders,

                    FulfillmentPriority =
                        location.FulfillmentPriority,

                    ProductCount =
                        summary?.ProductCount ?? 0,

                    QuantityOnHand =
                        summary?.QuantityOnHand ?? 0,

                    ReservedQuantity =
                        summary?.ReservedQuantity ?? 0
                });
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(
            new CreateInventoryLocationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateInventoryLocationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedCode =
            model.Code.Trim().ToUpperInvariant();

        var codeExists =
            await _dbContext.InventoryLocations
                .AnyAsync(location =>
                    location.Code == normalizedCode);

        if (codeExists)
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "This location code already exists.");

            return View(model);
        }

        InventoryLocation location;

        try
        {
            location = new InventoryLocation(
                model.Code,
                model.Name,
                model.Address,
                model.CanFulfillOnlineOrders,
                model.FulfillmentPriority);
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
            _dbContext.InventoryLocations.Add(location);
            await _dbContext.SaveChangesAsync();

            var products =
                await _dbContext.Products
                    .AsNoTracking()
                    .Select(product => new
                    {
                        product.Id,
                        product.ReorderLevel
                    })
                    .ToListAsync();

            var inventoryLevels =
                products.Select(product =>
                    new InventoryLevel(
                        product.Id,
                        location.Id,
                        0,
                        product.ReorderLevel))
                .ToList();

            _dbContext.InventoryLevels.AddRange(
                inventoryLevels);

            await _dbContext.SaveChangesAsync();
            await databaseTransaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await databaseTransaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                "The location could not be created.");

            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Location {location.Code} was created.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var location =
            await _dbContext.InventoryLocations
                .AsNoTracking()
                .SingleOrDefaultAsync(location =>
                    location.Id == id);

        if (location is null)
        {
            return NotFound();
        }

        var model = new EditInventoryLocationViewModel
        {
            Id = location.Id,
            Code = location.Code,
            Name = location.Name,
            Address = location.Address,

            CanFulfillOnlineOrders =
                location.CanFulfillOnlineOrders,

            FulfillmentPriority =
                location.FulfillmentPriority
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditInventoryLocationViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var location =
            await _dbContext.InventoryLocations
                .SingleOrDefaultAsync(location =>
                    location.Id == id);

        if (location is null)
        {
            return NotFound();
        }

        model.Code = location.Code;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            location.UpdateDetails(
                model.Name,
                model.Address,
                model.CanFulfillOnlineOrders,
                model.FulfillmentPriority);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                string.Empty,
                exception.Message);

            return View(model);
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Location {location.Code} was updated.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var location =
            await _dbContext.InventoryLocations
                .SingleOrDefaultAsync(location =>
                    location.Id == id);

        if (location is null)
        {
            return NotFound();
        }

        if (location.IsActive)
        {
            var hasInventory =
                await _dbContext.InventoryLevels
                    .AnyAsync(level =>
                        level.InventoryLocationId == id &&
                        (level.QuantityOnHand > 0 ||
                         level.ReservedQuantity > 0));

            if (hasInventory)
            {
                TempData["ErrorMessage"] =
                    $"Location {location.Code} cannot be disabled " +
                    "because it still has inventory.";

                return RedirectToAction(nameof(Index));
            }

            var activeLocationCount =
                await _dbContext.InventoryLocations
                    .CountAsync(location =>
                        location.IsActive);

            if (activeLocationCount <= 1)
            {
                TempData["ErrorMessage"] =
                    "The last active inventory location " +
                    "cannot be disabled.";

                return RedirectToAction(nameof(Index));
            }

            location.Deactivate();

            TempData["SuccessMessage"] =
                $"Location {location.Code} was disabled.";
        }
        else
        {
            location.Activate();

            TempData["SuccessMessage"] =
                $"Location {location.Code} was enabled.";
        }

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


}