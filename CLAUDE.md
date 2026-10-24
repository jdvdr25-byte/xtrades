# xTrades — Claude Code instructions

xTrades is a free, open-source order flow trading platform for Windows and macOS, built in C#/.NET 10 with Avalonia.
Everything runs 100% locally. Based on [Trading Hub](https://github.com/ralphtradesz/trading-hub) by @ralphtradesz.
Always communicate with the user in English.

## Project structure

- `TradingHub.slnx` — solution file (namespace stays `TradingHub.*` throughout)
- `src/TradingHub.Core` — data models, feeds (Rithmic, Tradovate, Databento, Binance, Bybit, OANDA, Alpaca, cTrader), order management
- `src/TradingHub.Sdk` — public indicator/strategy API (ATAS-compatible)
- `src/TradingHub.Charting` — chart engine (footprint, candles, DOM, tape) using SkiaSharp
- `src/TradingHub.Scripting` — Roslyn-based in-app C# strategy compiler
- `src/TradingHub.Strategies` — strategy API, backtesting, walk-forward optimization
- `src/TradingHub.Indicators.Native` — built-in indicators (VWAP, volume profile, footprint, delta, CVD, big trades)
- `src/TradingHub.Indicators.Technical` — technical indicator library (ATAS-compatible)
- `src/TradingHub.App` — Avalonia UI shell, views, windows, services
- `tests/TradingHub.Tests` — test suite
- `tools/TradingHub.UiSnapshot` — headless UI scene runner for visual testing

## Build and test

```bash
dotnet build TradingHub.slnx
dotnet test tests/TradingHub.Tests
```

macOS (current): `./build-app.sh --install`
Windows (in progress — see `WINDOWS_PORT.md`): `build-windows.ps1` once ported

## Headless UI testing

```bash
dotnet run --project tools/TradingHub.UiSnapshot -- <output-folder> 4 <scene>
```

Scenes: `trading`, `connections`, `panel`, `footprint`, `market`, `backtest`

Uses temporary data dirs — never run against the user's real data:
- `TRADINGHUB_DATA_DIR` — market data
- `TRADINGHUB_STRATEGIES_DIR` — strategies
- `TRADINGHUB_INDICATORS_DIR` — indicators

## Data locations

- macOS: `~/Library/Application Support/Trading Hub` and `~/Documents/Trading Hub`
- Windows: `%APPDATA%\Trading Hub` and `Documents\Trading Hub`

Data survives rebuilds. Controlled by `TradingHub.Common.Product`.

## Active milestone: Windows port

See `WINDOWS_PORT.md` for the full inventory of macOS-specific code and the step-by-step port plan.
Files that need platform abstractions:
- `Services/Connections.cs` — Keychain → Windows Credential Manager (`advapi32` CredWrite/CredRead/CredDelete)
- `Services/SleepGuard.cs` — IOKit → `SetThreadExecutionState` (`kernel32`)
- `Program.cs` — skip `CGGetActiveDisplayList` on Windows
- `Services/MenuBar.cs` — tray icon position via `GetCursorPos` (`user32`)
- `MainWindow.cs` / `Views/TradingView.cs` — `open` → `Process.Start(UseShellExecute)`, `afplay` → `SoundPlayer`
- `Services/ClaudeCode.cs` — find `claude.exe`/`claude.cmd` in `%USERPROFILE%\.local\bin`, `%APPDATA%\npm`, PATH
- Keyboard shortcuts: `KeyModifiers.Meta` → `Control` on Windows (use a `Keys.Command` helper)
- Fonts: `"Menlo, monospace"` → `"Cascadia Mono, Consolas, monospace"` on Windows

When making platform-specific changes:
- Use `OperatingSystem.IsWindows()` / `OperatingSystem.IsMacOS()` guards
- Prefer small interfaces (`ISecretStore`, `ISleepGuard`, `IShell`, `ISound`) with per-platform implementations
- Never break the macOS build — both platforms must compile and pass tests

## Active milestone: VP/Delta Profile upgrades (v1)

In priority order:
1. **RTH/ETH session split** — user-configurable session times per instrument; existing `IsNewSession()` uses feed boundaries only
2. **Monthly VP period** — add alongside existing Session/Week/Visible periods in `SessionVolumeProfile.cs`
3. **Developing POC** — real-time POC line drawn on the price chart as the current session builds
4. **Naked POC tracking** — prior session POCs not yet revisited, tracked 20 sessions back, drawn forward
5. **Split bid/ask delta bars** — stacked green (ask) / red (bid) per price level instead of single-color delta bars

## Security rules

- Never write credentials, tokens, or API keys to repo files — use the platform secret store
- Real orders are dangerous: always test with the simulated account
- Never add confirmation dialogs that block order button clicks — safety belongs in risk limits
- Test keychain/credential code with an env var pointing to a test store, not the user's real credentials

## License

MIT + Commons Clause. Free to use, modify, and share. Cannot be sold or used as the basis of a paid service.
Original work by @ralphtradesz — xTrades fork maintained separately.
