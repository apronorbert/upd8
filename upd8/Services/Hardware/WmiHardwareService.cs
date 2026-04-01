using System.Globalization;
using System.Management;
using upd8.Models.Hardware;

namespace upd8.Services.Hardware;

public sealed class WmiHardwareService : IHardwareService
{
    private readonly ILogger<WmiHardwareService> _logger;

    public WmiHardwareService(ILogger<WmiHardwareService> logger)
    {
        _logger = logger;
    }

    public HardwareSnapshot GetSnapshot()
    {
        var cpus = Safe("CPU", GetCpus);
        var gpus = Safe("GPU", GetGpus);
        var disks = Safe("Disk", GetDisks);
        var adapters = Safe("NetworkAdapter", GetNetworkAdapters);
        var memory = Safe("Memory", GetMemory);

        return new HardwareSnapshot(
            DateTimeOffset.UtcNow,
            Environment.MachineName,
            cpus,
            gpus,
            disks,
            adapters,
            memory);
    }

    private IReadOnlyList<CpuInfo> Safe(string name, Func<IReadOnlyList<CpuInfo>> getter)
    {
        try
        {
            return getter();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WMI query failed for {Name}", name);
            return Array.Empty<CpuInfo>();
        }
    }

    private IReadOnlyList<GpuInfo> Safe(string name, Func<IReadOnlyList<GpuInfo>> getter)
    {
        try
        {
            return getter();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WMI query failed for {Name}", name);
            return Array.Empty<GpuInfo>();
        }
    }

    private IReadOnlyList<DiskInfo> Safe(string name, Func<IReadOnlyList<DiskInfo>> getter)
    {
        try
        {
            return getter();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WMI query failed for {Name}", name);
            return Array.Empty<DiskInfo>();
        }
    }

    private IReadOnlyList<NetworkAdapterInfo> Safe(string name, Func<IReadOnlyList<NetworkAdapterInfo>> getter)
    {
        try
        {
            return getter();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WMI query failed for {Name}", name);
            return Array.Empty<NetworkAdapterInfo>();
        }
    }

    private MemoryInfo? Safe(string name, Func<MemoryInfo?> getter)
    {
        try
        {
            return getter();
        }
        catch
        {
            _logger.LogWarning("WMI query failed for {Name}", name);
            return null;
        }
    }

    private static IReadOnlyList<CpuInfo> GetCpus()
    {
        var list = new List<CpuInfo>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");

        foreach (ManagementObject obj in searcher.Get())
        {
            list.Add(new CpuInfo(
                obj["Name"]?.ToString(),
                obj["Manufacturer"]?.ToString(),
                ToUInt(obj["NumberOfCores"]),
                ToUInt(obj["NumberOfLogicalProcessors"]),
                ToUInt(obj["MaxClockSpeed"])));
        }

        return list;
    }

    private static IReadOnlyList<GpuInfo> GetGpus()
    {
        var list = new List<GpuInfo>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, DriverVersion, VideoProcessor, AdapterRAM FROM Win32_VideoController");

        foreach (ManagementObject obj in searcher.Get())
        {
            list.Add(new GpuInfo(
                obj["Name"]?.ToString(),
                obj["DriverVersion"]?.ToString(),
                obj["VideoProcessor"]?.ToString(),
                ToULong(obj["AdapterRAM"])));
        }

        return list;
    }

    private static IReadOnlyList<DiskInfo> GetDisks()
    {
        var list = new List<DiskInfo>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Index, Model, InterfaceType, MediaType, Size FROM Win32_DiskDrive");

        foreach (ManagementObject obj in searcher.Get())
        {
            list.Add(new DiskInfo(
                ToUInt(obj["Index"]),
                obj["Model"]?.ToString(),
                obj["InterfaceType"]?.ToString(),
                obj["MediaType"]?.ToString(),
                ToULong(obj["Size"])));
        }

        return list;
    }

    private static IReadOnlyList<NetworkAdapterInfo> GetNetworkAdapters()
    {
        var list = new List<NetworkAdapterInfo>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, MACAddress, Speed, NetEnabled, PhysicalAdapter FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True");

        foreach (ManagementObject obj in searcher.Get())
        {
            list.Add(new NetworkAdapterInfo(
                obj["Name"]?.ToString(),
                obj["MACAddress"]?.ToString(),
                ToULong(obj["Speed"]),
                ToBool(obj["NetEnabled"])));
        }

        return list;
    }

    private static MemoryInfo? GetMemory()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");

        foreach (ManagementObject obj in searcher.Get())
        {
            var totalKb = ToULong(obj["TotalVisibleMemorySize"]);
            var freeKb = ToULong(obj["FreePhysicalMemory"]);

            return new MemoryInfo(
                totalKb.HasValue ? totalKb.Value * 1024UL : null,
                freeKb.HasValue ? freeKb.Value * 1024UL : null);
        }

        return null;
    }

    private static uint? ToUInt(object? value)
    {
        return value is null ? null : Convert.ToUInt32(value, CultureInfo.InvariantCulture);
    }

    private static ulong? ToULong(object? value)
    {
        return value is null ? null : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
    }

    private static bool? ToBool(object? value)
    {
        return value is null ? null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }
}
