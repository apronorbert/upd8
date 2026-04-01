namespace upd8.Models.Hardware;

public sealed record DiskInfo(
    uint? Index,
    string? Model,
    string? InterfaceType,
    string? MediaType,
    ulong? SizeBytes);
