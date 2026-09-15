$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
. (Join-Path $PSScriptRoot '_common.ps1')

try {
    $project = Join-Path $Script:RepoRoot 'tests\Titlovi.Tests\Titlovi.Tests.csproj'
    $binRoot = Join-Path $Script:BuildRoot 'Tests\bin'
    $objRoot = Join-Path $Script:BuildRoot 'Tests\obj'
    $results = Join-Path $Script:BuildRoot 'Tests\results'
    $log = Join-Path $Script:BuildRoot 'logs\Tests\test.log'
    $trx = Join-Path $results 'test-results.trx'

    Reset-Directory $binRoot
    Reset-Directory $objRoot
    Reset-Directory $results
    Ensure-Directory (Split-Path -Parent $log)
    Set-Content -LiteralPath $log -Value ("Titlovi.com.Unified regression test log - {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) -Encoding UTF8

    $sdk = Get-DotNetSdk
    $sdkWork = Get-SdkWorkingDirectory 'windows-tests' $sdk.Version
    $nuget = Get-NuGetConfigPath
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

    Write-Host "Titlovi.com.Unified regression suite (Windows)" -ForegroundColor Green
    Write-Host "No live Titlovi.com requests are made."

    Push-Location $sdkWork
    try {
        $restore = Invoke-DotNetLogged $sdk.Path @('restore', $project, '--configfile', $nuget, '--nologo', '--verbosity', 'minimal', '-p:NuGetAudit=false') $log
        if ($restore -ne 0) { throw "Restore failed with exit code $restore." }
        $test = Invoke-DotNetLogged $sdk.Path @('test', $project, '-c', 'Release', '--no-restore', '--nologo', '--verbosity', 'normal', '--results-directory', $results, '--logger', 'trx;LogFileName=test-results.trx') $log
        if ($test -ne 0) { throw "Tests failed with exit code $test." }
    }
    finally { Pop-Location }

    if (-not (Test-Path -LiteralPath $trx)) { throw "test-results.trx was not produced at $trx" }
    Add-Content -LiteralPath $log -Value "`r`nSUCCESS" -Encoding UTF8
    Write-Host "`nSUCCESS - regression suite passed." -ForegroundColor Green
    Write-Host "Log: $log"
    Write-Host "TRX: $trx"
}
catch {
    $message = $_.Exception.Message
    Write-Host "`nTESTS FAILED" -ForegroundColor Red
    Write-Host $message -ForegroundColor Red
    exit 1
}
