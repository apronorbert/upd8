using Microsoft.AspNetCore.Mvc;
using upd8.Models.Info;
using upd8.Services.Info;

namespace upd8.Controllers;

[ApiController]
[Route("api/info")]
public sealed class InfoController : ControllerBase
{
    private readonly IInfoService _infoService;

    public InfoController(IInfoService infoService)
    {
        _infoService = infoService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(InfoSnapshot), StatusCodes.Status200OK)]
    public ActionResult<InfoSnapshot> Get()
    {
        var snapshot = _infoService.GetSnapshot();
        return Ok(snapshot);
    }
}
