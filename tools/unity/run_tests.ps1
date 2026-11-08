# Runs one set of Unity tests in batch mode and prints a short summary (Windows PowerShell 5.1 and later).
# The sets and when to run each: unity/Assets/Plans/TEST_RUNS.md. The same sets as run_tests.sh.
#
#   tools\unity\run_tests.ps1 world          # from the repository's root
#   ..\tools\unity\run_tests.ps1 slice       # from unity\
#   tools\unity\run_tests.ps1 slice -testFilter GameDev.Slice.Tests.TownsfolkTests   # extra Unity arguments pass through
#
# Unity must be closed: two editors cannot open one project. $env:UNITY_EXE overrides the editor's path.
param(
    [ValidateSet('slice', 'slice-play', 'world', 'slow', 'all')]
    [string]$Set = 'slice',
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Extra = @()
)

$unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe'
if ($env:UNITY_EXE) { $unity = $env:UNITY_EXE }
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..\unity')).Path

switch ($Set) {
    'slice'      { $setArgs = @('-testPlatform', 'EditMode', '-assemblyNames', 'GameDev.Slice.Tests.EditMode') }
    'slice-play' { $setArgs = @('-testPlatform', 'PlayMode', '-assemblyNames', 'GameDev.Slice.Tests.PlayMode', '-testCategory', '!Diagnostic') }
    'world'      { $setArgs = @('-testPlatform', 'EditMode', '-assemblyNames', 'Assembly-CSharp-Editor', '-testCategory', '!Slow') }
    'slow'       { $setArgs = @('-testPlatform', 'EditMode', '-testCategory', 'Slow') }
    'all'        { $setArgs = @('-testPlatform', 'EditMode') }
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$results = Join-Path $project "Logs\tests_${Set}_$stamp.xml"
$log = Join-Path $project "Logs\tests_${Set}_$stamp.log"

# Start-Process joins the arguments with spaces, so paths are quoted here.
$all = @('-batchmode', '-nographics', '-projectPath', "`"$project`"", '-runTests') + $setArgs +
       @('-testResults', "`"$results`"", '-logFile', "`"$log`"") + $Extra

Write-Host "Running '$Set' in $project ..."
$watch = [Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath $unity -ArgumentList $all -Wait -PassThru -NoNewWindow
Write-Host ("set: {0}, exit {1}, {2:0} s" -f $Set, $process.ExitCode, $watch.Elapsed.TotalSeconds)
Write-Host "log: $log"

if (Test-Path $results) {
    [xml]$xml = Get-Content -Raw $results
    $run = $xml.'test-run'
    Write-Host ("total {0}, passed {1}, failed {2}, skipped {3}" -f $run.total, $run.passed, $run.failed, $run.skipped)
    foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
        Write-Host "FAILED $($case.fullname)" -ForegroundColor Red
    }
}
else {
    Write-Host 'No results file: see the log (compile errors, or the project is open in another editor).' -ForegroundColor Yellow
}

exit $process.ExitCode
