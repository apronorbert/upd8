namespace upd8.Models.Software;

public sealed record SoftwareSnapshot(
    DateTimeOffset TimestampUtc,
    string MachineName,
    IReadOnlyList<SoftwareInfo> Items);
