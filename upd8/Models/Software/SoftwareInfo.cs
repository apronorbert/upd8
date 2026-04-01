namespace upd8.Models.Software;

public sealed record SoftwareInfo(
    string Name,
    string? Version,
    string? Publisher,
    string? InstallDate,
    string? InstallLocation,
    string? UninstallString,
    int? EstimatedSizeKb,
    string Source);
