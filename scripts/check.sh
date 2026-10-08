#!/usr/bin/env bash
# Linux check: build both app targets (cross-targeting Windows) and run the
# unit tests, one line per stage. Then stops the .NET compiler server, which
# otherwise stays resident (~1.3 GB) after a build; set KEEP_BUILD_SERVER=1
# to keep it warm for a series of builds.
#
#   scripts/check.sh
set -uo pipefail
cd "$(dirname "$0")/.."
export PATH="$HOME/.dotnet:$PATH"
APP=LibreHardwareMonitor.Windows.Forms/LibreHardwareMonitor.Windows.Forms.csproj
TESTS=GoGoGadgetHardwareMonitor.Tests/GoGoGadgetHardwareMonitor.Tests.csproj
rc=0
start=$(date +%s)

for tfm in net10.0-windows net472; do
  t=$(date +%s)
  if out=$(dotnet build "$APP" -c Release -p:Platform=x64 -f "$tfm" -p:EnableWindowsTargeting=true -nologo -v:q 2>&1); then
    echo "build $tfm ok $(( $(date +%s) - t )) s"
  else
    echo "build $tfm FAILED"
    grep -E ' error ' <<<"$out" | sort -u | head -20
    rc=1
  fi
done

t=$(date +%s)
if out=$(dotnet test "$TESTS" -p:EnableWindowsTargeting=true -nologo -v:q 2>&1); then
  echo "tests ok $(grep -oE 'Passed: +[0-9]+' <<<"$out" | head -1 | tr -s ' ') $(( $(date +%s) - t )) s"
else
  echo "tests FAILED"
  grep -E 'Failed |error' <<<"$out" | head -20
  rc=1
fi

[ "${KEEP_BUILD_SERVER:-0}" = 1 ] || dotnet build-server shutdown >/dev/null 2>&1
echo "rc=$rc $(( $(date +%s) - start )) s"
exit $rc
