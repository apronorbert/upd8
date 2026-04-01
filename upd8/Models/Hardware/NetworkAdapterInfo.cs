namespace upd8.Models.Hardware;

public sealed record NetworkAdapterInfo(
    string? Name,
    string? MacAddress,
    ulong? SpeedBitsPerSec,
    bool? NetEnabled);
