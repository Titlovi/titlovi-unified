Set-StrictMode -Version 2.0

$Script:RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$Script:BuildRoot = Join-Path $Script:RepoRoot '.build'
$Script:DistRoot = Join-Path $Script:RepoRoot 'dist'
$Script:PortableSdkVersion = '10.0.302'

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Reset-Directory([string]$Path) {
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    Ensure-Directory $Path
}

function Remove-LegacyGeneratedState {
    $legacyPaths = @(
        'src\Emby\bin',
        'src\Emby\obj',
        'src\Emby\.build',
        'src\Emby\embylibs',
        'src\Emby\output',
        'src\Jellyfin\bin',
        'src\Jellyfin\obj',
        'src\Jellyfin\.build',
        'src\Jellyfin\output',
        'tests\Titlovi.Tests\bin',
        'tests\Titlovi.Tests\obj',
        'tests\output'
    )

    foreach ($relative in $legacyPaths) {
        $path = Join-Path $Script:RepoRoot $relative
        if (Test-Path -LiteralPath $path) {
            Write-Host "Removing legacy generated directory: $relative" -ForegroundColor DarkGray
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

function Get-StableSdkFromHost([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) { return $null }
    try {
        $lines = @(& $Path --list-sdks 2>$null)
        if ($LASTEXITCODE -ne 0) { return $null }
        $choices = @()
        foreach ($line in $lines) {
            if ($line -match '^\s*([^\s]+)') {
                $text = $matches[1]
                if ($text -match '-') { continue }
                try {
                    $ver = [System.Version]::Parse($text)
                    if ($ver.Major -ge 10) {
                        $choices += [PSCustomObject]@{ Text = $text; Version = $ver }
                    }
                } catch { }
            }
        }
        if ($choices.Count -eq 0) { return $null }
        $best = $choices | Sort-Object Version -Descending | Select-Object -First 1
        return [PSCustomObject]@{ Path = $Path; Version = $best.Text }
    }
    catch { return $null }
}

function Get-DotNetSdk {
    $candidates = @()
    $programW6432 = [Environment]::GetEnvironmentVariable('ProgramW6432')
    if ($programW6432) { $candidates += (Join-Path $programW6432 'dotnet\dotnet.exe') }
    if ($env:ProgramFiles) { $candidates += (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe') }

    $fromPath = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if (-not $fromPath) { $fromPath = Get-Command dotnet -ErrorAction SilentlyContinue }
    if ($fromPath) { $candidates += $fromPath.Source }

    foreach ($candidate in ($candidates | Where-Object { $_ } | Select-Object -Unique)) {
        $sdk = Get-StableSdkFromHost $candidate
        if ($sdk) {
            Write-Host ("Using stable installed .NET SDK {0}: {1}" -f $sdk.Version, $sdk.Path) -ForegroundColor Cyan
            return $sdk
        }
    }

    $toolRoot = Join-Path $Script:BuildRoot 'tooling\windows'
    Ensure-Directory $toolRoot
    $sdkZip = Join-Path $toolRoot "dotnet-sdk-$Script:PortableSdkVersion-win-x64.zip"
    $sdkDir = Join-Path $toolRoot "dotnet-$Script:PortableSdkVersion"
    $dotnet = Join-Path $sdkDir 'dotnet.exe'
    $portable = Get-StableSdkFromHost $dotnet

    if (-not $portable) {
        $sdkUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/$Script:PortableSdkVersion/dotnet-sdk-$Script:PortableSdkVersion-win-x64.zip"
        if (-not (Test-Path -LiteralPath $sdkZip)) {
            Write-Host "Downloading portable .NET SDK $Script:PortableSdkVersion..." -ForegroundColor Cyan
            Invoke-WebRequest -UseBasicParsing -Uri $sdkUrl -OutFile $sdkZip
        }
        if (Test-Path -LiteralPath $sdkDir) {
            Remove-Item -LiteralPath $sdkDir -Recurse -Force
        }
        Ensure-Directory $sdkDir
        Expand-Archive -LiteralPath $sdkZip -DestinationPath $sdkDir -Force
        $portable = Get-StableSdkFromHost $dotnet
    }

    if (-not $portable) { throw "Could not locate a stable .NET 10 SDK." }
    Write-Host ("Using portable .NET SDK {0}: {1}" -f $portable.Version, $portable.Path) -ForegroundColor Cyan
    return $portable
}

function Get-SdkWorkingDirectory([string]$Name, [string]$Version) {
    $dir = Join-Path $Script:BuildRoot ("sdk\" + $Name)
    Reset-Directory $dir
    $globalJsonPath = Join-Path $dir 'global.json'
    $json = @{
        sdk = @{
            version = $Version
            rollForward = 'disable'
            allowPrerelease = $false
        }
    } | ConvertTo-Json -Depth 3
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($globalJsonPath, $json, $utf8NoBom)
    return $dir
}

function Get-NuGetConfigPath {
    $dir = Join-Path $Script:BuildRoot 'nuget'
    Ensure-Directory $dir
    $path = Join-Path $dir 'NuGet.Config'
    $content = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
'@
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
    return $path
}

function Invoke-DotNetLogged([string]$DotNetPath, [string[]]$Arguments, [string]$LogPath) {
    Ensure-Directory (Split-Path -Parent $LogPath)
    & $DotNetPath @Arguments 2>&1 | ForEach-Object {
        $text = $_.ToString()
        Write-Host $text
        Add-Content -LiteralPath $LogPath -Value $text -Encoding UTF8
    }
    return [int]$LASTEXITCODE
}

function Get-ProjectVersion([string]$ProjectPath) {
    [xml]$xml = Get-Content -Raw -LiteralPath $ProjectPath
    foreach ($group in @($xml.Project.PropertyGroup)) {
        if ($group.Version) { return [string]$group.Version }
    }
    throw "Project Version was not found in $ProjectPath"
}

Remove-LegacyGeneratedState
