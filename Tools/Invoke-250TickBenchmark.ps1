[CmdletBinding()]
param(
    [string]$PackagePath =
        'A:\Dev\RimWorld\Runtime\MechMuster-Campaign-20260813-package-final',
    [string]$FixturePath =
        'A:\Dev\RimWorld\Mods\MechMuster\Developer\MechMuster.TestFixture',
    [string]$HarnessPackagePath =
        'A:\Dev\Temp\CanonicalHarnessRepairLivePackage-20260813',
    [string]$OutputRoot =
        'A:\Dev\RimWorld\Runtime\MechMuster-250Tick-Benchmark',
    [switch]$PreflightOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$harnessRoot = 'A:\Dev\RimWorld\Infrastructure\AgenticHarness\RimWorld-Agent'
$rwa = Join-Path $harnessRoot 'Tools\rwa.cmd'
$protocolPath = Join-Path $PSScriptRoot '..\Engineering\benchmark\250-tick-paired-protocol.json'
$expectedHarnessCommit =
    '35f9df1b4f75caedbeaac88b374098473cf1a60f'
$expectedHarnessFingerprint =
    '43181F0B1939EB7221C1CDB9A638575547A2750832FFFF4B7CCF8ADADF107803'
$expectedHarnessAssemblyHash =
    '473E60C56E7A301FD6F3A34837F73C7E276B57EB41E1CDC83972D983E2FCE8AE'
$expectedHarnessHarmonyHash =
    '7B9E756306FA3D7620E02A857C8927A6AB04973F9BD8A77D3866700A6DEAC55C'
$expectedPayloadHash =
    'AA6AEE99DD796436C9A9BF99CF60CCC0ADB374C4C96853DB753C7D9724F0949E'
$expectedFixtureHash =
    '50B9B556282279080F64245CEF2D013DAA0123A2D230301CC101ADD037A65D08'
$saveName = 'MechMuster-250Tick-Frozen'

function ConvertTo-RwaCommandLineArgument
{
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value -notmatch '[\s"]')
    {
        return $Value
    }
    $escaped = [Regex]::Replace($Value, '(\\*)"', '$1$1\"')
    $escaped = [Regex]::Replace($escaped, '(\\+)$', '$1$1')
    return '"' + $escaped + '"'
}

function Invoke-RwaJson
{
    param([Parameter(Mandatory = $true)][string[]]$RwaArguments)

    $quoted = @(@($rwa) + @($RwaArguments) | ForEach-Object {
            ConvertTo-RwaCommandLineArgument -Value ([string]$_)
        })
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $env:ComSpec
    $startInfo.Arguments = '/d /s /c "' + ($quoted -join ' ') + '"'
    $startInfo.WorkingDirectory = (Get-Location).Path
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $controller = [System.Diagnostics.Process]::new()
    $controller.StartInfo = $startInfo
    if (-not $controller.Start())
    {
        throw 'Could not start the canonical rwa.cmd controller.'
    }
    $stdoutTask = $controller.StandardOutput.ReadToEndAsync()
    $stderrTask = $controller.StandardError.ReadToEndAsync()
    $controller.WaitForExit()
    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    if (-not [string]::IsNullOrWhiteSpace($stderr) -and
        (Test-Path -LiteralPath $OutputRoot -PathType Container))
    {
        $diagnostic = @(
            "utc=$([DateTime]::UtcNow.ToString('o'))"
            "command=$($RwaArguments -join ' ')"
            $stderr.TrimEnd()) -join [Environment]::NewLine
        Add-Content -LiteralPath (Join-Path $OutputRoot `
            'rwa-controller-diagnostics.log') -Value $diagnostic
    }
    if ($controller.ExitCode -ne 0)
    {
        throw "rwa failed ($($controller.ExitCode)): $($RwaArguments -join ' ')`n$stderr`n$stdout"
    }

    return $stdout | ConvertFrom-Json
}

function Write-RawJson
{
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $Value | ConvertTo-Json -Depth 20 |
        Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-RunningRwaSessions
{
    $listed = Invoke-RwaJson -RwaArguments @('list', '-AllSessions')
    return @($listed.Sessions | Where-Object Status -in @(
            'starting', 'running', 'reserved', 'leased', 'active'))
}

function Invoke-RwaDoctor
{
    $doctor = Invoke-RwaJson -RwaArguments @('doctor')
    if (-not [bool]$doctor.Ready)
    {
        $blockers = @(
            @($doctor.LeaseBlockers) +
            @($doctor.UnregisteredGameProcesses) +
            @($doctor.ResourceLeases))
        throw "rwa doctor is not ready: $($blockers | ConvertTo-Json -Compress -Depth 10)"
    }
    return $doctor
}

function Wait-ForBenchmarkSlot
{
    param(
        [Parameter(Mandatory = $true)][string]$EvidencePath,
        [ValidateRange(1, 300)][int]$SnapshotIntervalSeconds = 15
    )

    $snapshots = @()
    $consecutiveClean = 0
    while ($true)
    {
        $doctor = Invoke-RwaJson -RwaArguments @('doctor')
        $running = @(Get-RunningRwaSessions)
        $leases = @($doctor.ResourceLeases)
        $unregistered = @($doctor.UnregisteredGameProcesses)
        $leaseBlockers = @($doctor.LeaseBlockers)
        $clean = [bool]$doctor.Ready -and
            [string]$doctor.CoordinationAuthority -eq 'machine-global' -and
            $null -eq $doctor.CoordinationError -and
            $leases.Count -eq 0 -and
            $unregistered.Count -eq 0 -and
            $leaseBlockers.Count -eq 0
        $snapshots += [PSCustomObject]@{
            Utc = [DateTime]::UtcNow.ToString('o')
            Clean = $clean
            ConsecutiveClean = if ($clean) { $consecutiveClean + 1 } else { 0 }
            Doctor = $doctor
            RunningSessions = @($running | Select-Object `
                SessionId, Status, Version, Owner, Purpose, Mode, ManifestPath)
        }
        Write-RawJson -Value $snapshots -Path $EvidencePath
        if ($clean)
        {
            $consecutiveClean++
            if ($consecutiveClean -ge 2)
            {
                return $doctor
            }
        }
        else
        {
            $consecutiveClean = 0
        }
        Start-Sleep -Seconds $SnapshotIntervalSeconds
    }
}

function Invoke-BenchmarkStart
{
    param(
        [Parameter(Mandatory = $true)][string[]]$RwaArguments,
        [Parameter(Mandatory = $true)][string]$IsolationEvidencePath
    )

    while ($true)
    {
        try
        {
            $evidenceRoot = Split-Path -Parent $IsolationEvidencePath
            $invocationId = [Guid]::NewGuid().ToString('N')
            $stdoutPath = Join-Path $evidenceRoot `
                "00-rwa-start-$invocationId.stdout.json"
            $stderrPath = Join-Path $evidenceRoot `
                "00-rwa-start-$invocationId.stderr.txt"
            $quoted = @(@($rwa) + @($RwaArguments) | ForEach-Object {
                    ConvertTo-RwaCommandLineArgument -Value ([string]$_)
                })
            $innerCommand = ($quoted -join ' ') +
                ' 1>' + ('"' + $stdoutPath + '"') +
                ' 2>' + ('"' + $stderrPath + '"')
            $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
            $startInfo.FileName = $env:ComSpec
            $startInfo.Arguments = '/d /s /c "' + $innerCommand + '"'
            $startInfo.WorkingDirectory = (Get-Location).Path
            $startInfo.UseShellExecute = $false
            $startInfo.CreateNoWindow = $true
            $controller = [System.Diagnostics.Process]::new()
            $controller.StartInfo = $startInfo
            if (-not $controller.Start())
            {
                throw 'Could not start the canonical rwa.cmd controller.'
            }

            $deadline = [DateTime]::UtcNow.AddMinutes(15)
            while ([DateTime]::UtcNow -lt $deadline)
            {
                if (Test-Path -LiteralPath $stdoutPath -PathType Leaf)
                {
                    $text = Get-Content -LiteralPath $stdoutPath -Raw
                    if (-not [string]::IsNullOrWhiteSpace($text))
                    {
                        try
                        {
                            $started = $text | ConvertFrom-Json
                            if ([string]$started.Action -eq 'started')
                            {
                                Write-RawJson -Value ([PSCustomObject]@{
                                    Utc = [DateTime]::UtcNow.ToString('o')
                                    ControllerProcessId = $controller.Id
                                    ControllerStillRunning = -not $controller.HasExited
                                    StandardOutputPath = $stdoutPath
                                    StandardErrorPath = $stderrPath
                                    SessionId = [string]$started.SessionId
                                }) -Path (Join-Path $evidenceRoot `
                                    '00-rwa-start-controller.json')
                                return $started
                            }
                        }
                        catch
                        {
                            # The pretty-printed JSON may still be in flight.
                        }
                    }
                }
                if ($controller.HasExited -and $controller.ExitCode -ne 0)
                {
                    $errorText = if (Test-Path -LiteralPath $stderrPath)
                    {
                        Get-Content -LiteralPath $stderrPath -Raw
                    }
                    else
                    {
                        '<no stderr>'
                    }
                    throw "rwa start failed ($($controller.ExitCode)):`n$errorText"
                }
                Start-Sleep -Milliseconds 250
            }
            throw "rwa start did not publish JSON within 15 minutes; stdout=$stdoutPath stderr=$stderrPath"
        }
        catch
        {
            if ($_.Exception.Message -notmatch 'RWA-LEASE-BLOCKED')
            {
                throw
            }
            $null = Wait-ForBenchmarkSlot -EvidencePath $IsolationEvidencePath
        }
    }
}

function Assert-OwnedBenchmarkIsolation
{
    param(
        [Parameter(Mandatory = $true)][string]$SessionId,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Phase
    )

    $doctor = Invoke-RwaJson -RwaArguments @('doctor')
    $running = @(Get-RunningRwaSessions)
    $ownedLeases = @($doctor.ResourceLeases | Where-Object {
            [string]$_.SessionId -eq $SessionId -and
            [string]$_.Mode -eq 'Benchmark'
        })
    $foreignLeases = @($doctor.ResourceLeases | Where-Object {
            [string]$_.SessionId -ne $SessionId
        })
    $foreignSessions = @($running | Where-Object {
            [string]$_.SessionId -ne $SessionId
        })
    $clean = [bool]$doctor.Ready -and
        [string]$doctor.CoordinationAuthority -eq 'machine-global' -and
        $null -eq $doctor.CoordinationError -and
        $ownedLeases.Count -eq 1 -and
        $foreignLeases.Count -eq 0 -and
        @($doctor.UnregisteredGameProcesses).Count -eq 0 -and
        @($doctor.LeaseBlockers).Count -eq 0
    $snapshot = [PSCustomObject]@{
        Utc = [DateTime]::UtcNow.ToString('o')
        Phase = $Phase
        Clean = $clean
        SessionId = $SessionId
        Doctor = $doctor
        RunningSessions = @($running | Select-Object `
            SessionId, Status, Version, Owner, Purpose, Mode, ManifestPath)
    }
    Write-RawJson -Value $snapshot -Path $Path
    if (-not $clean)
    {
        throw "Benchmark isolation changed during $Phase for session $SessionId."
    }
    return $snapshot
}

function Invoke-RecordedCommand
{
    param(
        [Parameter(Mandatory = $true)][string]$SessionId,
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $result = Invoke-RwaJson -RwaArguments @(
        'command', '-Session', $SessionId, '-Command', $Command)
    Write-RawJson -Value $result -Path $Path
    return $result
}

function Stop-RwaSession
{
    param(
        [Parameter(Mandatory = $true)][string]$SessionId,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $result = Invoke-RwaJson -RwaArguments @(
        'stop', '-Session', $SessionId)
    Write-RawJson -Value $result -Path $Path
}

function Write-SessionProvenance
{
    param(
        [Parameter(Mandatory = $true)][string]$ManifestPath,
        [Parameter(Mandatory = $true)][string]$OutputPath,
        [Parameter(Mandatory = $true)][string]$Phase
    )

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf))
    {
        throw "Canonical lane manifest is missing: $ManifestPath"
    }

    $manifest = Get-Content -LiteralPath $ManifestPath -Raw |
        ConvertFrom-Json
    $sessionId = [string]$manifest.LaneId
    $harness = @($manifest.Mods | Where-Object {
            $_.PackageId -eq 'Coolnether123.RimWorldAgent'
        })
    if ($harness.Count -ne 1 -or
        [string]$harness[0].SourceFingerprint -ne $expectedHarnessFingerprint -or
        [string]$harness[0].SnapshotFingerprint -ne $expectedHarnessFingerprint)
    {
        throw "Session $SessionId does not use the frozen canonical harness fingerprint."
    }
    if ([string]$manifest.Mode -ne 'Benchmark')
    {
        throw "Session $SessionId is not a typed Benchmark lease."
    }
    if ([string]$manifest.AgentRootEnvironmentValue -like 'A:\UserTemp*' -or
        [string]$manifest.ProfileRootEnvironmentValue -like 'A:\UserTemp*' -or
        [string]$manifest.PackageRootEnvironmentValue -like 'A:\UserTemp*')
    {
        throw "Session $SessionId fell back to A:\UserTemp."
    }

    $manifestOutput = Join-Path $OutputPath "$Phase-lane-manifest.json"
    Write-RawJson -Value $manifest -Path $manifestOutput
    if (Test-Path -LiteralPath $manifest.LogPath -PathType Leaf)
    {
        $logOutput = Join-Path $OutputPath "$Phase-Player.log"
        Copy-Item -LiteralPath $manifest.LogPath -Destination $logOutput -Force
    }
}

if (-not (Test-Path -LiteralPath $rwa -PathType Leaf))
{
    throw "Sanctioned rwa entry point is missing: $rwa"
}
if (-not (Test-Path -LiteralPath $protocolPath -PathType Leaf))
{
    throw "Protocol is missing: $protocolPath"
}
if (-not (Test-Path -LiteralPath $HarnessPackagePath -PathType Container))
{
    throw "Frozen canonical harness package is missing: $HarnessPackagePath"
}

$harnessCommit = (& git -C $harnessRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $harnessCommit -ne $expectedHarnessCommit)
{
    throw "Canonical harness commit mismatch: expected $expectedHarnessCommit, got $harnessCommit"
}
$harnessAssembly = Join-Path $HarnessPackagePath `
    '1.6\Assemblies\RimWorld Agent.dll'
$harnessHarmony = Join-Path $HarnessPackagePath '1.6\Assemblies\0Harmony.dll'
$harnessAssemblyHash =
    (Get-FileHash -LiteralPath $harnessAssembly -Algorithm SHA256).Hash
$harnessHarmonyHash =
    (Get-FileHash -LiteralPath $harnessHarmony -Algorithm SHA256).Hash
if ($harnessAssemblyHash -ne $expectedHarnessAssemblyHash -or
    $harnessHarmonyHash -ne $expectedHarnessHarmonyHash)
{
    throw 'Frozen canonical harness package hash mismatch.'
}

$protocol = Get-Content -LiteralPath $protocolPath -Raw | ConvertFrom-Json
$payload = Join-Path $PackagePath '1.6\Assemblies\MechMuster.dll'
$fixtureAssembly = Join-Path $FixturePath '1.6\Assemblies\MechMuster.TestFixture.dll'
if (-not (Test-Path -LiteralPath $payload -PathType Leaf))
{
    throw "Payload is missing: $payload"
}
if (-not (Test-Path -LiteralPath $fixtureAssembly -PathType Leaf))
{
    throw "Fixture assembly is missing: $fixtureAssembly"
}

$payloadHash = (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash
$fixtureHash = (Get-FileHash -LiteralPath $fixtureAssembly -Algorithm SHA256).Hash
if ($payloadHash -ne $expectedPayloadHash)
{
    throw "Payload hash mismatch: expected $expectedPayloadHash, got $payloadHash"
}
if ($fixtureHash -ne $expectedFixtureHash)
{
    throw "Fixture hash mismatch: expected $expectedFixtureHash, got $fixtureHash"
}

$doctor = Invoke-RwaDoctor
$blockers = @(Get-RunningRwaSessions)
$preflightReady = [bool]$doctor.Ready -and
    [string]$doctor.CoordinationAuthority -eq 'machine-global' -and
    $null -eq $doctor.CoordinationError -and
    @($doctor.ResourceLeases).Count -eq 0 -and
    @($doctor.UnregisteredGameProcesses).Count -eq 0 -and
    @($doctor.LeaseBlockers).Count -eq 0
$preflight = [PSCustomObject]@{
    SchemaVersion = 1
    Ready = $preflightReady
    WaitingForIsolation = -not $preflightReady
    RwaPath = $rwa
    CanonicalHarnessCommit = $harnessCommit
    HarnessPackagePath = [System.IO.Path]::GetFullPath($HarnessPackagePath)
    HarnessPackageFingerprint = $expectedHarnessFingerprint
    HarnessAssemblySha256 = $harnessAssemblyHash
    HarnessHarmonySha256 = $harnessHarmonyHash
    ProtocolPath = [System.IO.Path]::GetFullPath($protocolPath)
    PackagePath = [System.IO.Path]::GetFullPath($PackagePath)
    PayloadSha256 = $payloadHash
    FixturePath = [System.IO.Path]::GetFullPath($FixturePath)
    FixtureAssemblySha256 = $fixtureHash
    HarnessDoctor = $doctor
    RunningSessionBlockers = @($blockers | Select-Object `
        SessionId, Status, Version, Owner, Purpose, ManifestPath)
}

if ($PreflightOnly)
{
    $preflight | ConvertTo-Json -Depth 10
    if (-not $preflight.Ready)
    {
        exit 2
    }
    exit 0
}
if (-not $preflight.Ready)
{
    # The per-arm two-snapshot gate below owns transient waiting. Static
    # provenance failures have already thrown; a busy machine is not fatal.
}
if (Test-Path -LiteralPath $OutputRoot)
{
    throw "OutputRoot already exists: $OutputRoot"
}

[System.IO.Directory]::CreateDirectory($OutputRoot) | Out-Null
Write-RawJson -Value $preflight -Path (Join-Path $OutputRoot 'preflight.json')
Copy-Item -LiteralPath $protocolPath -Destination (Join-Path $OutputRoot 'protocol.json')
$additionalPathsFile = Join-Path $OutputRoot 'fixture-path.txt'
$FixturePath | Set-Content -LiteralPath $additionalPathsFile -Encoding UTF8

$setupRoot = Join-Path $OutputRoot 'fixture-creation'
[System.IO.Directory]::CreateDirectory($setupRoot) | Out-Null
$setupSession = $null
$setupManifestPath = $null
try
{
    $isolationEvidence = Join-Path $setupRoot '00-isolation-snapshots.json'
    $null = Wait-ForBenchmarkSlot -EvidencePath $isolationEvidence
    $start = Invoke-BenchmarkStart -IsolationEvidencePath $isolationEvidence `
        -RwaArguments @(
        'start', '-Mode', 'Benchmark', '-KeepAlive', '-DisableIdleCleanup',
        '-HarnessPackagePath', $HarnessPackagePath,
        '-Version', '1.6', '-Owner', 'MechMusterBenchmark',
        '-ModPath', $PackagePath,
        '-AdditionalModPathsFile', $additionalPathsFile,
        '-Purpose', 'Create frozen MechMuster 250-tick paired benchmark fixture')
    $setupSession = [string]$start.SessionId
    $setupManifestPath = [string]$start.Session
    Write-RawJson -Value $start -Path (Join-Path $setupRoot '01-start.json')
    Write-SessionProvenance -ManifestPath $setupManifestPath `
        -OutputPath $setupRoot `
        -Phase '01-running'
    Assert-OwnedBenchmarkIsolation -SessionId $setupSession `
        -Path (Join-Path $setupRoot '01-isolation-after-start.json') `
        -Phase 'fixture start' | Out-Null
    Invoke-RecordedCommand -SessionId $setupSession -Command 'quickstart' `
        -Path (Join-Path $setupRoot '02-quickstart.json') | Out-Null
    Start-Sleep -Seconds 15
    Assert-OwnedBenchmarkIsolation -SessionId $setupSession `
        -Path (Join-Path $setupRoot '02-isolation-before-workload.json') `
        -Phase 'fixture workload preparation' | Out-Null
    Invoke-RecordedCommand -SessionId $setupSession `
        -Command 'debug-action "Prepare 250-tick benchmark workload" category="Mech Muster"' `
        -Path (Join-Path $setupRoot '03-prepare.json') | Out-Null
    Invoke-RecordedCommand -SessionId $setupSession `
        -Command 'debug-action "Report 250-tick benchmark workload" category="Mech Muster"' `
        -Path (Join-Path $setupRoot '04-report.json') | Out-Null
    Invoke-RecordedCommand -SessionId $setupSession `
        -Command "dev-run save-game $saveName" `
        -Path (Join-Path $setupRoot '05-save.json') | Out-Null
    Start-Sleep -Seconds 5

    $lane = Get-Content -LiteralPath $setupManifestPath -Raw |
        ConvertFrom-Json
    $createdSave = Join-Path $lane.ProfileRoot "Saves\$saveName.rws"
    if (-not (Test-Path -LiteralPath $createdSave -PathType Leaf))
    {
        throw "Frozen save was not created: $createdSave"
    }

    $frozenSave = Join-Path $OutputRoot "$saveName.rws"
    Copy-Item -LiteralPath $createdSave -Destination $frozenSave
    [PSCustomObject]@{
        Path = $frozenSave
        Sha256 = (Get-FileHash -LiteralPath $frozenSave -Algorithm SHA256).Hash
        SizeBytes = (Get-Item -LiteralPath $frozenSave).Length
    } | ConvertTo-Json | Set-Content `
        -LiteralPath (Join-Path $setupRoot '06-frozen-save.json') `
        -Encoding UTF8
}
finally
{
    if (-not [string]::IsNullOrEmpty($setupSession))
    {
        Stop-RwaSession -SessionId $setupSession `
            -Path (Join-Path $setupRoot '99-stop.json')
        Write-SessionProvenance -ManifestPath $setupManifestPath `
            -OutputPath $setupRoot `
            -Phase '99-final'
    }
}

$frozenSave = Join-Path $OutputRoot "$saveName.rws"
foreach ($trial in $protocol.schedule)
{
    $trialName = '{0:D2}-{1}' -f [int]$trial.trial, [string]$trial.arm
    $trialRoot = Join-Path $OutputRoot $trialName
    [System.IO.Directory]::CreateDirectory($trialRoot) | Out-Null
    $isolationEvidence = Join-Path $trialRoot '00-isolation-snapshots.json'
    $null = Wait-ForBenchmarkSlot -EvidencePath $isolationEvidence
    $trialSession = $null
    $trialManifestPath = $null
    $trialSummary = $null
    try
    {
        $start = Invoke-BenchmarkStart -IsolationEvidencePath $isolationEvidence `
            -RwaArguments @(
            'start', '-Mode', 'Benchmark', '-KeepAlive', '-DisableIdleCleanup',
            '-HarnessPackagePath', $HarnessPackagePath,
            '-Version', '1.6', '-Owner', 'MechMusterBenchmark',
            '-ModPath', $PackagePath,
            '-AdditionalModPathsFile', $additionalPathsFile,
            '-SeedSavePaths', $frozenSave,
            '-Purpose', "MechMuster 250-tick benchmark $trialName")
        $trialSession = [string]$start.SessionId
        $trialManifestPath = [string]$start.Session
        Write-RawJson -Value $start -Path (Join-Path $trialRoot '01-start.json')
        Write-SessionProvenance -ManifestPath $trialManifestPath `
            -OutputPath $trialRoot `
            -Phase '01-running'
        Assert-OwnedBenchmarkIsolation -SessionId $trialSession `
            -Path (Join-Path $trialRoot '01-isolation-after-start.json') `
            -Phase "$trialName start" | Out-Null
        Invoke-RecordedCommand -SessionId $trialSession `
            -Command "dev-run load-save $saveName --pause-after-load" `
            -Path (Join-Path $trialRoot '02-load.json') | Out-Null
        Start-Sleep -Seconds 15
        Assert-OwnedBenchmarkIsolation -SessionId $trialSession `
            -Path (Join-Path $trialRoot '02-isolation-after-load.json') `
            -Phase "$trialName loaded parity state" | Out-Null

        $automation = if ($trial.arm -eq 'candidate') { 'true' } else { 'false' }
        Invoke-RecordedCommand -SessionId $trialSession `
            -Command "dev-run mod-setting CoolNether123.MechMuster GlobalAutomationEnabled $automation persist" `
            -Path (Join-Path $trialRoot '03-automation-setting.json') | Out-Null
        Invoke-RecordedCommand -SessionId $trialSession `
            -Command 'dev-run mod-setting CoolNether123.MechMuster ShowAssignmentMessages false persist' `
            -Path (Join-Path $trialRoot '04-message-setting.json') | Out-Null
        Invoke-RecordedCommand -SessionId $trialSession `
            -Command 'debug-action "Report 250-tick benchmark workload" category="Mech Muster"' `
            -Path (Join-Path $trialRoot '05-before-report.json') | Out-Null
        Invoke-RecordedCommand -SessionId $trialSession `
            -Command 'dev-run measure-ticks 10000' `
            -Path (Join-Path $trialRoot '06-warmup.json') | Out-Null
        Assert-OwnedBenchmarkIsolation -SessionId $trialSession `
            -Path (Join-Path $trialRoot '06-isolation-after-warmup.json') `
            -Phase "$trialName post-warmup" | Out-Null

        $probeStart = Invoke-RecordedCommand -SessionId $trialSession `
            -Command 'dev-run performance-probe start' `
            -Path (Join-Path $trialRoot '07-performance-start.json')
        if ([string]$probeStart.Responses[0].Output -notmatch
            '^performanceProbe\.running=true$')
        {
            throw "$trialName did not start the performance probe."
        }

        $measurementBatches = @()
        for ($batch = 1; $batch -le 5; $batch++)
        {
            $batchPath = Join-Path $trialRoot `
                ('08-measure-batch-{0:D2}.json' -f $batch)
            $batchResult = Invoke-RecordedCommand -SessionId $trialSession `
                -Command 'dev-run measure-ticks 10000' -Path $batchPath
            $batchOutput = [string]$batchResult.Responses[0].Output
            if ($batchOutput -notmatch '(?m)^ranTicks=10000\r?$' -or
                $batchOutput -notmatch
                    '(?m)^ticksPerSecond=[0-9.]+\r?$')
            {
                throw "$trialName measurement batch $batch was incomplete."
            }
            $measurementBatches += $batchResult
        }

        $probeStop = Invoke-RecordedCommand -SessionId $trialSession `
            -Command 'dev-run performance-probe stop' `
            -Path (Join-Path $trialRoot '09-performance-stop.json')
        $probeOutput = [string]$probeStop.Responses[0].Output
        $requiredProbeFields = @(
            'running=False',
            'ticks=50000',
            'tps=',
            'wallMillisecondsPerTick=',
            'processCpuMilliseconds=',
            'processCpuMillisecondsPerTick=',
            'processCpuCoreUtilizationPercent=',
            'workingSetDelta=',
            'privateBytesDelta=',
            'memoryDelta=',
            'gc0=',
            'gc1=',
            'gc2=')
        foreach ($field in $requiredProbeFields)
        {
            if ($probeOutput -notmatch
                ('(?m)^performanceProbe\.' + [Regex]::Escape($field)))
            {
                throw "$trialName performance probe omitted $field."
            }
        }
        Assert-OwnedBenchmarkIsolation -SessionId $trialSession `
            -Path (Join-Path $trialRoot '09-isolation-after-measurement.json') `
            -Phase "$trialName post-measurement" | Out-Null
        Invoke-RecordedCommand -SessionId $trialSession `
            -Command 'debug-action "Report 250-tick benchmark workload" category="Mech Muster"' `
            -Path (Join-Path $trialRoot '10-after-report.json') | Out-Null
        $parity = Invoke-RecordedCommand -SessionId $trialSession `
            -Command 'logs source=game tail=120 filter=benchmarkReport stack=false' `
            -Path (Join-Path $trialRoot '11-behavior-parity.json')
        $parityOutput = [string]$parity.Responses[0].Output
        $automationState = if ($trial.arm -eq 'candidate') { 'true' } else { 'false' }
        $expectedReport =
            'benchmarkReport=complete plans=8 targets=8 unassignedMechs=64 ' +
            'assignedMechs=0 globalAutomation=' + $automationState
        $reports = @($parityOutput -split "`r?`n" | Where-Object {
                $_ -match 'benchmarkReport='
            })
        if ($reports.Count -ne 2 -or
            @($reports | Where-Object { $_ -notlike "*$expectedReport*" }).Count -ne 0)
        {
            throw "$trialName failed the before/after behavior parity gate."
        }

        $trialSummary = [PSCustomObject]@{
            Trial = [int]$trial.trial
            Block = [int]$trial.block
            Position = [int]$trial.position
            Arm = [string]$trial.arm
            SessionId = $trialSession
            FrozenSaveSha256 =
                (Get-FileHash -LiteralPath $frozenSave -Algorithm SHA256).Hash
            PayloadSha256 = $payloadHash
            FixtureAssemblySha256 = $fixtureHash
            MeasurementBatches = $measurementBatches.Count
            MeasurementTicks = 50000
            ExpectedRecurringInvocations = 200
            BehaviorParity = $true
        }
    }
    finally
    {
        if (-not [string]::IsNullOrEmpty($trialSession))
        {
            Stop-RwaSession -SessionId $trialSession `
                -Path (Join-Path $trialRoot '99-stop.json')
            Write-SessionProvenance -ManifestPath $trialManifestPath `
                -OutputPath $trialRoot -Phase '99-final'
        }
    }

    # The immutable final Player.log is captured by the canonical stop path.
    # Gate on that copy instead of issuing one more live command after all
    # measurements; this avoids a canonical lease heartbeat/reaper file race
    # without weakening the exception check or retrying a mutating command.
    $finalPlayerLog = Join-Path $trialRoot '99-final-Player.log'
    if (-not (Test-Path -LiteralPath $finalPlayerLog -PathType Leaf))
    {
        throw "$trialName did not capture its final Player.log."
    }
    $exceptionMatches = @(Select-String -LiteralPath $finalPlayerLog `
        -Pattern 'Exception' -CaseSensitive:$false)
    [PSCustomObject]@{
        Source = $finalPlayerLog
        SourceSha256 =
            (Get-FileHash -LiteralPath $finalPlayerLog -Algorithm SHA256).Hash
        Pattern = 'Exception'
        MatchCount = $exceptionMatches.Count
        Matches = @($exceptionMatches | ForEach-Object {
                [PSCustomObject]@{
                    LineNumber = $_.LineNumber
                    Line = $_.Line
                }
            })
        Passed = $exceptionMatches.Count -eq 0
    } | ConvertTo-Json -Depth 5 | Set-Content `
        -LiteralPath (Join-Path $trialRoot '12-exceptions-gate.json') `
        -Encoding UTF8
    if ($exceptionMatches.Count -ne 0)
    {
        throw "$trialName logged an exception."
    }
    $trialSummary | ConvertTo-Json | Set-Content `
        -LiteralPath (Join-Path $trialRoot 'trial.json') -Encoding UTF8
}

[PSCustomObject]@{
    SchemaVersion = 1
    Complete = $true
    OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
    Trials = @($protocol.schedule).Count
    PayloadSha256 = $payloadHash
    FixtureAssemblySha256 = $fixtureHash
    CanonicalHarnessCommit = $harnessCommit
    HarnessPackageFingerprint = $expectedHarnessFingerprint
} | ConvertTo-Json -Depth 5
