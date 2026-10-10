# Screenshot test, Windows side. Usually started by scripts/shots.sh.
#
# Builds the app (x64, net10.0-windows), then runs every scenario in
# shot-scenarios.json at the same time: each gets its own copy of the build
# (run\<scenario>) with that scenario's settings as the app's settings file,
# and the app is started with --screenshot. Each run starts from clean
# settings, so the default sensor selection is applied as on a first run.
# Never touches any other copy of the app or its settings.
param(
    [string]$Work = (Join-Path $env:USERPROFILE 'gogogadget-shots'),
    [string[]]$Only = @(),
    [int]$DelayMs = 5000,
    [int]$TimeoutMs = 60000,
    [switch]$NoBuild,
    [switch]$KeepBuildServer
)
$ErrorActionPreference = 'Stop'
# -File passes "-Only a,b" as one string: split it.
$Only = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ })

$src = Join-Path $Work 'src'
$runRoot = Join-Path $Work 'run'
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
    if (-not $KeepBuildServer) {
        # The compiler server stays resident (~1 GB) after a build: free it.
        $null = Invoke-Tool 'dotnet' @('build-server', 'shutdown') (Join-Path $logs 'build-server')
    }
    if ($rc -ne 0) {
        Get-Content (Join-Path $logs 'build.out') | Select-String -Pattern 'error' | Select-Object -First 20 | ForEach-Object { $_.Line }
        Write-Output "BUILD FAILED rc=$rc (log: $logs\build.out)"
        exit 1
    }
    Write-Output ("build ok {0:N0} s" -f $t.Elapsed.TotalSeconds)
}

$scenarios = Get-Content -Raw (Join-Path $src 'scripts\shot-scenarios.json') | ConvertFrom-Json
foreach ($dir in @($out, $runRoot)) {
    if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

# 1. Prepare every scenario and start it; they run side by side.
$t = [Diagnostics.Stopwatch]::StartNew()
$started = @()
foreach ($scenario in $scenarios.PSObject.Properties) {
    $name = $scenario.Name
    if ($Only.Count -gt 0 -and $Only -notcontains $name) { continue }

    $run = Join-Path $runRoot $name
    $null = & robocopy $app $run /MIR /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE)" }
    # Portable mode: settings and logs stay in this run folder, not in the
    # shared %LOCALAPPDATA%\GoGoGadget (scenarios run side by side).
    New-Item -ItemType File -Force -Path (Join-Path $run 'portable.txt') | Out-Null

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
    $started += [pscustomobject]@{ Name = $name; Process = $p; Dir = $shotDir; Run = $run }
}

# 2. Collect results. Crash details: run\<scenario>\log\*Crash*.log
$failed = 0
foreach ($s in $started) {
    $remaining = [Math]::Max(1000, $TimeoutMs - [int]$t.ElapsedMilliseconds)
    if (-not $s.Process.WaitForExit($remaining)) {
        $s.Process.Kill()
        Write-Output "FAILED $($s.Name) : timeout"
        $failed++
        continue
    }
    $errorFile = Join-Path $s.Dir 'error.txt'
    $listFile = Join-Path $s.Dir 'shots.txt'
    if (Test-Path $errorFile) {
        Write-Output "FAILED $($s.Name) : $((Get-Content $errorFile | Select-Object -First 1))"
        $failed++
    } elseif (-not (Test-Path $listFile)) {
        Write-Output "FAILED $($s.Name) : no shots.txt (exit code $($s.Process.ExitCode), see $($s.Run)\log)"
        $failed++
    } else {
        Write-Output ("shot {0}: {1} files" -f $s.Name, (Get-Content $listFile).Count)
    }
}
Write-Output ("{0} scenarios in {1:N0} s, {2} failed" -f $started.Count, $t.Elapsed.TotalSeconds, $failed)
exit $failed
