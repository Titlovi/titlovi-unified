param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Emby', 'Jellyfin', 'All')]
    [string]$Target
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
. (Join-Path $PSScriptRoot '_common.ps1')

$FallbackEmbyVersion = '4.10.0.40'

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

function Get-EmbyReferences([string]$RefRoot) {
    Reset-Directory $RefRoot
    $installed = Find-InstalledEmbyReferences
    if ($installed) {
        Write-Host "Using installed Emby reference assemblies from: $installed" -ForegroundColor Cyan
        foreach ($name in @('MediaBrowser.Model.dll', 'MediaBrowser.Common.dll', 'MediaBrowser.Controller.dll')) {
            Copy-Item -LiteralPath (Join-Path $installed $name) -Destination (Join-Path $RefRoot $name) -Force
        }
        return
    }

    $cache = Join-Path $Script:BuildRoot 'cache\Emby'
    Ensure-Directory $cache
    $archive = Join-Path $cache "emby-$FallbackEmbyVersion.tar.xz"
    $extract = Join-Path $cache "emby-$FallbackEmbyVersion-extract"
    $url = "https://github.com/MediaBrowser/Emby.Releases/releases/download/$FallbackEmbyVersion/emby-server-freebsd14_${FallbackEmbyVersion}_amd64.tar.xz"

    if (-not (Test-Path -LiteralPath $archive)) {
        Write-Host "Downloading fallback Emby $FallbackEmbyVersion reference archive..." -ForegroundColor Cyan
        Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $archive
    }
    Reset-Directory $extract

    $tar = Get-Command tar.exe -ErrorAction SilentlyContinue
    if (-not $tar) { $tar = Get-Command tar -ErrorAction SilentlyContinue }
    if (-not $tar) { throw 'tar was not found.' }
    & $tar.Source -xf $archive -C $extract
    if ($LASTEXITCODE -ne 0) { throw 'Could not extract the official Emby reference archive.' }

    foreach ($name in @('MediaBrowser.Model.dll', 'MediaBrowser.Common.dll', 'MediaBrowser.Controller.dll')) {
        $file = Get-ChildItem -LiteralPath $extract -Filter $name -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $file) { throw "Reference $name was not found in the Emby archive." }
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $RefRoot $name) -Force
    }
}

function Invoke-EmbyBuild {
    $project = Join-Path $Script:RepoRoot 'src\Emby\Emby.Titlovi.csproj'
    $refRoot = Join-Path $Script:BuildRoot 'Emby\refs'
    $binRoot = Join-Path $Script:BuildRoot 'Emby\bin'
    $objRoot = Join-Path $Script:BuildRoot 'Emby\obj'
    $dist = Join-Path $Script:DistRoot 'Emby'
    $log = Join-Path $Script:BuildRoot 'logs\Emby\build.log'

    Reset-Directory $binRoot
    Reset-Directory $objRoot
    Reset-Directory $dist
    Ensure-Directory (Split-Path -Parent $log)
    Set-Content -LiteralPath $log -Value ("Titlovi.com.Unified Emby build - {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -Encoding UTF8

    Write-Host "`nTitlovi.com.Unified - Emby build (Windows)" -ForegroundColor Green
    Write-Host "Target: Emby 4.10.x"
    Get-EmbyReferences $refRoot

    $sdk = Get-DotNetSdk
    $sdkWork = Get-SdkWorkingDirectory 'windows-emby' $sdk.Version
    $nuget = Get-NuGetConfigPath
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

    Push-Location $sdkWork
    try {
        $restore = Invoke-DotNetLogged $sdk.Path @('restore', $project, '--configfile', $nuget, '--nologo', '--verbosity', 'minimal', '--ignore-failed-sources', '-p:NuGetAudit=false') $log
        if ($restore -ne 0) { throw "Emby restore failed with exit code $restore." }
        $build = Invoke-DotNetLogged $sdk.Path @('build', $project, '-c', 'Release', '--no-restore', '--nologo', '--verbosity', 'normal', '-p:NuGetAudit=false') $log
        if ($build -ne 0) { throw "Emby build failed with exit code $build." }
    }
    finally { Pop-Location }

    $built = Join-Path $Script:BuildRoot 'Emby\bin\Release\netstandard2.0\Emby.Titlovi.dll'
    if (-not (Test-Path -LiteralPath $built)) { throw "Build succeeded but Emby.Titlovi.dll was not found at $built" }
    $final = Join-Path $dist 'Emby.Titlovi.dll'
    Copy-Item -LiteralPath $built -Destination $final -Force
    Add-Content -LiteralPath $log -Value "`r`nSUCCESS`r`nBuilt plugin: $final" -Encoding UTF8
    Write-Host "SUCCESS - $final" -ForegroundColor Green
}

function Invoke-JellyfinBuild {
    $project = Join-Path $Script:RepoRoot 'src\Jellyfin\Jellyfin.Titlovi.csproj'
    $thumb = Join-Path $Script:RepoRoot 'src\Jellyfin\thumb.png'
    $binRoot = Join-Path $Script:BuildRoot 'Jellyfin\bin'
    $objRoot = Join-Path $Script:BuildRoot 'Jellyfin\obj'
    $dist = Join-Path $Script:DistRoot 'Jellyfin'
    $log = Join-Path $Script:BuildRoot 'logs\Jellyfin\build.log'

    Reset-Directory $binRoot
    Reset-Directory $objRoot
    Reset-Directory $dist
    Ensure-Directory (Split-Path -Parent $log)
    Set-Content -LiteralPath $log -Value ("Titlovi.com.Unified Jellyfin build - {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -Encoding UTF8

    $version = Get-ProjectVersion $project
    $packageVersion = $version
    if ($packageVersion -match '^\d+\.\d+\.\d+$') { $packageVersion += '.0' }

    Write-Host "`nTitlovi.com.Unified $version - Jellyfin build (Windows)" -ForegroundColor Green
    Write-Host "Target: Jellyfin 12.0 / .NET 10"

    $sdk = Get-DotNetSdk
    $sdkWork = Get-SdkWorkingDirectory 'windows-jellyfin' $sdk.Version
    $nuget = Get-NuGetConfigPath
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

    Push-Location $sdkWork
    try {
        $restore = Invoke-DotNetLogged $sdk.Path @('restore', $project, '--configfile', $nuget, '--nologo', '--verbosity', 'minimal', '--ignore-failed-sources', '-p:NuGetAudit=false') $log
        if ($restore -ne 0) { throw "Jellyfin restore failed with exit code $restore." }
        $build = Invoke-DotNetLogged $sdk.Path @('build', $project, '-c', 'Release', '--no-restore', '--nologo', '--verbosity', 'normal', '-p:NuGetAudit=false') $log
        if ($build -ne 0) { throw "Jellyfin build failed with exit code $build." }
    }
    finally { Pop-Location }

    $built = Join-Path $Script:BuildRoot 'Jellyfin\bin\Release\net10.0\Jellyfin.Titlovi.dll'
    if (-not (Test-Path -LiteralPath $built)) { throw "Build succeeded but Jellyfin.Titlovi.dll was not found at $built" }
    if (-not (Test-Path -LiteralPath $thumb)) { throw "thumb.png was not found at $thumb" }

    $packageFolderName = "Titlovi.com_$packageVersion"
    $packageFolder = Join-Path $dist $packageFolderName
    Ensure-Directory $packageFolder
    $finalDll = Join-Path $packageFolder 'Jellyfin.Titlovi.dll'
    $finalThumb = Join-Path $packageFolder 'thumb.png'
    $finalMeta = Join-Path $packageFolder 'meta.json'
    Copy-Item -LiteralPath $built -Destination $finalDll -Force
    Copy-Item -LiteralPath $thumb -Destination $finalThumb -Force

    $meta = [ordered]@{
        category = 'Subtitles'
        changelog = 'Titlovi.com Unified 2.0: Jellyfin 12 support, preservation of legacy Jellyfin credentials, and shared Emby/Jellyfin subtitle engine.'
        description = 'Titlovi.com subtitle provider for Jellyfin.'
        guid = '6e25df50-638e-4109-a50b-03c14fc93fdd'
        name = 'Titlovi.com'
        overview = 'Titlovi.com for Jellyfin 12, built from the unified Emby and Jellyfin source tree.'
        owner = 'Titlovi.com'
        targetAbi = '12.0.0.0'
        timestamp = (Get-Date).ToUniversalTime().ToString('o')
        version = $packageVersion
        status = 'Active'
        autoUpdate = $false
        imagePath = 'thumb.png'
        assemblies = @('Jellyfin.Titlovi.dll')
    }
    $metaJson = $meta | ConvertTo-Json -Depth 4
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($finalMeta, $metaJson, $utf8NoBom)

    $parsedMeta = Get-Content -Raw -LiteralPath $finalMeta | ConvertFrom-Json
    if ($parsedMeta.name -ne 'Titlovi.com') { throw 'Generated meta.json contains an unexpected plugin name.' }
    if ($parsedMeta.imagePath -ne 'thumb.png') { throw 'Generated meta.json is missing imagePath=thumb.png.' }
    if ($parsedMeta.assemblies -notcontains 'Jellyfin.Titlovi.dll') { throw 'Generated meta.json is missing the plugin assembly whitelist.' }

    $zip = Join-Path $dist "titlovi-com_$packageVersion.zip"
    Compress-Archive -Path (Join-Path $packageFolder '*') -DestinationPath $zip -CompressionLevel Optimal -Force
    Add-Content -LiteralPath $log -Value "`r`nSUCCESS`r`nPlugin folder: $packageFolder`r`nPackage ZIP: $zip" -Encoding UTF8
    Write-Host "SUCCESS" -ForegroundColor Green
    Write-Host "Plugin folder: $packageFolder" -ForegroundColor Green
    Write-Host "Package ZIP:   $zip" -ForegroundColor Green
}

try {
    if ($Target -eq 'Emby' -or $Target -eq 'All') { Invoke-EmbyBuild }
    if ($Target -eq 'Jellyfin' -or $Target -eq 'All') { Invoke-JellyfinBuild }
    if ($Target -eq 'All') {
        Write-Host "`nBoth Windows builds completed successfully." -ForegroundColor Green
        Write-Host "Distribution artifacts are under: $Script:DistRoot" -ForegroundColor Cyan
    }
}
catch {
    $message = $_.Exception.Message
    Write-Host "`nBUILD FAILED" -ForegroundColor Red
    Write-Host $message -ForegroundColor Red
    exit 1
}
