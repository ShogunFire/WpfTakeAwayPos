using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/menuitems")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemComponentRepository _menuItemComponentRepository;

    public MenuItemsController(
        IMenuItemRepository menuItemRepository,
        IMenuItemComponentRepository menuItemComponentRepository)
    {
        _menuItemRepository = menuItemRepository;
        _menuItemComponentRepository = menuItemComponentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetAll()
    {
        var items = await _menuItemRepository.GetAllAsync();
        var dtos = items.Select(i => new MenuItemDto
        {
            Id = i.Id,
            Name = i.Name,
            Description = i.Description,
            Price = i.Price,
            Category = i.Category,
            IsActive = i.IsActive
        });
        
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MenuItemDto>> GetById(Guid id)
    {
        var item = await _menuItemRepository.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        var dto = new MenuItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            Category = item.Category,
            IsActive = item.IsActive
        };
        
        return Ok(dto);
    }

    [HttpGet("{id:guid}/components")]
    public async Task<ActionResult<IEnumerable<MenuItemComponentDto>>> GetComponents(Guid id)
    {
        var components = await _menuItemComponentRepository.GetByMenuItemIdAsync(id);
        var dtos = components.Select(c => new MenuItemComponentDto
        {
            Id = c.Id,
            MenuItemId = c.MenuItemId,
            InventoryItemId = c.InventoryItemId,
            Quantity = c.Quantity
        });
        
        return Ok(dtos);
    }

    [HttpGet("components")]
    public async Task<ActionResult<IEnumerable<MenuItemComponentDto>>> GetAllComponents()
    {
        var components = await _menuItemComponentRepository.GetAllAsync();
        var dtos = components.Select(c => new MenuItemComponentDto
        {
            Id = c.Id,
            MenuItemId = c.MenuItemId,
            InventoryItemId = c.InventoryItemId,
            Quantity = c.Quantity
        });
        
        return Ok(dtos);
    }
}
