using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/features")]
public class FeatureOptionsController : ControllerBase
{
    private readonly IFeatureOptionsRepository _featureOptionsRepository;

    public FeatureOptionsController(IFeatureOptionsRepository featureOptionsRepository)
    {
        _featureOptionsRepository = featureOptionsRepository;
    }

    [HttpGet]
    public async Task<ActionResult<FeatureOptionsDto>> Get()
    {
        var options = await _featureOptionsRepository.GetAsync();
        return Ok(options);
    }

    [HttpPut]
    public async Task<ActionResult<FeatureOptionsDto>> Update([FromBody] FeatureOptionsDto options)
    {
        await _featureOptionsRepository.UpdateAsync(options);
        return Ok(options);
    }
}
