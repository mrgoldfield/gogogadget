# Screenshot test, Windows side. Usually started by scripts/shots.sh.
#
# Builds the app (x64, net10.0-windows), then for each scenario in
# shot-scenarios.json: copies the build into a fresh run folder, writes
# that scenario's settings as the app's settings file, starts the app with
# --screenshot and collects the PNGs it saves. Each run starts from clean
# settings, so the default sensor selection is applied as on a first run.
# Never touches any other copy of the app or its settings.
param(
    [string]$Work = (Join-Path $env:USERPROFILE 'gogogadget-shots'),
    [string[]]$Only = @(),
    [int]$DelayMs = 5000,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
# -File passes "-Only a,b" as one string: split it.
$Only = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ })

$src = Join-Path $Work 'src'
$run = Join-Path $Work 'run'
$out = Join-Path $Work 'out'
$logs = Join-Path $Work 'logs'
$app = Join-Path $src 'bin\Release\x64\net10.0-windows'
$exeName = 'GoGoGadgetHardwareMonitor'
New-Item -ItemType Directory -Force -Path $logs | Out-Null

function Invoke-Tool([string]$file, [string[]]$arguments, [string]$log) {
    # Start-Process + redirected logs: native stderr would otherwise throw
    # under ErrorActionPreference=Stop in PowerShell 5.1.
    $p = Start-Process -FilePath $file -ArgumentList $arguments -NoNewWindow -PassThru `
        -RedirectStandardOutput "$log.out" -RedirectStandardError "$log.err"
    $null = $p.Handle
    $p.WaitForExit()
    return $p.ExitCode
}

if (-not $NoBuild) {
    $t = [Diagnostics.Stopwatch]::StartNew()
    $csproj = Join-Path $src 'LibreHardwareMonitor.Windows.Forms\LibreHardwareMonitor.Windows.Forms.csproj'
    $rc = Invoke-Tool 'dotnet' @('build', "`"$csproj`"", '-c', 'Release', '-p:Platform=x64', '-f', 'net10.0-windows', '-nologo', '-v:q') (Join-Path $logs 'build')
    if ($rc -ne 0) {
        Get-Content (Join-Path $logs 'build.out') | Select-String -Pattern 'error' | Select-Object -First 20 | ForEach-Object { $_.Line }
        Write-Output "BUILD FAILED rc=$rc (log: $logs\build.out)"
        exit 1
    }
    Write-Output ("build ok {0:N0} s" -f $t.Elapsed.TotalSeconds)
}

$scenarios = Get-Content -Raw (Join-Path $src 'scripts\shot-scenarios.json') | ConvertFrom-Json
if (Test-Path $out) { Remove-Item -Recurse -Force $out }
New-Item -ItemType Directory -Force -Path $out | Out-Null

$failed = 0
foreach ($scenario in $scenarios.PSObject.Properties) {
    $name = $scenario.Name
    if ($Only.Count -gt 0 -and $Only -notcontains $name) { continue }
    $t = [Diagnostics.Stopwatch]::StartNew()

    # Fresh copy of the build: robocopy /MIR also removes the previous
    # run's UserSettings and logs (exit codes below 8 = success).
    $null = & robocopy $app $run /MIR /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE)" }

    $settingsDir = Join-Path $run 'UserSettings'
    New-Item -ItemType Directory -Force -Path $settingsDir | Out-Null
    $xml = New-Object System.Xml.XmlDocument
    $null = $xml.AppendChild($xml.CreateXmlDeclaration('1.0', 'utf-8', $null))
    $configuration = $xml.AppendChild($xml.CreateElement('configuration'))
    $appSettings = $configuration.AppendChild($xml.CreateElement('appSettings'))
    foreach ($setting in $scenario.Value.PSObject.Properties) {
        $add = $xml.CreateElement('add')
        $add.SetAttribute('key', $setting.Name)
        $add.SetAttribute('value', [string]$setting.Value)
        $null = $appSettings.AppendChild($add)
    }
    $xml.Save((Join-Path $settingsDir "$exeName.config"))

    $shotDir = Join-Path $out $name
    $p = Start-Process -FilePath (Join-Path $run "$exeName.exe") -PassThru `
        -ArgumentList @('--screenshot', "`"$shotDir`"", '--screenshot-delay', $DelayMs)
    $null = $p.Handle
    if (-not $p.WaitForExit($DelayMs + 45000)) {
        $p.Kill()
        Write-Output "FAILED $name : timeout"
        $failed++
        continue
    }
    if (Test-Path (Join-Path $shotDir 'error.txt')) {
        Write-Output "FAILED $name : $((Get-Content (Join-Path $shotDir 'error.txt') | Select-Object -First 1))"
        $failed++
        continue
    }
    if (-not (Test-Path (Join-Path $shotDir 'shots.txt'))) {
        Write-Output "FAILED $name : no shots.txt (exit code $($p.ExitCode))"
        $failed++
        continue
    }
    $count = (Get-Content (Join-Path $shotDir 'shots.txt')).Count
    Write-Output ("shot {0}: {1} files {2:N0} s" -f $name, $count, $t.Elapsed.TotalSeconds)
}
exit $failed
