param(
    [string]$Remote = "origin"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

$projectPath = Join-Path $repoRoot "upd8\upd8.csproj"
$publishDir = Join-Path $repoRoot "publish"
$repoUrl = "https://github.com/apronorbert/upd8"
$packId = "upd8"
$mainExe = "upd8.exe"
$packTitle = "upd8"
$packAuthors = "apronorbert"

if (-not (Test-Path $projectPath)) {
    throw "Project not found: $projectPath"
}

[xml]$csproj = Get-Content $projectPath
$versionNode = $csproj.SelectSingleNode("//Project/PropertyGroup/Version")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "Version not found in $projectPath. Add <Version>x.y.z</Version>."
}
$version = $versionNode.InnerText.Trim()
Write-Host "Using version: $version"

$tag = "v$version"

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Host "Installing vpk tool..."
    dotnet tool install -g vpk
}

$dotnetTools = Join-Path $env:USERPROFILE ".dotnet\tools"
if ($env:Path -notlike "*$dotnetTools*") {
    $env:Path = "$env:Path;$dotnetTools"
}

Write-Host "Publishing..."
dotnet publish $projectPath -c Release -r win-x64 -o $publishDir --self-contained true

Write-Host "Downloading release metadata (if any)..."
$token = $env:GITHUB_TOKEN
if ([string]::IsNullOrWhiteSpace($token)) {
    $token = Read-Host "GITHUB_TOKEN is empty. Enter token (input hidden)" -AsSecureString |
        ForEach-Object { [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($_)) }
}

if (-not [string]::IsNullOrWhiteSpace($token)) {
    vpk download github --repoUrl $repoUrl --token $token
} else {
    Write-Host "No GITHUB_TOKEN provided. Proceeding without token."
    vpk download github --repoUrl $repoUrl
}

Write-Host "Packing..."
vpk pack --packId $packId --packVersion $version --packDir $publishDir --mainExe $mainExe --packTitle $packTitle --packAuthors $packAuthors --msi --instLocation PerMachine

Write-Host "Uploading..."
if (-not [string]::IsNullOrWhiteSpace($token)) {
    vpk upload github --repoUrl $repoUrl --publish --releaseName "upd8 $version" --tag "v$version" --token $token
} else {
    Write-Host "No GITHUB_TOKEN provided. Upload may fail due to rate limits."
    vpk upload github --repoUrl $repoUrl --publish --releaseName "upd8 $version" --tag "v$version"
}

Write-Host "Tagging and pushing $tag to $Remote..."
$existing = git tag -l $tag
if (-not [string]::IsNullOrWhiteSpace($existing)) {
    Write-Host "Tag already exists: $tag. Skipping tag creation."
} else {
    git tag $tag
}

git push $Remote $tag

Write-Host "Done."
