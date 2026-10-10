# Changes from upstream LibreHardwareMonitor

This fork is based on [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) at commit
[`82bc3bd`](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/commit/82bc3bdce4bdd819c451a0db832daef9ebfcaca2)
(2026-09-13, "Support multiple Arctic fan controllers"). Everything not listed
below is unmodified upstream code.

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

## Wake-from-sleep reset on the UI thread

Upstream resets all hardware in `MainForm.PowerModeChanged` on Windows'
system-events thread. The tree view rebuilds its row list on the UI thread
at the same time and can hit a node that was just removed
(`ArgumentOutOfRangeException` in `TreeViewAdv.CreateRowMap`, seen after
waking the PC). The reset is now queued to the UI thread with
`BeginInvoke`.

Hooks:
- `LibreHardwareMonitor.Windows.Forms/UI/MainForm.cs` — `PowerModeChanged`, one marked line

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

## Not changed, and why

- **NuGet package identity** (`LibreHardwareMonitorLib`) — this fork doesn't
  publish its own package; renaming it would break nothing here but would
  be pure churn.
- **C# namespaces, file/folder layout inside `LibreHardwareMonitor.Windows.Forms/`**
  — kept as-is to keep future `git merge`/rebase from upstream tractable.
- **`icon.ico` / `smallicon.ico`** — still upstream's icon as a placeholder.
  Custom branding icon is a known gap, not yet done.
