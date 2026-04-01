using Microsoft.AspNetCore.Mvc;
using upd8.Models.Updates;
using upd8.Services.Updates;

namespace upd8.Controllers;

[ApiController]
[Route("api/update")]
public sealed class UpdateController : ControllerBase
{
    private readonly IUpdateService _updateService;

    public UpdateController(IUpdateService updateService)
    {
        _updateService = updateService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UpdateCheckResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateCheckResult>> Check(CancellationToken cancellationToken)
    {
        var result = await _updateService.CheckForUpdatesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("apply")]
    [ProducesResponseType(typeof(UpdateApplyResult), StatusCodes.Status202Accepted)]
    public ActionResult<UpdateApplyResult> Apply()
    {
        _ = Task.Run(() => _updateService.ApplyUpdatesAsync(CancellationToken.None));
        return Accepted(new UpdateApplyResult(true, true, true, "Update started. App may restart if an update is found."));
    }
}
