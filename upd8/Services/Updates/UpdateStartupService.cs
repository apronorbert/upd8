using Microsoft.Extensions.Options;
using upd8.Options;

namespace upd8.Services.Updates;

public sealed class UpdateStartupService : BackgroundService
{
    private readonly IUpdateService _updateService;
    private readonly IOptions<UpdateSettings> _options;
    private readonly ILogger<UpdateStartupService> _logger;

    public UpdateStartupService(
        IUpdateService updateService,
        IOptions<UpdateSettings> options,
        ILogger<UpdateStartupService> logger)
    {
        _updateService = updateService;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = _options.Value;
        if (!settings.AutoCheckOnStartup && !settings.AutoApplyOnStartup && settings.CheckIntervalMinutes <= 0)
        {
            return;
        }

        if (settings.StartupDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(settings.StartupDelaySeconds), stoppingToken);
        }

        if (settings.AutoApplyOnStartup)
        {
            _logger.LogInformation("Auto-apply updates enabled.");
            await _updateService.ApplyUpdatesAsync(stoppingToken);
        }
        else if (settings.AutoCheckOnStartup)
        {
            _logger.LogInformation("Auto-check updates enabled.");
            await _updateService.CheckForUpdatesAsync(stoppingToken);
        }

        if (settings.CheckIntervalMinutes > 0)
        {
            var interval = TimeSpan.FromMinutes(settings.CheckIntervalMinutes);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(interval, stoppingToken);
                await _updateService.CheckForUpdatesAsync(stoppingToken);
            }
        }
    }
}
