namespace upd8.Models.Hardware;

public sealed record MemoryInfo(
    ulong? TotalBytes,
    ulong? FreeBytes);
