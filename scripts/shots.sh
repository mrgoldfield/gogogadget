#!/usr/bin/env bash
# Screenshot test: send this working tree to a Windows machine, build it
# there, run every scenario in scripts/shot-scenarios.json with
# --screenshot, fetch the PNGs into output/shots/ and make a contact sheet.
#
#   scripts/shots.sh [--no-build] [scenario ...]
#
# Needs WIN_HOST (ssh target of the Windows machine, key auth) in .env
# or the environment. The Windows side needs only the .NET 10 SDK and
# its built-in tar (no git). Contact sheet needs python3 + Pillow.
set -euo pipefail
cd "$(dirname "$0")/.."
[ -f .env ] && . ./.env
: "${WIN_HOST:?set WIN_HOST in .env (ssh target of the Windows test machine)}"
WIN_DIR="${WIN_DIR:-gogogadget-shots}"
SSH=(ssh -o BatchMode=yes -o ConnectTimeout=10 "$WIN_HOST")
start=$(date +%s)

ps_args=()
only=()
for a in "$@"; do
  case "$a" in
    --no-build) ps_args+=(-NoBuild) ;;
    *) only+=("$a") ;;
  esac
done
[ ${#only[@]} -gt 0 ] && ps_args+=(-Only "$(IFS=,; echo "${only[*]}")")

# (cmd: 'if cond a & b' would put b inside the if, so plain mkdir 2>nul.)
# 1. Send the tree (tracked + new files, nothing git-ignored). Extracted
#    over the previous copy so bin/obj stay warm; a file deleted here
#    lingers there until the src folder is wiped.
git ls-files -co --exclude-standard -z | tar --null -T - -cf - |
  "${SSH[@]}" "mkdir %USERPROFILE%\\$WIN_DIR\\src 2>nul & tar -xf - -C %USERPROFILE%\\$WIN_DIR\\src"
echo "sent $(( $(date +%s) - start )) s"

# 2. Build + shoot on Windows.
rc=0
"${SSH[@]}" "powershell -NoProfile -ExecutionPolicy Bypass -File %USERPROFILE%\\$WIN_DIR\\src\\scripts\\shots.ps1 ${ps_args[*]}" || rc=$?

# 3. Fetch the PNGs, then the contact sheet.
rm -rf output/shots && mkdir -p output/shots
"${SSH[@]}" "tar -cf - -C %USERPROFILE%\\$WIN_DIR\\out ." | tar -xf - -C output/shots
python3 scripts/contact_sheet.py output/shots output/shots/contact-sheet.png
echo "rc=$rc $(( $(date +%s) - start )) s"
exit $rc
