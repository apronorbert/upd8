using Microsoft.Win32;
using upd8.Models.Software;

namespace upd8.Services.Software;

public sealed class RegistrySoftwareService : ISoftwareService
{
    private static readonly string[] UninstallSubkeys =
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    };

    private readonly ILogger<RegistrySoftwareService> _logger;

    public RegistrySoftwareService(ILogger<RegistrySoftwareService> logger)
    {
        _logger = logger;
    }

    public SoftwareSnapshot GetSnapshot()
    {
        var items = new List<SoftwareInfo>();

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            items.AddRange(ReadUninstallKeys(RegistryHive.LocalMachine, view, "HKLM"));
            items.AddRange(ReadUninstallKeys(RegistryHive.CurrentUser, view, "HKCU"));
        }

        var distinct = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .GroupBy(i => $"{i.Name}|{i.Version}|{i.Publisher}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SoftwareSnapshot(DateTimeOffset.UtcNow, Environment.MachineName, distinct);
    }

    private IEnumerable<SoftwareInfo> ReadUninstallKeys(RegistryHive hive, RegistryView view, string source)
    {
        var results = new List<SoftwareInfo>();

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            foreach (var subkeyPath in UninstallSubkeys)
            {
                using var uninstallKey = baseKey.OpenSubKey(subkeyPath);
                if (uninstallKey is null)
                {
                    continue;
                }

                foreach (var name in uninstallKey.GetSubKeyNames())
                {
                    using var appKey = uninstallKey.OpenSubKey(name);
                    if (appKey is null)
                    {
                        continue;
                    }

                    var displayName = appKey.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        continue;
                    }

                    results.Add(new SoftwareInfo(
                        displayName.Trim(),
                        appKey.GetValue("DisplayVersion") as string,
                        appKey.GetValue("Publisher") as string,
                        appKey.GetValue("InstallDate") as string,
                        appKey.GetValue("InstallLocation") as string,
                        appKey.GetValue("UninstallString") as string,
                        appKey.GetValue("EstimatedSize") as int?,
                        $"{source}:{view}"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed reading uninstall keys for {Hive} {View}", hive, view);
        }

        return results;
    }
}
