using Microsoft.AspNetCore.Mvc;
using upd8.Models.Hardware;
using upd8.Services.Hardware;

namespace upd8.Controllers;

[ApiController]
[Route("api/hardware")]
public sealed class HardwareController : ControllerBase
{
    private readonly IHardwareService _hardwareService;

    public HardwareController(IHardwareService hardwareService)
    {
        _hardwareService = hardwareService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HardwareSnapshot), StatusCodes.Status200OK)]
    public ActionResult<HardwareSnapshot> Get()
    {
        var snapshot = _hardwareService.GetSnapshot();
        return Ok(snapshot);
    }
}
