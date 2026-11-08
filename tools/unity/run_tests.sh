#!/usr/bin/env bash
# Runs one set of Unity tests in batch mode and prints a short summary.
# The sets and when to run each: unity/Assets/Plans/TEST_RUNS.md. In PowerShell use run_tests.ps1.
#
#   tools/unity/run_tests.sh slice        # the slice's EditMode tests (the default)
#   tools/unity/run_tests.sh slice-play   # the slice's PlayMode tests
#   tools/unity/run_tests.sh world        # the world's EditMode tests, without the Slow ones
#   tools/unity/run_tests.sh slow         # only the tests marked [Category("Slow")]
#   tools/unity/run_tests.sh all          # every EditMode test
#   tools/unity/run_tests.sh slice -testFilter GameDev.Slice.Tests.TownsfolkTests   # extra Unity arguments pass through
#
# Unity must be closed: two editors cannot open one project. UNITY_EXE overrides the editor's path.
set -u

set_name="${1:-slice}"
[ $# -gt 0 ] && shift
unity="${UNITY_EXE:-/c/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe}"
project="$(cd "$(dirname "$0")/../../unity" && pwd)"

case "$set_name" in
  slice)      args=(-testPlatform EditMode -assemblyNames GameDev.Slice.Tests.EditMode) ;;
  slice-play) args=(-testPlatform PlayMode -assemblyNames GameDev.Slice.Tests.PlayMode -testCategory "!Diagnostic") ;;
  world)      args=(-testPlatform EditMode -assemblyNames Assembly-CSharp-Editor -testCategory '!Slow') ;;
  slow)       args=(-testPlatform EditMode -testCategory Slow) ;;
  all)        args=(-testPlatform EditMode) ;;
  *) echo "unknown set '$set_name': slice | slice-play | world | slow | all"; exit 2 ;;
esac

stamp="$(date +%Y%m%d-%H%M%S)"
results="$project/Logs/tests_${set_name}_$stamp.xml"
log="$project/Logs/tests_${set_name}_$stamp.log"
started=$(date +%s)
"$unity" -batchmode -nographics -projectPath "$project" -runTests "${args[@]}" -testResults "$results" -logFile "$log" "$@"
code=$?
echo "set: $set_name, exit $code, $(( $(date +%s) - started )) s"
echo "log: $log"
if [ -f "$results" ]; then
  grep -oE '<test-run [^>]*' "$results" | grep -oE '(total|passed|failed|skipped)="[0-9]+"' | tr '\n' ' '
  echo
  grep -oE '<test-case [^>]*result="Failed"[^>]*' "$results" | grep -oE 'fullname="[^"]*"' | sed 's/^/FAILED /'
else
  echo "no results file: see the log (compile errors, or the project is open in another editor)"
fi
exit $code
