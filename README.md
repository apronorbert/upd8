# upd8

Kestrel Web API with Swagger, WMI hardware endpoints, and self-update via Velopack from GitHub Releases.

**Endpoints**
1. `GET /api/hardware` returns CPU, GPU, disk, network adapters, and memory info from WMI.
1. `GET /api/software` returns installed software from Windows Registry (HKLM/HKCU uninstall keys).
1. `GET /api/info` returns host/OS/user/network/domain metadata with nulls for unavailable values.
1. `GET /api/update` checks for updates using the configured GitHub repo.
1. `POST /api/update/apply` downloads and applies updates, then restarts if an update exists.

**Config**
1. `Updates:RepoUrl` must point to the GitHub repository hosting releases.
1. `Updates:AccessToken` optional, needed for private repos or rate limits.
1. `Updates:Channel` optional. Leave empty to use the default channel from the packaged release.
1. `Updates:AutoCheckOnStartup` and `Updates:AutoApplyOnStartup` control startup behavior.

**Local Run**
```powershell
dotnet run --project .\upd8\upd8.csproj
```

## Packaging With Velopack

Velopack updates only work when the app is packaged and installed by Velopack.

**One-time tool install**
```powershell
dotnet tool install -g vpk
```

**Build and pack**
```powershell
dotnet publish .\upd8\upd8.csproj -c Release -r win-x64 -o .\publish --self-contained true
vpk download github --repoUrl https://github.com/apronorbert/upd8
vpk pack --packId upd8 --packVersion 0.1.0 --packDir .\publish --mainExe upd8.exe
```

**Upload to GitHub Releases**
```powershell
vpk upload github --repoUrl https://github.com/apronorbert/upd8 --publish
```

**Install (on Windows)**
1. Go to the GitHub Releases page for the repo.
1. Download the generated `Setup.exe` (or the installer produced by Velopack).
1. Run the installer. This creates the Velopack app directory and registers the app as installed.

## How Updates Apply

1. The app checks GitHub Releases for new Velopack packages.
1. When an update is found, it downloads the update package to the local Velopack packages folder.
1. The app calls `ApplyUpdatesAndRestart`, which exits, applies the update, and restarts the app.

## CI Release Workflow

The GitHub Actions workflow builds, packs, and uploads releases automatically on tags like `v0.1.0`.
