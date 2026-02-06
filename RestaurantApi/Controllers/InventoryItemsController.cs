using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/inventoryitems")]
public class InventoryItemsController : ControllerBase
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public InventoryItemsController(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAll()
    {
        var items = await _inventoryItemRepository.GetAllAsync();
        var dtos = items.Select(i => new InventoryItemDto
        {
            InventoryItemId = i.Id,
            Name = i.Name,
            Unit = i.Unit,
            Quantity = 0 // This endpoint returns items without location-specific quantities
        });
        
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryItemDto>> GetById(Guid id)
    {
        var item = await _inventoryItemRepository.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        var dto = new InventoryItemDto
        {
            InventoryItemId = item.Id,
            Name = item.Name,
            Unit = item.Unit,
            Quantity = 0
        };
        
        return Ok(dto);
    }
}
