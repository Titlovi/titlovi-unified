$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$TestsRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $TestsRoot
$BuildRoot = Join-Path $TestsRoot '.build'
$OutRoot   = Join-Path $TestsRoot 'output'
$LogPath   = Join-Path $OutRoot 'test.log'
$TrxPath   = Join-Path $OutRoot 'test-results.trx'
$Project   = Join-Path $TestsRoot 'Titlovi.Tests\Titlovi.Tests.csproj'
$PortableSdkVersion = '10.0.302'
$SdkUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/$PortableSdkVersion/dotnet-sdk-$PortableSdkVersion-win-x64.zip"

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { New-Item -ItemType Directory -Path $Path -Force | Out-Null }
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
                    if ($ver.Major -ge 10) { $choices += [PSCustomObject]@{ Text = $text; Version = $ver } }
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
    if (-not $portable) { throw 'Could not locate a stable .NET 10 SDK.' }
    return $portable
}

function Invoke-DotNetLive([string]$DotNetPath, [string[]]$Arguments) {
    # Keep the log UTF-8 on Windows PowerShell 5.1. Tee-Object -FilePath may
    # append native-process output as UTF-16LE, producing mixed-encoding logs.
    & $DotNetPath @Arguments 2>&1 | ForEach-Object {
        $text = $_.ToString()
        Write-Host $text
        Add-Content -LiteralPath $LogPath -Value $text -Encoding UTF8
    }
    $exitCode = $LASTEXITCODE
    return [int]$exitCode
}

try {
    Ensure-Directory $OutRoot
    Ensure-Directory $BuildRoot
    if (Test-Path -LiteralPath $LogPath) { Remove-Item -LiteralPath $LogPath -Force }
    if (Test-Path -LiteralPath $TrxPath) { Remove-Item -LiteralPath $TrxPath -Force }
    Set-Content -LiteralPath $LogPath -Value ("Titlovi.com.Unified 1.1.9 regression test log - {0}`r`n" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -Encoding UTF8

    $sdk = Get-DotNetSdk
    Write-Host "Titlovi.com.Unified 1.1.9 regression suite" -ForegroundColor Green
    Write-Host "Using .NET SDK $($sdk.Version): $($sdk.Path)" -ForegroundColor Cyan
    Write-Host "No live Titlovi.com requests are made."

    $globalJsonPath = Join-Path $TestsRoot 'global.json'
    $globalJson = @{ sdk = @{ version = $sdk.Version; rollForward = 'disable'; allowPrerelease = $false } } | ConvertTo-Json -Depth 3
    Set-Content -LiteralPath $globalJsonPath -Value $globalJson -Encoding UTF8

    $nugetConfigPath = Join-Path $BuildRoot 'NuGet.Config'
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

    Push-Location $TestsRoot
    try {
        Add-Content -LiteralPath $LogPath -Value "`r`n=== RESTORE ===" -Encoding UTF8
        $restore = Invoke-DotNetLive $sdk.Path @('restore', $Project, '--configfile', $nugetConfigPath, '--nologo', '--verbosity', 'minimal', '-p:NuGetAudit=false')
        if ($restore -ne 0) { throw "Restore failed with exit code $restore." }

        Add-Content -LiteralPath $LogPath -Value "`r`n=== TEST ===" -Encoding UTF8
        $test = Invoke-DotNetLive $sdk.Path @('test', $Project, '-c', 'Release', '--no-restore', '--nologo', '--verbosity', 'normal', '--results-directory', $OutRoot, '--logger', 'trx;LogFileName=test-results.trx')
        if ($test -ne 0) { throw "Tests failed with exit code $test." }
    }
    finally {
        Pop-Location
    }

    Add-Content -LiteralPath $LogPath -Value "`r`nSUCCESS" -Encoding UTF8
    Write-Host "`nSUCCESS - regression suite passed." -ForegroundColor Green
    Write-Host "Log: $LogPath"
    Write-Host "TRX: $TrxPath"
}
catch {
    $message = $_.Exception.Message
    Write-Host "`nTESTS FAILED" -ForegroundColor Red
    Write-Host $message -ForegroundColor Red
    try { Add-Content -LiteralPath $LogPath -Value "`r`nTESTS FAILED: $message" -Encoding UTF8 } catch { }
    exit 1
}
