namespace upd8.Options;

public sealed class UpdateSettings
{
    public const string SectionName = "Updates";

    public string RepoUrl { get; init; } = string.Empty;
    public string? AccessToken { get; init; }
    public bool IncludePrerelease { get; init; }
    public string? Channel { get; init; }
    public bool AutoCheckOnStartup { get; init; } = true;
    public bool AutoApplyOnStartup { get; init; }
    public int StartupDelaySeconds { get; init; } = 3;
    public int CheckIntervalMinutes { get; init; }
}
