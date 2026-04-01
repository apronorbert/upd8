using upd8.Models.Updates;

namespace upd8.Services.Updates;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken);
    Task<UpdateApplyResult> ApplyUpdatesAsync(CancellationToken cancellationToken);
}
