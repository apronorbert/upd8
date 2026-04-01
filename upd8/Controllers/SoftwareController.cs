using Microsoft.AspNetCore.Mvc;
using upd8.Models.Software;
using upd8.Services.Software;

namespace upd8.Controllers;

[ApiController]
[Route("api/software")]
public sealed class SoftwareController : ControllerBase
{
    private readonly ISoftwareService _softwareService;

    public SoftwareController(ISoftwareService softwareService)
    {
        _softwareService = softwareService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SoftwareSnapshot), StatusCodes.Status200OK)]
    public ActionResult<SoftwareSnapshot> Get()
    {
        var snapshot = _softwareService.GetSnapshot();
        return Ok(snapshot);
    }
}
