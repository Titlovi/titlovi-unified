$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
# Always run from the host project directory so relative project paths and global.json are resolved correctly.
Set-Location -LiteralPath $ProjectRoot
$BuildRoot   = Join-Path $ProjectRoot '.build'
$OutRoot     = Join-Path $ProjectRoot 'output'
$LogPath     = Join-Path $OutRoot 'build.log'
$PortableSdkVersion = '10.0.302'
$SdkUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/$PortableSdkVersion/dotnet-sdk-$PortableSdkVersion-win-x64.zip"

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
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
    } catch { return $null }
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
        if ($sdk) { return $sdk }
    }

    Ensure-Directory $BuildRoot
    $sdkZip = Join-Path $BuildRoot "dotnet-sdk-$PortableSdkVersion-win-x64.zip"
    $sdkDir = Join-Path $BuildRoot "dotnet-$PortableSdkVersion"
    $dotnet = Join-Path $sdkDir 'dotnet.exe'
    $portable = Get-StableSdkFromHost $dotnet
    if (-not $portable) {
        if (-not (Test-Path -LiteralPath $sdkZip)) {
            Write-Host "Downloading portable .NET SDK $PortableSdkVersion..." -ForegroundColor Cyan
            Invoke-WebRequest -UseBasicParsing -Uri $SdkUrl -OutFile $sdkZip
        }
        if (Test-Path -LiteralPath $sdkDir) { Remove-Item -LiteralPath $sdkDir -Recurse -Force }
        Ensure-Directory $sdkDir
        Expand-Archive -LiteralPath $sdkZip -DestinationPath $sdkDir -Force
        $portable = Get-StableSdkFromHost $dotnet
    }
    if (-not $portable) { throw "Could not locate a stable .NET 10 SDK." }
    return $portable
}

function Invoke-DotNetLive([string]$DotNetPath, [string[]]$Arguments) {
    # Do not let native process output become part of this function's return value,
    # and keep build.log UTF-8 on Windows PowerShell 5.1.
    & $DotNetPath @Arguments 2>&1 | ForEach-Object {
        $text = $_.ToString()
        Write-Host $text
        Add-Content -LiteralPath $LogPath -Value $text -Encoding UTF8
    }
    $exitCode = $LASTEXITCODE
    return [int]$exitCode
}

try {
    # Keep one predictable output directory and remove stale artifacts from older builds.
    if (Test-Path -LiteralPath $OutRoot) { Remove-Item -LiteralPath $OutRoot -Recurse -Force }
    Ensure-Directory $OutRoot
    Set-Content -LiteralPath $LogPath -Value ("Jellyfin.Titlovi 1.1.9 build log - {0}`r`nTarget Jellyfin: 12.0.0`r`n" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -Encoding UTF8

    Write-Host "Titlovi.com.Unified 1.1.9 - Jellyfin build" -ForegroundColor Green
    Write-Host "Target: Jellyfin 12.0 / .NET 10"
    Write-Host "This only compiles the plugin DLL; it does not install or modify Jellyfin."

    $sdk = Get-DotNetSdk
    Write-Host "Using .NET SDK $($sdk.Version): $($sdk.Path)" -ForegroundColor Cyan

    $globalJsonPath = Join-Path $ProjectRoot 'global.json'
    $globalJson = @{
        sdk = @{
            version = $sdk.Version
            rollForward = 'disable'
            allowPrerelease = $false
        }
    } | ConvertTo-Json -Depth 3
    Set-Content -LiteralPath $globalJsonPath -Value $globalJson -Encoding UTF8

    $nugetConfigPath = Join-Path $BuildRoot 'NuGet.Config'
    Ensure-Directory $BuildRoot
    $nugetConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
'@
    Set-Content -LiteralPath $nugetConfigPath -Value $nugetConfig -Encoding UTF8

    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

    Add-Content -LiteralPath $LogPath -Value "`r`n=== RESTORE ===" -Encoding UTF8
    $restore = Invoke-DotNetLive $sdk.Path @('restore','Jellyfin.Titlovi.csproj','--configfile',$nugetConfigPath,'--nologo','--verbosity','minimal','--ignore-failed-sources','-p:NuGetAudit=false')
    if ($restore -ne 0) { throw "Restore failed with exit code $restore." }

    Add-Content -LiteralPath $LogPath -Value "`r`n=== BUILD ===" -Encoding UTF8
    $build = Invoke-DotNetLive $sdk.Path @('build','Jellyfin.Titlovi.csproj','-c','Release','--no-restore','--nologo','--verbosity','normal','-p:NuGetAudit=false')
    if ($build -ne 0) { throw "Build failed with exit code $build." }

    $builtDll = Join-Path $ProjectRoot 'bin\Release\net10.0\Jellyfin.Titlovi.dll'
    if (-not (Test-Path -LiteralPath $builtDll)) { throw "Build succeeded but Jellyfin.Titlovi.dll was not found." }

    # Jellyfin expects meta.json and thumb.png beside the plugin DLL. Build the
    # ready-to-copy plugin directory directly under output; there is no second
    # staging/release directory.
    $packageFolderName = 'Titlovi.com (Unofficial)_1.1.9.0'
    $packageFolder = Join-Path $OutRoot $packageFolderName
    Ensure-Directory $packageFolder

    $finalDll   = Join-Path $packageFolder 'Jellyfin.Titlovi.dll'
    $finalThumb = Join-Path $packageFolder 'thumb.png'
    $finalMeta  = Join-Path $packageFolder 'meta.json'

    Copy-Item -LiteralPath $builtDll -Destination $finalDll -Force

    $thumbSource = Join-Path $ProjectRoot 'thumb.png'
    if (-not (Test-Path -LiteralPath $thumbSource)) { throw "thumb.png was not found." }
    Copy-Item -LiteralPath $thumbSource -Destination $finalThumb -Force

    # Generate meta.json as UTF-8 without BOM. Jellyfin 12 parses the manifest
    # during plugin discovery, so the package is validated before it is zipped.
    $meta = [ordered]@{
        category = 'Subtitles'
        changelog = 'Initial Titlovi.com.Unified release with shared Emby/Jellyfin subtitle logic, B/S/H alternative search, archive handling, encoding conversion and synchronization-aware release ranking.'
        description = 'Unofficial Titlovi.com subtitle provider for Jellyfin.'
        guid = '8d8e1f97-ae88-4b46-acac-a16f6a2d88dd'
        name = 'Titlovi.com (Unofficial)'
        overview = 'Unofficial Titlovi.com subtitle provider built from the shared Titlovi.com.Unified engine.'
        owner = 'Unofficial community plugin'
        targetAbi = '12.0.0.0'
        timestamp = (Get-Date).ToUniversalTime().ToString('o')
        version = '1.1.9.0'
        status = 'Active'
        autoUpdate = $false
        imagePath = 'thumb.png'
        assemblies = @('Jellyfin.Titlovi.dll')
    }
    $metaJson = $meta | ConvertTo-Json -Depth 4
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($finalMeta, $metaJson, $utf8NoBom)

    $metaBytes = [System.IO.File]::ReadAllBytes($finalMeta)
    if ($metaBytes.Length -ge 3 -and $metaBytes[0] -eq 0xEF -and $metaBytes[1] -eq 0xBB -and $metaBytes[2] -eq 0xBF) {
        throw 'Generated meta.json unexpectedly contains a UTF-8 BOM.'
    }
    try {
        $parsedMeta = Get-Content -Raw -LiteralPath $finalMeta | ConvertFrom-Json
    } catch {
        throw 'Generated meta.json is not valid JSON.'
    }
    if ($parsedMeta.name -ne 'Titlovi.com (Unofficial)') { throw 'Generated meta.json contains an unexpected plugin name.' }
    if ($parsedMeta.imagePath -ne 'thumb.png') { throw 'Generated meta.json is missing imagePath=thumb.png.' }
    if ($parsedMeta.assemblies -notcontains 'Jellyfin.Titlovi.dll') { throw 'Generated meta.json is missing the plugin assembly whitelist.' }
    if (-not (Test-Path -LiteralPath $finalDll)) { throw 'Jellyfin.Titlovi.dll is missing from the package folder.' }
    if (-not (Test-Path -LiteralPath $finalThumb)) { throw 'thumb.png is missing from the package folder.' }

    $packageZip = Join-Path $OutRoot 'Titlovi.com.Unified-1.1.9-Jellyfin-12.zip'
    if (Test-Path -LiteralPath $packageZip) { Remove-Item -LiteralPath $packageZip -Force }
    Compress-Archive -Path (Join-Path $packageFolder '*') -DestinationPath $packageZip -CompressionLevel Optimal

    Add-Content -LiteralPath $LogPath -Value "`r`nSUCCESS`r`nPlugin folder: $packageFolder`r`nPackage ZIP: $packageZip" -Encoding UTF8

    Write-Host "`nSUCCESS" -ForegroundColor Green
    Write-Host "Plugin folder: $packageFolder" -ForegroundColor Green
    Write-Host "Package ZIP:   $packageZip" -ForegroundColor Green
}
catch {
    $message = $_.Exception.Message
    Write-Host "`nBUILD FAILED" -ForegroundColor Red
    Write-Host $message -ForegroundColor Red
    try { Add-Content -LiteralPath $LogPath -Value "`r`nBUILD FAILED: $message" -Encoding UTF8 } catch { }
    exit 1
}
