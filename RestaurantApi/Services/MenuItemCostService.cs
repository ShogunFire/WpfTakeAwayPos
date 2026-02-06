using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;

namespace RestaurantApi.Services;

public interface IMenuItemCostService
{
    /// <summary>
    /// Calculates the COGS for a specific menu item based on current component costs
    /// </summary>
    Task<decimal> CalculateMenuItemCOGSAsync(Guid menuItemId);

    /// <summary>
    /// Updates a menu item's COGS and creates a history record
    /// </summary>
    Task UpdateMenuItemCOGSAsync(Guid menuItemId);

    /// <summary>
    /// Recalculates COGS for all menu items that use a given ingredient
    /// </summary>
    Task RecalculateMenuItemsCOGSByIngredientAsync(Guid inventoryItemId);

    /// <summary>
    /// Creates a gross profit history snapshot for a menu item
    /// </summary>
    Task CreateGrossProfitHistoryAsync(Guid menuItemId);
}

public class MenuItemCostService : IMenuItemCostService
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemComponentRepository _componentRepository;
    private readonly IInventoryItemRepository _inventoryRepository;
    private readonly IMenuItemGrossProfitHistoryRepository _historyRepository;
    private readonly ILogger<MenuItemCostService> _logger;

    public MenuItemCostService(
        IMenuItemRepository menuItemRepository,
        IMenuItemComponentRepository componentRepository,
        IInventoryItemRepository inventoryRepository,
        IMenuItemGrossProfitHistoryRepository historyRepository,
        ILogger<MenuItemCostService> logger)
    {
        _menuItemRepository = menuItemRepository;
        _componentRepository = componentRepository;
        _inventoryRepository = inventoryRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task<decimal> CalculateMenuItemCOGSAsync(Guid menuItemId)
    {
        var components = await _componentRepository.GetByMenuItemIdAsync(menuItemId);
        decimal totalCOGS = 0;

        foreach (var component in components)
        {
            var inventoryItem = await _inventoryRepository.GetByIdAsync(component.InventoryItemId);
            if (inventoryItem != null)
            {
                // COGS = Quantity × UnitCost
                totalCOGS += component.Quantity * inventoryItem.CurrentUnitCost;
            }
        }

        return totalCOGS;
    }

    public async Task UpdateMenuItemCOGSAsync(Guid menuItemId)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId);
        if (menuItem == null)
        {
            _logger.LogWarning("MenuItem with ID {MenuItemId} not found", menuItemId);
            return;
        }

        // Calculate new COGS
        decimal newCOGS = await CalculateMenuItemCOGSAsync(menuItemId);

        // Update only if COGS changed
        if (Math.Abs(menuItem.CurrentCOGS - newCOGS) > 0.0001m)
        {
            menuItem.CurrentCOGS = newCOGS;
            menuItem.LastCOGSUpdate = DateTime.UtcNow;
            menuItem.UpdatedAt = DateTime.UtcNow;

            await _menuItemRepository.UpdateAsync(menuItem);

            _logger.LogInformation(
                "Updated MenuItem COGS: {MenuItemName} (ID: {MenuItemId}) - New COGS: ${NewCOGS:F2}",
                menuItem.Name, menuItemId, newCOGS);

            // Create history record
            await CreateGrossProfitHistoryAsync(menuItemId);
        }
    }

    public async Task RecalculateMenuItemsCOGSByIngredientAsync(Guid inventoryItemId)
    {
        // Find all MenuItemComponents that use this ingredient
        var components = await _componentRepository.GetByInventoryItemIdAsync(inventoryItemId);

        if (!components.Any())
        {
            _logger.LogInformation(
                "No MenuItems found using InventoryItem ID {InventoryItemId}",
                inventoryItemId);
            return;
        }

        // Get unique menu item IDs
        var menuItemIds = components.Select(c => c.MenuItemId).Distinct();

        // Update COGS for each affected menu item
        foreach (var menuItemId in menuItemIds)
        {
            try
            {
                await UpdateMenuItemCOGSAsync(menuItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating COGS for MenuItem {MenuItemId} after ingredient cost change",
                    menuItemId);
            }
        }

        _logger.LogInformation(
            "Recalculated COGS for {Count} MenuItems affected by InventoryItem {InventoryItemId}",
            menuItemIds.Count(), inventoryItemId);
    }

    public async Task CreateGrossProfitHistoryAsync(Guid menuItemId)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId);
        if (menuItem == null)
        {
            _logger.LogWarning("MenuItem with ID {MenuItemId} not found for history snapshot", menuItemId);
            return;
        }

        var grossProfit = menuItem.Price - menuItem.CurrentCOGS;
        var grossMargin = menuItem.Price > 0 
            ? Math.Round((decimal)(grossProfit / menuItem.Price), 4)
            : 0;

        var history = new MenuItemGrossProfitHistory
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItemId,
            Price = menuItem.Price,
            UnitCost = menuItem.CurrentCOGS,
            GrossProfit = grossProfit,
            GrossMargin = grossMargin,
            SnapshotDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _historyRepository.AddAsync(history);

        _logger.LogInformation(
            "Created gross profit history snapshot for MenuItem {MenuItemId}: Price=${Price:F2}, Cost=${Cost:F2}, GrossProfit=${GrossProfit:F2}, Margin={Margin:P}",
            menuItemId, menuItem.Price, menuItem.CurrentCOGS, grossProfit, grossMargin);
    }
}
