# Porting xTrades to Windows with Claude

xTrades is built on C#/.NET 10 with Avalonia and SkiaSharp, which work identically on Windows. The vast majority of
the code (data, feeds, charts, backtesting, strategies, indicators) is platform-agnostic. The macOS-specific parts
are in a small, well-identified set of files. This guide gives Claude Code everything it needs to do the work on a
Windows PC: an inventory of what needs changing, a stage-by-stage plan, and the full prompt.

> Requirements: Windows 10/11 64-bit (x64 or ARM64), [Git](https://git-scm.com),
> [.NET 10 SDK](https://dotnet.microsoft.com/download), and [Claude Code](https://claude.com/claude-code) signed in
> (`npm install -g @anthropic-ai/claude-code` or the official installer).

## What depends on macOS (inventory)

| File | What it does on macOS | What to do on Windows |
|---|---|---|
| `build-app.sh` | Publishes for `osx-arm64`, builds `Trading Hub.app`, signs with `codesign`, copies with `ditto` | A `build-windows.ps1` that publishes `win-x64` (and `win-arm64`) self-contained with an `.ico` icon; optionally an installer (Inno Setup or MSIX) |
| `src/TradingHub.App/Services/Connections.cs` (`Keychain` class) | Stores credentials in Keychain via `/usr/bin/security` | Windows Credential Manager (`CredWrite`/`CredRead`/`CredDelete` from `advapi32.dll`) or DPAPI (`ProtectedData`) with a per-user encrypted file. Keep the same `Get`/`Set` interface. |
| `src/TradingHub.App/Services/SleepGuard.cs` | Prevents sleep (IOKit `IOPMAssertion`) and with lid closed, `pmset` via `osascript` with admin privileges | `SetThreadExecutionState(ES_CONTINUOUS \| ES_SYSTEM_REQUIRED)` from `kernel32.dll`. Hide the lid-closed option on Windows or replace it with an explanation about power settings. |
| `src/TradingHub.App/Program.cs` | `CGGetActiveDisplayList` (waits for a display before starting) | Skip that wait on Windows (`OperatingSystem.IsMacOS()`) |
| `src/TradingHub.App/Services/MenuBar.cs` | Menu bar icon (Avalonia `TrayIcon`) and mouse position via `objc_msgSend` | `TrayIcon` works in the Windows system tray; mouse position from `GetCursorPos` (`user32.dll`), panel opens above the taskbar |
| `src/TradingHub.App/Windows/MainWindow.cs` (`Shell.OpenUrl`, `OpenFolder`…) and `Views/StrategiesView.Reports.cs` | Opens links, folders, and reports with `open` | `Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })`; to show a file in its folder, `explorer.exe /select,"path"` |
| `src/TradingHub.App/Views/TradingView.cs` | Sound playback with `afplay` | `System.Media.SoundPlayer` (WAV) or an Avalonia audio player; keep the same sound files |
| `src/TradingHub.App/Services/ClaudeCode.cs` | Finds the `claude` CLI in Mac paths, sets `PATH` with `/usr/local/bin`, `/opt/homebrew/bin`…, opens Terminal via `osascript` for `/login` | Find `claude.exe`/`claude.cmd` in `%USERPROFILE%\.local\bin`, `%APPDATA%\npm`, `%LOCALAPPDATA%\Programs`, and `PATH` (via `where claude`); open `cmd.exe /k claude /login` or Windows Terminal (`wt.exe`) |
| `MainWindow` (title bar) | Navigation lives inside the macOS title bar with traffic lights on the left | On Windows, window buttons go on the right: leave space on the right instead of the left, and verify that dragging and maximizing work |
| Shortcuts (`KeyModifiers.Meta`, ~30 uses) | ⌘ | `Ctrl` on Windows: a `Keys.Command` helper that returns `Meta` on Mac and `Control` on Windows; help text should say "Ctrl" |
| Fonts (`"Menlo, monospace"`, ~12 uses) | Mac monospace font | `"Cascadia Mono, Consolas, monospace"` on Windows (a shared `Ui.MonoFont` constant) |
| `TradingHub.Common.Product` | Data in `~/Library/Application Support/Trading Hub`, files in `~/Documents/Trading Hub` | Already uses `Environment.SpecialFolder`: on Windows this becomes `%APPDATA%\Trading Hub` and `Documents\Trading Hub` with no code changes. Check comments and UI text that say "Mac". |
| UI text | Mentions "the Mac", "macOS Keychain", "menu bar", ⌘Q | Platform-specific text (`OperatingSystem.IsWindows()`): "the PC", "Credential Manager", "system tray", Alt+F4 |

Everything else — Rithmic, Tradovate, Binance, Bybit, OANDA, Alpaca, cTrader, Databento, the backtest engine, walk-
forward, Skia charts, Roslyn strategy compiler, and the MCP lab — is cross-platform.

## Stage plan

1. **Get it building and running.** `dotnet build TradingHub.slnx` and `dotnet test tests/TradingHub.Tests` on Windows.
   Isolate Mac-specific code behind `OperatingSystem.IsMacOS()` or behind small interfaces (`ISecretStore`,
   `ISleepGuard`, `IShell`) with two implementations, without breaking the Mac build.
2. **Credential store.** Windows Credential Manager with tests that don't touch the user's real credentials (like on
   Mac, an env var points to a test file).
3. **System integrations.** Sleep guard, system tray, opening links and folders, sounds, Claude Code CLI.
4. **UI.** Title bar, Ctrl shortcuts, fonts, platform-specific text. Verify each section with headless scenes
   (`dotnet run --project tools/TradingHub.UiSnapshot -- <folder> 4 <scene>`).
5. **Packaging.** `build-windows.ps1` with self-contained publish and icon, and optionally an installer.
6. **Verification.** Unit tests, scenes, and a manual test with the simulated account and free Binance data.

## Prompt for Claude Code

Clone the repository on the Windows PC, open a terminal in that folder, run `claude`, and paste this:

```
I want to port xTrades to Windows without breaking the macOS build. It is a C#/.NET 10 desktop app
with Avalonia and SkiaSharp (solution TradingHub.slnx). Read CLAUDE.md, README.md, docs/development.md,
and WINDOWS_PORT.md first (it has the inventory of macOS-specific code and the stage plan).

Rules:
- The macOS build must keep compiling and working. Isolate platform-specific code with
  OperatingSystem.IsMacOS()/IsWindows(), or preferably with small interfaces (ISecretStore, ISleepGuard,
  IShell, ISound) and one implementation per platform. No copying whole files.
- Never store credentials, tokens, or passwords in text files: on Windows use Credential Manager
  (advapi32 CredWrite/CredRead/CredDelete) or DPAPI. Tests must not touch real credentials: use an env var
  (like TRADINGHUB_KEYCHAIN_FILE on Mac) pointing to a test store.
- Tools and tests never use the user's real data: headless scenes already use temp folders
  (TRADINGHUB_DATA_DIR, TRADINGHUB_STRATEGIES_DIR, TRADINGHUB_INDICATORS_DIR); keep that.
- Do not add confirmation dialogs that block order button clicks; safety is in risk limits.
- All new text must be in English.

Work stage by stage and after each one build (dotnet build TradingHub.slnx), run tests
(dotnet test tests/TradingHub.Tests), and tell me what changed:
1. Build and start on Windows (Program.cs without the macOS display wait, paths, fonts).
2. Windows credential store behind the same interface as Keychain (Connections.cs).
3. Integrations: SleepGuard with SetThreadExecutionState, system tray (TrayIcon + GetCursorPos to
   position the panel), open links/folders/reports with UseShellExecute, sounds without afplay, and
   Claude Code (find claude.exe or claude.cmd with `where`, %USERPROFILE%\.local\bin and %APPDATA%\npm;
   open /login in cmd or Windows Terminal).
4. UI: title bar with Windows buttons on the right, Ctrl shortcuts (a shared helper instead of
   KeyModifiers.Meta), and platform-specific text (Mac/PC, Keychain/Credential Manager, menu bar/system tray).
5. A build-windows.ps1 that publishes self-contained for win-x64 and win-arm64 with the icon and leaves
   the app ready in a dist\xTrades folder. Optionally an Inno Setup script.
6. Final verification: unit tests, the relevant headless scenes
   (dotnet run --project tools/TradingHub.UiSnapshot -- captures 4 trading connections panel footprint)
   and a list of what to verify manually with the simulated account.

If something can't work the same way as on Mac (e.g., lid-closed mode), hide that option on Windows
and explain it in Settings text. Show real errors if something fails; don't continue as if it worked.
```

## After the port

- Keep tests green on both platforms: a change on one should not break the other.
- Linux follows the same path (Secret Service/libsecret for credentials, `systemd-inhibit` for sleep,
  `xdg-open` for links).
