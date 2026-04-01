namespace upd8.Models.Hardware;

public sealed record GpuInfo(
    string? Name,
    string? DriverVersion,
    string? VideoProcessor,
    ulong? AdapterRamBytes);
