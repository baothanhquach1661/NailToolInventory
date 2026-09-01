using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NailToolInventory.Data;
using NailToolInventory.Models;
using NailToolInventory.ViewModels;

namespace NailToolInventory.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _dbContext;


    public HomeController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<IActionResult> Index()
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .ToListAsync();

        var recentTransactions =
            await _dbContext.InventoryTransactions
                .AsNoTracking()
                .Include(transaction => transaction.Product)
                .OrderByDescending(
                    transaction => transaction.CreatedAtUtc)
                .ThenByDescending(transaction => transaction.Id)
                .Take(5)
                .ToListAsync();

        var model = new DashboardViewModel
        {
            TotalProducts = products.Count,

            TotalUnitsOnHand = products.Sum(
                product => product.QuantityOnHand),

            LowStockProducts = products.Count(
                product =>
                    product.IsActive &&
                    product.IsLowStock()),

            InventoryCostValue = products.Sum(
                product =>
                    product.CostPrice *
                    product.QuantityOnHand),

            RecentTransactions = recentTransactions
        };

        return View(model);
    }


    public IActionResult Privacy()
    {
        return View();
    }


    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId =
                Activity.Current?.Id ??
                HttpContext.TraceIdentifier
        });
    }
}