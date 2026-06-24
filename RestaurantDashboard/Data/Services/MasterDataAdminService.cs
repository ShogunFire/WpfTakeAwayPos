using Dapper;
using Microsoft.Data.SqlClient;
using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

public interface IMasterDataAdminService
{
    Task<List<MasterCategory>> GetCategoriesAsync();
    Task<List<MasterInventoryItem>> GetInventoryItemsAsync();
    Task<List<MasterMenuItem>> GetMenuItemsAsync();
    Task<List<MasterMenuItemComponentView>> GetMenuItemComponentsAsync();

    Task CreateCategoryAsync(MasterCategory category);
    Task UpdateCategoryAsync(MasterCategory category);

    Task CreateInventoryItemAsync(MasterInventoryItem inventoryItem);
    Task UpdateInventoryItemAsync(MasterInventoryItem inventoryItem);

    Task CreateMenuItemAsync(MasterMenuItem menuItem);
    Task UpdateMenuItemAsync(MasterMenuItem menuItem);

    Task CreateMenuItemComponentAsync(MasterMenuItemComponent component);
    Task UpdateMenuItemComponentAsync(MasterMenuItemComponent component);
    Task<List<MasterMenuItemComponentView>> GetMenuItemComponentsByMenuItemIdAsync(Guid menuItemId);
    Task DeleteMenuItemComponentAsync(Guid id);
}

public class MasterDataAdminService : IMasterDataAdminService
{
    private readonly string _connectionString;

    public MasterDataAdminService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<List<MasterCategory>> GetCategoriesAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Name, Description, IsActive
            FROM Categories
            ORDER BY Name";

        var result = await connection.QueryAsync<MasterCategory>(sql);
        return result.ToList();
    }

    public async Task<List<MasterInventoryItem>> GetInventoryItemsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Name, Unit, CurrentUnitCost
            FROM InventoryItems
            ORDER BY Name";

        var result = await connection.QueryAsync<MasterInventoryItem>(sql);
        return result.ToList();
    }

    public async Task<List<MasterMenuItem>> GetMenuItemsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, IdCategory, Name, Description, Price, IsActive
            FROM MenuItems
            ORDER BY Name";

        var result = await connection.QueryAsync<MasterMenuItem>(sql);
        return result.ToList();
    }

    public async Task<List<MasterMenuItemComponentView>> GetMenuItemComponentsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT
                c.Id,
                c.MenuItemId,
                m.Name AS MenuItemName,
                c.InventoryItemId,
                i.Name AS InventoryItemName,
                c.Quantity
            FROM MenuItemComponents c
            INNER JOIN MenuItems m ON m.Id = c.MenuItemId
            INNER JOIN InventoryItems i ON i.Id = c.InventoryItemId
            ORDER BY m.Name, i.Name";

        var result = await connection.QueryAsync<MasterMenuItemComponentView>(sql);
        return result.ToList();
    }

    public async Task CreateCategoryAsync(MasterCategory category)
    {
        category.Id = category.Id == Guid.Empty ? Guid.NewGuid() : category.Id;

        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Categories (Id, Name, Description, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @Description, @IsActive, @Now, @Now)";

        await connection.ExecuteAsync(sql, new
        {
            category.Id,
            category.Name,
            category.Description,
            category.IsActive,
            Now = DateTime.UtcNow
        });
    }

    public async Task UpdateCategoryAsync(MasterCategory category)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE Categories
            SET Name = @Name,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = @Now
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            category.Id,
            category.Name,
            category.Description,
            category.IsActive,
            Now = DateTime.UtcNow
        });
    }

    public async Task CreateInventoryItemAsync(MasterInventoryItem inventoryItem)
    {
        inventoryItem.Id = inventoryItem.Id == Guid.Empty ? Guid.NewGuid() : inventoryItem.Id;

        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO InventoryItems (Id, Name, Unit, CurrentUnitCost, LastCostUpdate, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @Unit, @CurrentUnitCost, @Now, @Now, @Now)";

        await connection.ExecuteAsync(sql, new
        {
            inventoryItem.Id,
            inventoryItem.Name,
            inventoryItem.Unit,
            CurrentUnitCost = 0m,
            Now = DateTime.UtcNow
        });
    }

    public async Task UpdateInventoryItemAsync(MasterInventoryItem inventoryItem)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE InventoryItems
            SET Name = @Name,
                Unit = @Unit,
                UpdatedAt = @Now
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            inventoryItem.Id,
            inventoryItem.Name,
            inventoryItem.Unit,
            Now = DateTime.UtcNow
        });
    }

    public async Task CreateMenuItemAsync(MasterMenuItem menuItem)
    {
        menuItem.Id = menuItem.Id == Guid.Empty ? Guid.NewGuid() : menuItem.Id;

        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO MenuItems (Id, IdCategory, Name, Description, Price, CurrentCOGS, LastCOGSUpdate, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Id, @IdCategory, @Name, @Description, @Price, 0, NULL, @IsActive, @Now, @Now)";

        await connection.ExecuteAsync(sql, new
        {
            menuItem.Id,
            menuItem.IdCategory,
            menuItem.Name,
            menuItem.Description,
            menuItem.Price,
            menuItem.IsActive,
            Now = DateTime.UtcNow
        });
    }

    public async Task UpdateMenuItemAsync(MasterMenuItem menuItem)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE MenuItems
            SET IdCategory = @IdCategory,
                Name = @Name,
                Description = @Description,
                Price = @Price,
                IsActive = @IsActive,
                UpdatedAt = @Now
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            menuItem.Id,
            menuItem.IdCategory,
            menuItem.Name,
            menuItem.Description,
            menuItem.Price,
            menuItem.IsActive,
            Now = DateTime.UtcNow
        });
    }

    public async Task CreateMenuItemComponentAsync(MasterMenuItemComponent component)
    {
        component.Id = component.Id == Guid.Empty ? Guid.NewGuid() : component.Id;

        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO MenuItemComponents (Id, MenuItemId, InventoryItemId, Quantity)
            VALUES (@Id, @MenuItemId, @InventoryItemId, @Quantity)";

        await connection.ExecuteAsync(sql, component);
    }

    public async Task UpdateMenuItemComponentAsync(MasterMenuItemComponent component)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE MenuItemComponents
            SET MenuItemId = @MenuItemId,
                InventoryItemId = @InventoryItemId,
                Quantity = @Quantity
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, component);
    }

    public async Task<List<MasterMenuItemComponentView>> GetMenuItemComponentsByMenuItemIdAsync(Guid menuItemId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT
                c.Id,
                c.MenuItemId,
                m.Name AS MenuItemName,
                c.InventoryItemId,
                i.Name AS InventoryItemName,
                c.Quantity
            FROM MenuItemComponents c
            INNER JOIN MenuItems m ON m.Id = c.MenuItemId
            INNER JOIN InventoryItems i ON i.Id = c.InventoryItemId
            WHERE c.MenuItemId = @MenuItemId
            ORDER BY i.Name";

        var result = await connection.QueryAsync<MasterMenuItemComponentView>(sql, new { MenuItemId = menuItemId });
        return result.ToList();
    }

    public async Task DeleteMenuItemComponentAsync(Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM MenuItemComponents WHERE Id = @Id", new { Id = id });
    }
}
