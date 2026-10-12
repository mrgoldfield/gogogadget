# Changes from upstream LibreHardwareMonitor

This fork is based on [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) at commit
[`82bc3bd`](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/commit/82bc3bdce4bdd819c451a0db832daef9ebfcaca2)
(2026-09-13, "Support multiple Arctic fan controllers"), and synced with upstream
up to [`54f4022`](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/commit/54f4022)
(2026-10-12). Everything not listed below is unmodified upstream code.

**How to sync with upstream:** the fork's git history is not related to
upstream's (it started from a snapshot), so `git merge` refuses. Apply the
upstream diff since the last sync point instead, on a branch:
`git fetch upstream && git diff --binary <last sync> upstream/master > up.patch && git apply --3way up.patch`,
resolve conflicts (keep the fork's side in fork-rewritten code, then port
upstream's intent by hand), build + test + screenshots, and update the sync
commit above and the watch list below.

For day-to-day changes as the fork's own features ship, see [CHANGELOG.md](CHANGELOG.md).
This file tracks the *structural* differences from upstream instead — the
things a future rebase onto a newer upstream commit needs to watch for.

## Rebranding (Phase 0)

Product name, window title, tray tooltip, about box, HTTP server auth realm,
and the built assembly name changed from "Libre Hardware Monitor" /
`LibreHardwareMonitor.Windows.Forms` to "GoGoGadget Hardware Monitor" /
`GoGoGadgetHardwareMonitor`. **C# namespaces were deliberately left as
`LibreHardwareMonitor.*`** to keep future upstream merges simple — this is a
product/branding change, not a code reorganization.

Files touched:
- `LibreHardwareMonitor.Windows.Forms/LibreHardwareMonitor.Windows.Forms.csproj` — `AssemblyName`, `AssemblyTitle`, `Product`, `Copyright`
- `LibreHardwareMonitor.Windows.Forms/UI/MainForm.Designer.cs` — window title
- `LibreHardwareMonitor.Windows.Forms/UI/SystemTray.cs` — tray tooltip
- `LibreHardwareMonitor.Windows.Forms/UI/AboutBox.Designer.cs` — about box title
- `LibreHardwareMonitor.Windows.Forms/Utilities/HttpServer.cs` — HTTP Basic Auth realm string
- `Directory.Build.props` — version scheme (see below)

## Autostart / logging collision fix

`StartupManager.cs` used `nameof(LibreHardwareMonitor)` (the namespace) as
the literal name for its Registry Run-key value **and** its Windows
Scheduled Task, for both toggling and detecting whether autostart is
enabled. Since the namespace was intentionally left unchanged, this would
have made the fork's autostart entry collide with a real LibreHardwareMonitor
install's own autostart entry on the same machine — enabling one could
silently overwrite or misreport the other's state. Replaced with an explicit
`AppRegistryValueName = "GoGoGadgetHardwareMonitor"` constant instead.

`Logger.cs`'s CSV log filename prefix was similarly changed from
`LibreHardwareMonitorLog-*.csv` to `GoGoGadgetHardwareMonitorLog-*.csv` so
logs from both apps don't interleave/collide if pointed at the same folder.

## Versioning

`Directory.Build.props` version bumped from the literal `0.9.6` (which
would have been misleading — this fork's base commit is well past the
`v0.9.6` tag, including several unreleased upstream features) to
`0.9.7-gogogadget.0`, signaling "based on progress toward upstream's next
release, fork build 0 (nothing of the fork's own shipped yet)". CI's
existing auto-bump-on-push behavior (`master.yml` / `pull requests.yml`)
is unchanged.

## CI

`master.yml` / `pull requests.yml`: branch trigger changed from `master` to
`main` (this fork's default branch), and the app build's uploaded-artifact
label renamed to match the new assembly name.

**2026-09-28: dropped from 6 uploaded artifacts per run to 1.** Both
workflows used to also build and upload `LibreHardwareMonitorLib`
separately for x64/x86/ARM64 plus a packed nupkg (4 more artifacts,
upstream's own NuGet-publishing steps) and both the net472 and
net10.0-windows app builds (2 artifacts). This fork never publishes to
NuGet (`master.yml`'s `Publish to NuGet` step was already gated to
`github.repository == 'LibreHardwareMonitor/LibreHardwareMonitor'` and
never ran here) and only ever installs/runs the net10.0-windows app
build (see CONTRIBUTING.md's "Building" section) — so the
library-specific build+upload steps were removed entirely, and only the
net10.0-windows app upload remains (net472 is still built as part of the
existing "Build application" step, for the net472/net10.0-windows
compatibility gate that build serves as, just no longer uploaded). This
was a deliberate departure from the original fork plan's "keep
upstream's existing CI build matrix, don't rebuild it from scratch"
guidance — done because 6 artifacts/run at the default 90-day retention
had filled the account's entire GitHub Actions storage quota (500MB) and
blocked Actions from running at all. The remaining artifact also sets
`retention-days: 30` instead of the default 90.

**Later the same day: back to 2 artifacts (x64 + ARM64).** The app
itself had never been built for anything but x64 (matching upstream's
own app-build step, which is also x64-only — the removed multi-platform
matrix above was only ever for the *library*, for NuGet consumers).
Added a native ARM64 build for Windows-on-ARM devices, net10.0-windows
only (no real native ARM64 .NET Framework runtime, so net472 stays
x64-only). Required widening `LibreHardwareMonitor.Windows.Forms.csproj`'s
`OutputPath` to include `$(Platform)` (previously just
`bin\$(Configuration)\`, unlike `LibreHardwareMonitorLib`'s own
`OutputPath` which already did this) — without that, an ARM64 build
would land in the exact same folder as the x64 one and silently
overwrite it before either got published.

## Screenshot mode (test tooling)

`--screenshot <folder>` makes the app save its windows and gadget as PNGs
and exit (see `Utilities/SelfGrab.cs`; driven by `scripts/shots.sh`). New
fork files: `Utilities/SelfGrab.cs`, `UI/Gadget.SelfGrab.cs`. Hooks in
upstream files, each marked `GoGoGadget:` (re-apply these after a rebase):
- `Program.cs` - `Main(string[] args)` instead of `Main()`, and one
  `SelfGrab.Init(args)` line right after `SetCompatibleTextRenderingDefault`.
- `UI/Gadget.cs` - `partial` on the class, and one `SelfGrab.Track(this)`
  line in the constructor.

Main-window changes in general (Goldfield, 2026-10-08) follow the same
pattern: logic in fork files, only small marked hooks in upstream files,
each listed here.

## Settings and logs in %LOCALAPPDATA%

Upstream writes its `.config` next to the exe. The fork keeps settings in
`%LOCALAPPDATA%\GoGoGadget\UserSettings` and logs in `...\log`, or next to
the exe when `portable.txt` is there (`Utilities/AppPaths.cs`, fork file).
File → Open Settings Folder is added at runtime by `UI/SettingsFolderMenu.cs`.
Hooks:
- `UI/MainForm.cs` - one `SettingsFolderMenu.Attach(...)` line after
  `InitializeComponent()` in the constructor.

## Autostart follows the app to a new folder

`StartupManager` stores the full exe path in its Scheduled Task (or Run
registry value), and each release is unzipped into a new folder. When the
stored exe no longer exists, the entry is re-created for the running exe
(`UI/StartupRepoint.cs`, fork file); an entry pointing to an exe that still
exists is left alone. Screenshot runs skip it.
Hooks:
- `UI/StartupManager.cs` - two marked lines in the constructor (task branch
  and registry branch).

## About box links open again

On .NET 10 (unlike .NET Framework 4.7.2) `ProcessStartInfo.UseShellExecute`
defaults to `false`, so `Process.Start(url)` threw and the empty `catch`
hid it: none of the About box links did anything. Upstream has the same
handler.
Hooks:
- `UI/AboutBox.cs` - `LinkLabel_LinkClicked`, one marked line.

## Upstream update watch list

What can break, or needs a manual look, when merging/rebasing a newer
upstream. Check each item after the merge builds; `grep -rn "GoGoGadget:"`
lists every marked hook line.

**Marked hook lines in upstream files** (re-apply if a conflict drops them):
- `Program.cs` - `Main(string[] args)` and `SelfGrab.Init(args)` (screenshot mode).
- `UI/Gadget.cs` - `partial` on the class, `SelfGrab.Track(this)` in the constructor.
- `UI/MainForm.cs` - `SettingsFolderMenu.Attach(...)` after `InitializeComponent()`.
- `UI/MainForm.cs` (`HardwareAdded`/`HardwareRemoved`), `UI/SensorGadget.cs`, `UI/SystemTray.cs`,
  `UI/HardwareNode.cs` (`SensorAdded`/`SensorRemoved`) - `UiThread.Post(...)` instead of upstream's
  `BeginInvoke` (#2452 crashed on start: BeginInvoke before MainForm's handle exists).
  If upstream fixes this itself, take theirs and delete `UI/UiThread.cs`.
- `UI/StartupManager.cs` - two `StartupRepoint` lines in the constructor.
- `UI/AboutBox.cs` - `UseShellExecute = true` in `LinkLabel_LinkClicked`.

**Upstream files the fork changed heavily** (conflicts here need hand work,
not just re-applying a line):
- `UI/SensorGadget.cs` - most of the file is fork code (layout, colours,
  thresholds, Mini/Full, icons, menus). Any upstream change to it has to be
  ported by hand, especially value formatting (see below).
- `UI/GadgetWindow.cs` - resize/drag handling (`WM_WINDOWPOSCHANGING/CHANGED`,
  `WM_GETMINMAXINFO`, `_inProgrammaticResize`).
- `UI/MainForm.Designer.cs` - window title, Help -> Startup Guide item.
  Upstream adds menu items here too: merge both item lists.
- `UI/AboutBox.Designer.cs` / `AboutBox.cs` - fork link label.
- `UI/StartupManager.cs` - `AppRegistryValueName` instead of `nameof(...)`.
- `Utilities/Logger.cs` (log file prefix), `Utilities/HttpServer.cs` (auth realm),
  `UI/SystemTray.cs` (tooltip), `Utilities/PersistentSettings.cs` (`GetAll()`),
  the `.csproj` (assembly name/product) and `Resources/app.manifest`.
- Several upstream files only gained the MPL header (first lines). A
  conflict there is trivial: keep the header.

**Fork fixes upstream may make itself** (if upstream fixes it, keep
upstream's version and drop ours plus its CHANGES.md section):
- About box `UseShellExecute`.

**Already waiting upstream:** nothing (synced to `54f4022`, 2026-10-12).
Check with `git log 54f4022..upstream/master -- LibreHardwareMonitor.Windows.Forms`
(app) and the library commits for anything that changes sensor values or units.

**Synced 2026-10-12 (7 upstream commits, `7615073..54f4022`):** `UI/MainForm.cs`
conflicted: upstream's hibernation fix (#2452: reset after resume on a worker
thread after 3 s with the timer off, tree/gadget/tray changes marshalled to the UI
thread) replaces the fork's own wake fix, which was dropped. `UI/SensorGadget.cs`:
kept the fork's tinted bars, added upstream's progress clamp (#2564) by hand.
#2568 (drive first reading) was picked a day early in `97e4d0f`.

**Synced 2026-10-10 (15 upstream commits, `82bc3bd..7615073`):** only
`UI/SensorGadget.cs` conflicted (upstream's gadget paint loop vs the fork's
rewrite: kept the fork's). Ported by hand: #2533 Data sensors now report
bytes and `SmallData` is gone, so the fork's value formatting uses
`UnitManager.BytesToString` / `BytesPerSecondToString` like upstream.

## Not changed, and why

- **NuGet package identity** (`LibreHardwareMonitorLib`) — this fork doesn't
  publish its own package; renaming it would break nothing here but would
  be pure churn.
- **C# namespaces, file/folder layout inside `LibreHardwareMonitor.Windows.Forms/`**
  — kept as-is to keep future `git merge`/rebase from upstream tractable.
- **`icon.ico` / `smallicon.ico`** — still upstream's icon as a placeholder.
  Custom branding icon is a known gap, not yet done.
