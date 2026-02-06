using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/locations/{locationId:guid}/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryQueryRepository _inventoryQueryRepository;

    public InventoryController(IInventoryQueryRepository inventoryQueryRepository)
    {
        _inventoryQueryRepository = inventoryQueryRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetForLocation(Guid locationId)
    {
        var items = await _inventoryQueryRepository.GetInventoryForLocationAsync(locationId);
        return Ok(items);
    }
}
