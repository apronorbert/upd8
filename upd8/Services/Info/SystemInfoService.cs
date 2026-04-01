using System.Globalization;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.DirectoryServices.ActiveDirectory;
using Microsoft.Win32;
using upd8.Models.Info;

namespace upd8.Services.Info;

public sealed class SystemInfoService : IInfoService
{
    private readonly ILogger<SystemInfoService> _logger;

    public SystemInfoService(ILogger<SystemInfoService> logger)
    {
        _logger = logger;
    }

    public InfoSnapshot GetSnapshot()
    {
        var hostname = Environment.MachineName;
        var cs = Safe("ComputerSystem", GetComputerSystemInfo);
        var bios = Safe("BIOS", GetBiosInfo);
        var tpm = Safe("TPM", GetTpmInfo);
        var windows = Safe("Windows", GetWindowsInfo);
        var secureBoot = Safe("SecureBoot", GetSecureBootEnabled);
        var bootMode = Safe("BootMode", GetBootMode);
        if (bootMode is null && secureBoot == true)
        {
            bootMode = "UEFI";
        }

        return new InfoSnapshot(
            DateTimeOffset.UtcNow,
            hostname,
            Safe("FQDN", () => GetFqdn(hostname)),
            Safe("Domain", GetDomain),
            Safe("Site", GetSite),
            Safe("OS Version", () => Environment.OSVersion.VersionString),
            Safe("OS Description", () => RuntimeInformation.OSDescription),
            Safe("OS Architecture", () => RuntimeInformation.OSArchitecture.ToString()),
            Safe("Process Architecture", () => RuntimeInformation.ProcessArchitecture.ToString()),
            cs?.Manufacturer,
            cs?.Model,
            bios?.SerialNumber,
            bios?.BiosVersion,
            bios?.ReleaseDateUtc,
            cs?.DomainRole,
            cs?.DomainJoined,
            bootMode,
            cs?.TotalPhysicalMemoryBytes,
            cs?.HypervisorPresent,
            secureBoot,
            tpm?.Present,
            tpm?.Enabled,
            tpm?.Activated,
            tpm?.Owned,
            tpm?.ManufacturerId,
            tpm?.ManufacturerIdTxt,
            tpm?.SpecVersion,
            windows?.ProductName,
            windows?.DisplayVersion,
            windows?.BuildNumber,
            windows?.Ubr,
            windows?.InstallDateUtc,
            Safe("Uptime", GetUptime),
            Safe("Last Boot", GetLastBootUtc),
            Safe("TimeZone", () => TimeZoneInfo.Local.DisplayName),
            Safe("Culture", () => CultureInfo.CurrentCulture.Name),
            SafeList("Logged In Users", GetLoggedInUsers),
            SafeList("IP Addresses", GetIpAddresses),
            BuildExtras());
    }

    private IReadOnlyDictionary<string, string?> BuildExtras()
    {
        var extras = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        extras["UserName"] = Safe("UserName", () => Environment.UserName);
        extras["UserDomain"] = Safe("UserDomain", () => Environment.UserDomainName);
        extras["Is64BitOperatingSystem"] = Safe("Is64BitOperatingSystem", () => Environment.Is64BitOperatingSystem.ToString());
        extras["Is64BitProcess"] = Safe("Is64BitProcess", () => Environment.Is64BitProcess.ToString());
        extras["SystemDirectory"] = Safe("SystemDirectory", () => Environment.SystemDirectory);
        extras["ProcessorCount"] = Safe("ProcessorCount", () => Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture));

        return extras;
    }

    private string? GetFqdn(string hostname)
    {
        var entry = Dns.GetHostEntry(hostname);
        return string.IsNullOrWhiteSpace(entry.HostName) ? null : entry.HostName;
    }

    private string? GetDomain()
    {
        var ip = IPGlobalProperties.GetIPGlobalProperties();
        if (!string.IsNullOrWhiteSpace(ip.DomainName))
        {
            return ip.DomainName;
        }

        var envDomain = Environment.UserDomainName;
        return string.IsNullOrWhiteSpace(envDomain) ? null : envDomain;
    }

    private string? GetSite()
    {
        var site = ActiveDirectorySite.GetComputerSite();
        return string.IsNullOrWhiteSpace(site?.Name) ? null : site.Name;
    }

    private IReadOnlyList<string> GetLoggedInUsers()
    {
        var users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_LoggedOnUser");

        foreach (ManagementObject obj in searcher.Get())
        {
            var antecedent = obj["Antecedent"]?.ToString();
            if (string.IsNullOrWhiteSpace(antecedent))
            {
                continue;
            }

            var name = ExtractWmiKeyValue(antecedent, "Name");
            var domain = ExtractWmiKeyValue(antecedent, "Domain");
            var combined = string.IsNullOrWhiteSpace(domain) ? name : $"{domain}\\{name}";

            if (!string.IsNullOrWhiteSpace(combined))
            {
                users.Add(combined);
            }
        }

        return users.OrderBy(u => u, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private IReadOnlyList<string> GetIpAddresses()
    {
        var list = new List<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            foreach (var addr in ni.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
                    addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    list.Add(addr.Address.ToString());
                }
            }
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private ComputerSystemInfo? GetComputerSystemInfo()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Manufacturer, Model, DomainRole, PartOfDomain, TotalPhysicalMemory, HypervisorPresent FROM Win32_ComputerSystem");

        foreach (ManagementObject obj in searcher.Get())
        {
            return new ComputerSystemInfo(
                obj["Manufacturer"]?.ToString(),
                obj["Model"]?.ToString(),
                MapDomainRole(ToUInt(obj["DomainRole"])),
                ToBool(obj["PartOfDomain"]),
                ToULong(obj["TotalPhysicalMemory"]),
                ToBool(obj["HypervisorPresent"]));
        }

        return null;
    }

    private BiosInfo? GetBiosInfo()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT SerialNumber, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");

        foreach (ManagementObject obj in searcher.Get())
        {
            DateTimeOffset? releaseUtc = null;
            var rawDate = obj["ReleaseDate"]?.ToString();
            if (!string.IsNullOrWhiteSpace(rawDate))
            {
                var dt = ManagementDateTimeConverter.ToDateTime(rawDate);
                releaseUtc = new DateTimeOffset(dt).ToUniversalTime();
            }

            return new BiosInfo(
                obj["SerialNumber"]?.ToString(),
                obj["SMBIOSBIOSVersion"]?.ToString(),
                releaseUtc);
        }

        return null;
    }

    private TpmInfo? GetTpmInfo()
    {
        try
        {
            var scope = new ManagementScope(@"\\.\root\cimv2\Security\MicrosoftTpm");
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM Win32_Tpm"));

            foreach (ManagementObject obj in searcher.Get())
            {
                return new TpmInfo(
                    Present: true,
                    Enabled: ToBool(obj["IsEnabled_InitialValue"]),
                    Activated: ToBool(obj["IsActivated_InitialValue"]),
                    Owned: ToBool(obj["IsOwned_InitialValue"]),
                    ManufacturerId: obj["ManufacturerId"]?.ToString(),
                    ManufacturerIdTxt: obj["ManufacturerIdTxt"]?.ToString(),
                    SpecVersion: obj["SpecVersion"]?.ToString());
            }

            return new TpmInfo(false, null, null, null, null, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query TPM info");
        }

        var fromRegistry = ReadTpmFromRegistry();
        return fromRegistry;
    }

    private TpmInfo? ReadTpmFromRegistry()
    {
        bool? present = null;
        bool? enabled = null;
        bool? activated = null;
        bool? owned = null;
        string? manufacturerId = null;
        string? manufacturerIdTxt = null;
        string? specVersion = null;

        try
        {
            using var stateKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\TPM\State");
            if (stateKey is not null)
            {
                present = ReadBoolDword(stateKey, "TPMPresent", "TpmPresent");
                enabled = ReadBoolDword(stateKey, "TpmEnabled", "TPMEnabled", "IsEnabled");
                activated = ReadBoolDword(stateKey, "TpmActivated", "TPMActivated", "IsActivated");
                owned = ReadBoolDword(stateKey, "TpmOwned", "TPMOwned", "IsOwned");
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            using var tpmKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\TPM");
            if (tpmKey is not null)
            {
                manufacturerId = tpmKey.GetValue("ManufacturerId")?.ToString();
                manufacturerIdTxt = tpmKey.GetValue("ManufacturerIdTxt")?.ToString();
                specVersion = tpmKey.GetValue("SpecVersion")?.ToString();
            }
        }
        catch
        {
            // ignore
        }

        if (present is null && enabled is null && activated is null && owned is null &&
            string.IsNullOrWhiteSpace(manufacturerId) &&
            string.IsNullOrWhiteSpace(manufacturerIdTxt) &&
            string.IsNullOrWhiteSpace(specVersion))
        {
            return null;
        }

        return new TpmInfo(
            present ?? true,
            enabled,
            activated,
            owned,
            manufacturerId,
            manufacturerIdTxt,
            specVersion);
    }

    private WindowsInfo? GetWindowsInfo()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key is null)
        {
            return null;
        }

        var productName = key.GetValue("ProductName") as string;
        var displayVersion = key.GetValue("DisplayVersion") as string;
        var buildNumber = key.GetValue("CurrentBuildNumber") as string ?? key.GetValue("CurrentBuild") as string;
        var ubr = key.GetValue("UBR");
        var installDate = key.GetValue("InstallDate");

        DateTimeOffset? installUtc = null;
        if (installDate is int intVal)
        {
            installUtc = DateTimeOffset.FromUnixTimeSeconds(intVal);
        }
        else if (installDate is long longVal)
        {
            installUtc = DateTimeOffset.FromUnixTimeSeconds(longVal);
        }

        return new WindowsInfo(
            productName,
            displayVersion,
            buildNumber,
            ubr is null ? null : Convert.ToInt32(ubr, CultureInfo.InvariantCulture),
            installUtc);
    }

    private string? GetBootMode()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
        if (key is null)
        {
            return null;
        }

        var value = key.GetValue("PEFirmwareType");
        if (value is null)
        {
            return null;
        }

        var type = Convert.ToInt32(value, CultureInfo.InvariantCulture);
        return type switch
        {
            1 => "BIOS",
            2 => "UEFI",
            _ => null
        };
    }

    private bool? GetSecureBootEnabled()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
        if (key is null)
        {
            return null;
        }

        var value = key.GetValue("UEFISecureBootEnabled");
        if (value is null)
        {
            return null;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
    }

    private TimeSpan? GetUptime()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT LastBootUpTime FROM Win32_OperatingSystem");

        foreach (ManagementObject obj in searcher.Get())
        {
            var raw = obj["LastBootUpTime"]?.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var boot = ManagementDateTimeConverter.ToDateTime(raw);
            return DateTimeOffset.Now - boot;
        }

        return null;
    }

    private DateTimeOffset? GetLastBootUtc()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT LastBootUpTime FROM Win32_OperatingSystem");

        foreach (ManagementObject obj in searcher.Get())
        {
            var raw = obj["LastBootUpTime"]?.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var boot = ManagementDateTimeConverter.ToDateTime(raw);
            return new DateTimeOffset(boot).ToUniversalTime();
        }

        return null;
    }

    private T? Safe<T>(string name, Func<T?> getter)
    {
        try
        {
            return getter();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read {Name}", name);
            return default;
        }
    }

    private IReadOnlyList<string> SafeList(string name, Func<IReadOnlyList<string>> getter)
    {
        try
        {
            return getter();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read {Name}", name);
            return Array.Empty<string>();
        }
    }

    private static string? ExtractWmiKeyValue(string wmiObjectPath, string key)
    {
        var marker = key + "=\"";
        var start = wmiObjectPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        var end = wmiObjectPath.IndexOf("\"", start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            return null;
        }

        return wmiObjectPath[start..end];
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

    private static string? MapDomainRole(uint? role)
    {
        return role switch
        {
            0 => "StandaloneWorkstation",
            1 => "MemberWorkstation",
            2 => "StandaloneServer",
            3 => "MemberServer",
            4 => "BackupDomainController",
            5 => "PrimaryDomainController",
            _ => null
        };
    }

    private sealed record ComputerSystemInfo(
        string? Manufacturer,
        string? Model,
        string? DomainRole,
        bool? DomainJoined,
        ulong? TotalPhysicalMemoryBytes,
        bool? HypervisorPresent);

    private sealed record BiosInfo(
        string? SerialNumber,
        string? BiosVersion,
        DateTimeOffset? ReleaseDateUtc);

    private sealed record TpmInfo(
        bool? Present,
        bool? Enabled,
        bool? Activated,
        bool? Owned,
        string? ManufacturerId,
        string? ManufacturerIdTxt,
        string? SpecVersion);

    private sealed record WindowsInfo(
        string? ProductName,
        string? DisplayVersion,
        string? BuildNumber,
        int? Ubr,
        DateTimeOffset? InstallDateUtc);

    private static bool? ReadBoolDword(RegistryKey key, params string[] names)
    {
        foreach (var name in names)
        {
            var value = key.GetValue(name);
            if (value is null)
            {
                continue;
            }

            var intVal = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return intVal != 0;
        }

        return null;
    }
}
