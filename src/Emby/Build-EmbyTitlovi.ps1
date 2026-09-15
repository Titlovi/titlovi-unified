$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
# Always run from the host project directory so relative project paths and global.json are resolved correctly.
Set-Location -LiteralPath $ProjectRoot
$BuildRoot   = Join-Path $ProjectRoot '.build'
$RefRoot     = Join-Path $ProjectRoot 'embylibs'
$OutRoot     = Join-Path $ProjectRoot 'output'
$LogPath     = Join-Path $OutRoot 'build.log'
$FallbackEmbyVersion = '4.10.0.40'
$PortableSdkVersion = '8.0.425'
$SdkUrl      = "https://builds.dotnet.microsoft.com/dotnet/Sdk/$PortableSdkVersion/dotnet-sdk-$PortableSdkVersion-win-x64.zip"
$EmbyRefUrl  = "https://github.com/MediaBrowser/Emby.Releases/releases/download/$FallbackEmbyVersion/emby-server-freebsd14_${FallbackEmbyVersion}_amd64.tar.xz"

function Write-Step([string]$Text) {
    Write-Host "`n==> $Text" -ForegroundColor Cyan
}

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Add-Log([string[]]$Lines) {
    if ($null -eq $Lines) { return }
    Add-Content -LiteralPath $LogPath -Value $Lines -Encoding UTF8
}

function Test-EmbyReferenceFolder([string]$Folder) {
    if ([string]::IsNullOrWhiteSpace($Folder)) { return $false }
    return (Test-Path (Join-Path $Folder 'MediaBrowser.Model.dll')) -and
           (Test-Path (Join-Path $Folder 'MediaBrowser.Common.dll')) -and
           (Test-Path (Join-Path $Folder 'MediaBrowser.Controller.dll'))
}

function Find-InstalledEmbyReferences {
    try {
        $proc = Get-Process -Name 'EmbyServer' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($proc -and $proc.Path) {
            $dir = Split-Path -Parent $proc.Path
            if (Test-EmbyReferenceFolder $dir) { return $dir }
            $systemDir = Join-Path $dir 'system'
            if (Test-EmbyReferenceFolder $systemDir) { return $systemDir }
        }
    } catch { }

    $candidates = @()
    if ($env:APPDATA)      { $candidates += (Join-Path $env:APPDATA 'Emby-Server\system') }
    if ($env:PROGRAMDATA)  { $candidates += (Join-Path $env:PROGRAMDATA 'Emby-Server\system') }
    if ($env:LOCALAPPDATA) {
        $candidates += (Join-Path $env:LOCALAPPDATA 'Emby-Server\system')
        $candidates += (Join-Path $env:LOCALAPPDATA 'Programs\Emby Server\system')
        $candidates += (Join-Path $env:LOCALAPPDATA 'Programs\EmbyServer\system')
    }
    if ($env:ProgramFiles) {
        $candidates += (Join-Path $env:ProgramFiles 'Emby\Emby Server\system')
        $candidates += (Join-Path $env:ProgramFiles 'Emby Server\system')
    }

    foreach ($dir in ($candidates | Select-Object -Unique)) {
        if (Test-EmbyReferenceFolder $dir) { return $dir }
    }

    return $null
}

function Get-EmbyReferences {
    Ensure-Directory $RefRoot
    Remove-Item (Join-Path $RefRoot '*.dll') -Force -ErrorAction SilentlyContinue

    $installed = Find-InstalledEmbyReferences
    if ($installed) {
        Write-Step "Found installed Emby reference assemblies in: $installed"
        foreach ($name in @('MediaBrowser.Model.dll','MediaBrowser.Common.dll','MediaBrowser.Controller.dll')) {
            Copy-Item -LiteralPath (Join-Path $installed $name) -Destination (Join-Path $RefRoot $name) -Force
        }
        return
    }

    Write-Step "Installed Emby assemblies were not located automatically. Downloading fallback Emby $FallbackEmbyVersion reference archive (~36 MB)."
    Ensure-Directory $BuildRoot
    $archive = Join-Path $BuildRoot "emby-$FallbackEmbyVersion.tar.xz"
    $extract = Join-Path $BuildRoot "emby-$FallbackEmbyVersion-extract"
    if (-not (Test-Path -LiteralPath $archive)) {
        Invoke-WebRequest -UseBasicParsing -Uri $EmbyRefUrl -OutFile $archive
    }
    if (Test-Path -LiteralPath $extract) { Remove-Item -LiteralPath $extract -Recurse -Force }
    Ensure-Directory $extract

    $tar = Get-Command tar.exe -ErrorAction SilentlyContinue
    if (-not $tar) { $tar = Get-Command tar -ErrorAction SilentlyContinue }
    if (-not $tar) { throw "Windows tar was not found." }

    & $tar.Source -xf $archive -C $extract
    if ($LASTEXITCODE -ne 0) { throw "Could not extract the official Emby reference archive." }

    foreach ($name in @('MediaBrowser.Model.dll','MediaBrowser.Common.dll','MediaBrowser.Controller.dll')) {
        $file = Get-ChildItem -LiteralPath $extract -Filter $name -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $file) { throw "Reference $name was not found in the Emby archive." }
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $RefRoot $name) -Force
    }
}

function Get-StableSdkFromHost([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) { return $null }

    try {
        $oldPref = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $sdkLines = @(& $Path --list-sdks 2>$null)
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference = $oldPref
        if ($exitCode -ne 0 -or $sdkLines.Count -eq 0) { return $null }

        $choices = @()
        foreach ($line in $sdkLines) {
            if ($line -match '^\s*([^\s]+)') {
                $text = $matches[1]
                # Do not select preview/RC SDKs for this build.
                if ($text -match '-') { continue }
                try {
                    $ver = [System.Version]::Parse($text)
                    if ($ver.Major -ge 8) {
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
            Write-Step ("Using stable installed .NET SDK {0}: {1}" -f $sdk.Version, $sdk.Path)
            return $sdk
        }
    }

    Write-Step "No stable x64 .NET 8+ SDK was found. Downloading portable Microsoft .NET SDK $PortableSdkVersion (no installation required)."
    Ensure-Directory $BuildRoot
    $sdkZip = Join-Path $BuildRoot "dotnet-sdk-$PortableSdkVersion-win-x64.zip"
    $sdkDir = Join-Path $BuildRoot "dotnet-$PortableSdkVersion"
    $dotnet = Join-Path $sdkDir 'dotnet.exe'

    $portable = Get-StableSdkFromHost $dotnet
    if (-not $portable) {
        if (-not (Test-Path -LiteralPath $sdkZip)) {
            Invoke-WebRequest -UseBasicParsing -Uri $SdkUrl -OutFile $sdkZip
        }
        if (Test-Path -LiteralPath $sdkDir) { Remove-Item -LiteralPath $sdkDir -Recurse -Force }
        Ensure-Directory $sdkDir
        Expand-Archive -LiteralPath $sdkZip -DestinationPath $sdkDir -Force
        $portable = Get-StableSdkFromHost $dotnet
    }

    if (-not $portable) { throw "Portable .NET SDK extraction succeeded, but no stable SDK was detected." }
    Write-Step ("Using portable .NET SDK {0}: {1}" -f $portable.Version, $portable.Path)
    return $portable
}

function Invoke-DotNetLive([string]$DotNetPath, [string[]]$Arguments, [string]$Prefix) {
    Ensure-Directory $BuildRoot
    $combined = Join-Path $BuildRoot ($Prefix + '.txt')
    Remove-Item $combined -Force -ErrorAction SilentlyContinue

    $oldPref = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $DotNetPath @Arguments 2>&1 | ForEach-Object {
            $text = $_.ToString()
            Write-Host $text
            Add-Content -LiteralPath $combined -Value $text -Encoding UTF8
        }
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $oldPref
    }

    if (Test-Path -LiteralPath $combined) {
        Add-Log @(Get-Content -LiteralPath $combined)
    }
    return $exitCode
}

try {
    # Keep one predictable output directory and remove stale artifacts from older builds.
    if (Test-Path -LiteralPath $OutRoot) { Remove-Item -LiteralPath $OutRoot -Recurse -Force }
    Ensure-Directory $OutRoot
    Set-Content -LiteralPath $LogPath -Value ("Emby.Titlovi 1.1.9 build log - {0}`r`nEmby references: installed server when available; fallback {1}`r`n" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $FallbackEmbyVersion) -Encoding UTF8

    Write-Host "Titlovi.com.Unified 1.1.9 - Emby build" -ForegroundColor Green
    Write-Host "Emby references: installed server when available (fallback $FallbackEmbyVersion)"
    Write-Host "This does NOT install or modify Emby. It only compiles the plugin DLL."

    Get-EmbyReferences
    $sdk = Get-DotNetSdk

    Write-Step 'Detected Emby reference assembly versions'
    $detectedVersions = @()
    foreach ($name in @('MediaBrowser.Model.dll','MediaBrowser.Common.dll','MediaBrowser.Controller.dll')) {
        $path = Join-Path $RefRoot $name
        $ver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($path).FileVersion
        if ([string]::IsNullOrWhiteSpace($ver)) { $ver = '(unknown)' }
        $detectedVersions += $ver
        $line = "  {0}: {1}" -f $name, $ver
        Write-Host $line
        Add-Log @($line)
    }

    $uniqueKnownVersions = @($detectedVersions | Where-Object { $_ -ne '(unknown)' } | Select-Object -Unique)
    if ($uniqueKnownVersions.Count -gt 1) {
        $warning = "WARNING: Emby reference assemblies report different file versions. The build will continue; compatibility is determined by the compiler/runtime, not blocked by this script."
        Write-Host $warning -ForegroundColor Yellow
        Add-Log @($warning)
    }

    # Force dotnet to use the stable SDK we actually selected, instead of a newer preview.
    $globalJsonPath = Join-Path $ProjectRoot 'global.json'
    $globalJson = @{
        sdk = @{
            version = $sdk.Version
            rollForward = 'disable'
            allowPrerelease = $false
        }
    } | ConvertTo-Json -Depth 3
    Set-Content -LiteralPath $globalJsonPath -Value $globalJson -Encoding UTF8
    Write-Step "Pinned build to stable .NET SDK $($sdk.Version)"
    Add-Log @("Selected dotnet: $($sdk.Path)", "Pinned SDK: $($sdk.Version)")

    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

    Write-Step 'Verifying selected .NET SDK'
    $infoExit = Invoke-DotNetLive $sdk.Path @('--version') 'dotnet-version'
    if ($infoExit -ne 0) { throw "Selected dotnet executable could not run the pinned SDK (exit $infoExit)." }

    # Use a clean NuGet configuration so a stale/private package source cannot stall restore.
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

    Write-Step 'Restoring framework packages (live output)'
    Add-Log @('', '=== RESTORE ===')
    $restoreExit = Invoke-DotNetLive $sdk.Path @('restore','Emby.Titlovi.csproj','--configfile',$nugetConfigPath,'--nologo','--verbosity','minimal','--ignore-failed-sources','-p:NuGetAudit=false') 'dotnet-restore'
    if ($restoreExit -ne 0) { throw "Restore failed with exit code $restoreExit. See output\build.log." }

    Write-Step 'Building Emby.Titlovi.dll (live output)'
    Add-Log @('', '=== BUILD ===')
    $buildExit = Invoke-DotNetLive $sdk.Path @('build','Emby.Titlovi.csproj','-c','Release','--no-restore','--nologo','--verbosity','normal','-p:NuGetAudit=false') 'dotnet-build'
    if ($buildExit -ne 0) { throw "Build failed with exit code $buildExit. See compiler output above and in output\build.log." }

    $builtDll = Join-Path $ProjectRoot 'bin\Release\netstandard2.0\Emby.Titlovi.dll'
    if (-not (Test-Path -LiteralPath $builtDll)) { throw "Build reported success but Emby.Titlovi.dll was not found." }

    $finalDll = Join-Path $OutRoot 'Emby.Titlovi.dll'
    Copy-Item -LiteralPath $builtDll -Destination $finalDll -Force
    Add-Log @('', 'SUCCESS', "Built plugin: $finalDll")

    Write-Host "`nSUCCESS" -ForegroundColor Green
    Write-Host "Built plugin: $finalDll" -ForegroundColor Green
    Write-Host "See README-BUILD.txt for installation instructions." -ForegroundColor Cyan
}
catch {
    $message = $_.Exception.Message
    if ([string]::IsNullOrWhiteSpace($message)) { $message = $_.ToString() }
    Write-Host "`nBUILD FAILED" -ForegroundColor Red
    Write-Host $message -ForegroundColor Red
    try { Add-Log @('', "BUILD FAILED: $message") } catch { }
    Write-Host "See output\build.log for diagnostic details." -ForegroundColor Yellow
    exit 1
}
