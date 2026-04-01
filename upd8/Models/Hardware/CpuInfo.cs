namespace upd8.Models.Hardware;

public sealed record CpuInfo(
    string? Name,
    string? Manufacturer,
    uint? Cores,
    uint? LogicalProcessors,
    uint? MaxClockMhz);
