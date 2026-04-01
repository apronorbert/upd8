$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

param(
    [string]$ServiceName = "upd8",
    [string]$DisplayName = "upd8 Service",
    [string]$Description = "upd8 API service",
    [string]$ExePath
)

if ([string]::IsNullOrWhiteSpace($ExePath)) {
    throw "ExePath is required. Example: -ExePath 'C:\Program Files\upd8\upd8.exe'"
}

if (-not (Test-Path $ExePath)) {
    throw "Exe not found: $ExePath"
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $existing) {
    Write-Host "Service already exists: $ServiceName"
    return
}

Write-Host "Creating service $ServiceName..."
sc.exe create $ServiceName binPath= "\"$ExePath\"" start= auto DisplayName= "\"$DisplayName\"" obj= LocalSystem | Out-Host
sc.exe description $ServiceName "\"$Description\"" | Out-Host

Write-Host "Configuring recovery..."
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/5000 | Out-Host
sc.exe failureflag $ServiceName 1 | Out-Host

Write-Host "Starting service..."
sc.exe start $ServiceName | Out-Host

Write-Host "Done."
