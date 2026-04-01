namespace upd8.Models.Hardware;

public sealed record HardwareSnapshot(
    DateTimeOffset TimestampUtc,
    string MachineName,
    IReadOnlyList<CpuInfo> Cpus,
    IReadOnlyList<GpuInfo> Gpus,
    IReadOnlyList<DiskInfo> Disks,
    IReadOnlyList<NetworkAdapterInfo> NetworkAdapters,
    MemoryInfo? Memory);
