namespace upd8.Models.Updates;

public sealed record UpdateCheckResult(
    bool Enabled,
    bool IsInstalled,
    bool UpdateAvailable,
    string? CurrentVersion,
    string? LatestVersion,
    string? Message);
