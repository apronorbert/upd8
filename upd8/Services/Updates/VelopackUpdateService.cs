using Microsoft.Extensions.Options;
using upd8.Models.Updates;
using upd8.Options;
using Velopack;
using Velopack.Sources;

namespace upd8.Services.Updates;

public sealed class VelopackUpdateService : IUpdateService
{
    private readonly IOptions<UpdateSettings> _options;
    private readonly ILogger<VelopackUpdateService> _logger;

    public VelopackUpdateService(IOptions<UpdateSettings> options, ILogger<VelopackUpdateService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        var settings = _options.Value;
        if (string.IsNullOrWhiteSpace(settings.RepoUrl))
        {
            return new UpdateCheckResult(false, false, false, null, null, "Updates are disabled.");
        }

        var manager = CreateManager(settings);
        if (!manager.IsInstalled)
        {
            return new UpdateCheckResult(true, false, false, manager.CurrentVersion?.ToString(), null,
                "App is not installed via Velopack.");
        }

        var updateInfo = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
        if (updateInfo is null)
        {
            return new UpdateCheckResult(true, true, false, manager.CurrentVersion?.ToString(), null, "No updates available.");
        }

        var target = updateInfo.TargetFullRelease;
        return new UpdateCheckResult(true, true, true, manager.CurrentVersion?.ToString(), target?.Version?.ToString(),
            "Update available.");
    }

    public async Task<UpdateApplyResult> ApplyUpdatesAsync(CancellationToken cancellationToken)
    {
        var settings = _options.Value;
        if (string.IsNullOrWhiteSpace(settings.RepoUrl))
        {
            return new UpdateApplyResult(false, false, false, "Updates are disabled.");
        }

        var manager = CreateManager(settings);
        if (!manager.IsInstalled)
        {
            return new UpdateApplyResult(true, false, false, "App is not installed via Velopack.");
        }

        var updateInfo = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
        if (updateInfo is null)
        {
            return new UpdateApplyResult(true, true, false, "No updates available.");
        }

        await manager.DownloadUpdatesAsync(updateInfo, progress =>
        {
            _logger.LogInformation("Update download {Progress}%", progress);
        }, cancellationToken).ConfigureAwait(false);

        var target = updateInfo.TargetFullRelease;
        if (target is null)
        {
            return new UpdateApplyResult(true, true, false, "Update target not found.");
        }

        manager.ApplyUpdatesAndRestart(target, Array.Empty<string>());
        return new UpdateApplyResult(true, true, true, "Update applied. Restarting...");
    }

    private static UpdateManager CreateManager(UpdateSettings settings)
    {
        var source = new GithubSource(
            settings.RepoUrl,
            settings.AccessToken ?? string.Empty,
            settings.IncludePrerelease,
            null);

        var options = new UpdateOptions
        {
            ExplicitChannel = string.IsNullOrWhiteSpace(settings.Channel) ? null : settings.Channel
        };

        return new UpdateManager(source, options, locator: null);
    }
}
