[← Back to index](README.md)

# Development

For anyone who wants to work on the code. xTrades is C# on .NET 10, with Avalonia 11.3 for the UI and SkiaSharp 2.88
for drawing charts.

## Solution structure

`TradingHub.slnx` has ten projects:

| Project | What it does |
|---|---|
| `src/TradingHub.Core` | Market and data: instruments, trades, candles, tick files, CSV import, history sources (combined Rithmic + Databento), connections (Binance, Bybit, Rithmic, Tradovate, OANDA, Alpaca, cTrader, Databento, Interactive Brokers test, simulator, and replay), and the trading layer (accounts, simulated account, stops/targets, history). No external packages. |
| `src/TradingHub.Sdk` | Indicator API, matching the ATAS shape: `Indicator`, series, drawing, attributes, and strings. |
| `src/TradingHub.Charting` | Chart engine with Skia: model, candles, footprint, DOM, tape, drawings, and themes. |
| `src/TradingHub.Scripting` | Compiles C# in memory with Roslyn (indicators and strategies) and adapts ATAS namespaces. |
| `src/TradingHub.Strategies` | Strategy API, backtest engine, optimization, smart walk-forward, robustness validation, portfolios, and live execution with accounting. Depends only on Core. |
| `src/TradingHub.Indicators.Native` | Built-in indicators. |
| `src/TradingHub.Indicators.Technical` | Technical indicator library port; the public version only ships the editor types they use. |
| `src/TradingHub.App` | The app (executable `TradingHub`): `Views` (sections), `Windows`, `Controls`, `Services` (connections and secret store, Claude Code, live strategies, risk agent, market conditions, menu bar, sleep guard…), `Lab` (MCP server and reports), and `Styles`. Run with `--mcp` to start as an MCP lab server with no windows. |
| `tests/TradingHub.Tests` | Tests using xUnit. |
| `tools/TradingHub.UiSnapshot` | Headless app with scenes that verify and capture the UI. |

Avalonia and SkiaSharp versions, target framework, and product version are in `Directory.Build.props`.
The visible app name comes from `TradingHub.Common.Product.Name`; data paths from `Product` and `AppPaths`.

## Build and run

**macOS:**
```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build TradingHub.slnx
dotnet test tests/TradingHub.Tests
./build-app.sh                   # builds dist/Trading Hub.app (ad hoc signed)
./build-app.sh --install         # and copies it to /Applications
```

**Windows (port in progress — see `WINDOWS_PORT.md`):**
```powershell
dotnet build TradingHub.slnx
dotnet test tests/TradingHub.Tests
.\build-windows.ps1              # once the port is complete
```

To run the app from source without touching your real data or credentials:

```bash
# macOS
TRADINGHUB_DATA_DIR=/tmp/th-dev TRADINGHUB_KEYCHAIN_FILE=/tmp/th-dev/keychain.json dotnet run --project src/TradingHub.App

# Windows
$env:TRADINGHUB_DATA_DIR="C:\Temp\th-dev"; $env:TRADINGHUB_KEYCHAIN_FILE="C:\Temp\th-dev\keychain.json"; dotnet run --project src/TradingHub.App
```

With `TRADINGHUB_DATA_DIR` set, strategies and indicators also go to subfolders of that directory; the menu bar icon is hidden and the sleep guard does not touch system energy settings. Never run a second copy against the same real data folder — two processes writing the same files will corrupt them.

## Tests

`dotnet test tests/TradingHub.Tests` runs all tests. Coverage includes: backtest engine and fills (`StrategyTests`), stops/targets/OCO (`BracketTests`), market context and filter (`ContextTests`), robustness and validation (`RobustnessTests`), smart walk-forward (`SmartWalkForwardTests`), live execution (`LiveRunnerTests`, `LivePortfolioTests`), portfolios and reports (`PortfolioTests`), data (`CoreTests`, `DataIntegrityTests`, `HistorySourcesTests`, `LongHistoryTests`, `CsvImportTests`), accounts (`AccountTests`), drawings and legend (`DrawingAndLegendTests`), and connections (`RithmicTests`, `BrokerConnectionTests`, `DataSourceTests`, `BinanceSignatureTests`).

Tests use temporary folders and fake servers built from real service message payloads (`FakeRithmicServer`, `FakeHttpServer` with `FakeBrokers` for OANDA, Alpaca, Databento, cTrader, and Tradovate). They never touch a real account. To run a subset: `dotnet test tests/TradingHub.Tests --filter FullyQualifiedName~SmartWalkForward`.

## Headless UI scenes

```bash
dotnet run --project tools/TradingHub.UiSnapshot -- <output-folder> 4 <scene> [<scene>…] [--open connection:SYMBOL] [--size 1600x950]
```

`<output-folder>` is where PNG captures are saved. `4` is the seconds to wait for the app to load before the scenes run (with no scenes, captures Trading). Scenes with verifications print `OK` or `FAIL` per check; the process exits with the failure count as the exit code.

| Scene | What it does |
|---|---|
| `trading`, `accounts`, `strategies`, `live`, `data`, `connections`, `settings` | Opens that section and captures it |
| `conexiones` | Tradovate, cTrader, OANDA, Alpaca, and Databento against fake servers: add, test, Databento cost, Tradovate accounts, OANDA live prices |
| `panel` | Right panel in Trading (Chart Trader, DOM, tape), DOM buttons, and floating windows |
| `footprint` | Footprint ladders with custom colors and the footprint editor |
| `mercado` | Backtest by market and filtered run by condition |
| `theme` | Dark and light mode with chart configuration |
| `choice`, `picker`, `esquinas` | History picker, instrument selector, and rounded-corner diagnostics |
| `data+rithmic`, `data+import` | Data with Rithmic (fake server) and CSV test import |
| `accounts+demo` | Accounts section with orders in the simulated account and fake Rithmic |
| `connections+rithmic`, `connections+amp` | Rithmic or AMP profile with server list |
| `trading+…` | `layout2`, `layout2r`, `layout3`, `layout4`, `layout6`, `history<hours>[+period]`, `historyall`, `jitter`, `float`, `brackets`, `drawings`, `drawsettings`, `links`, `toolbar`, `trader`, `accounts`, `crossdemo`, `strategy`, `domfloat`, `domsettings` |
| `strategies+…` | `backtest`, `optimize`, `wfo` (run and capture), `code`, `history`, `reports`, `portfolio`, `claudefloat`, `claude`, `claudelab`; append `+FileName` to pick a specific strategy |

Data isolation in scenes:
- Sets `TRADINGHUB_SLEEP_GUARD=off` and a temporary `TRADINGHUB_KEYCHAIN_FILE` with fake secrets — never reads the real credential store.
- Copies your strategies and indicators to temporary folders (`TRADINGHUB_STRATEGIES_DIR`, `TRADINGHUB_INDICATORS_DIR`).
- Uses a temporary data folder (`TRADINGHUB_DATA_DIR`) with copies of `settings.json`, `workspace.json`, `connections.json`, and downloaded data (`Ticks`, `Klines`, `Drawings`, except Rithmic) — never the originals. Connections have API keys stripped, real orders disabled, and Rithmic pointed at a fake server with `demo` user.
- Only one scene run at a time: a lock in the temp folder makes the next one wait (up to 20 minutes).
- If any of those env vars are already set, your values are respected.

Note: `strategies+claude` and `strategies+claudelab` send a real request to your Claude Code session (consumes your quota), unless `TRADINGHUB_CLAUDE_CLI` points to another executable.

## Environment variables

| Variable | Effect |
|---|---|
| `TRADINGHUB_DATA_DIR` | Data folder (instead of the default per-platform path) |
| `TRADINGHUB_STRATEGIES_DIR`, `TRADINGHUB_INDICATORS_DIR` | Strategies and indicators folders |
| `TRADINGHUB_KEYCHAIN_FILE` | JSON file with secrets instead of the platform credential store |
| `TRADINGHUB_BACKTESTS_DIR`, `TRADINGHUB_LIVE_DIR` | Backtest history and live portfolio folders |
| `TRADINGHUB_SLEEP_GUARD=off` | Disables the sleep guard |
| `TRADINGHUB_CLAUDE_CLI` | Path to the Claude Code CLI |
| `TRADINGHUB_MCP_COMMAND` | Executable that starts the lab server (default: the app itself) |
| `TRADINGHUB_OANDA_REST`, `TRADINGHUB_OANDA_STREAM`, `TRADINGHUB_ALPACA_DATA`, `TRADINGHUB_ALPACA_TRADING`, `TRADINGHUB_ALPACA_STREAM`, `TRADINGHUB_TRADOVATE_REST`, `TRADINGHUB_TRADOVATE_WS`, `TRADINGHUB_TRADOVATE_MD`, `TRADINGHUB_CTRADER_URL`, `TRADINGHUB_DATABENTO_URL` | Point each connection at an alternative server (used by fake-server scenes) |

## Adding a connection

1. In `src/TradingHub.Core/Feeds/<Name>/`, a class implementing `IMarketDataFeed`: `GetInstrumentsAsync`, `LoadTradesAsync` (history in batches), and `Subscribe`, delivering trades, depth, and state to an `IMarketDataSink`. Depending on what the service offers, also implement `IKlineHistory` (candles for long history), `IDayDownloader` (full-day downloads in Data), `ISessionAwareFeed` (markets that close), or `ITickerStatsProvider` (24h price and change in the instrument selector).
2. If tradable, an account implementing `ITradingAccount` (market, limit, stop, cancel, modify, close, reverse, position, and `IsReal`).
3. In `src/TradingHub.App/Services/Connections.cs`: a new `ConnectionKind` value, its `MarketDataId`, non-secret fields in `ConnectionProfile`, the feed in `ConnectionManager.GetFeed`, and the test in `TestAsync`. Secrets go through `SetSecret` / `GetSecret` (stored in the platform credential store). Add a `TRADINGHUB_…` env var to redirect the server in tests.
4. The form in `src/TradingHub.App/Views/ConnectionsView.cs` and, if tradable, its accounts in `AppServices.AccountsFor` and `AccountFor`: real accounts only with "Allow real orders" enabled.
5. A fake server in `tests/TradingHub.Tests` (start from `FakeHttpServer`) with its tests.

## Adding an indicator or lab tool

- **Built-in indicator**: a class in `src/TradingHub.Indicators.Native` inheriting from `Indicator`, with a no-arg constructor, `[DisplayName]`, and `[Category(IndicatorCategories.…)]`. The app scans that assembly at startup. For a personal indicator, no code changes needed — just drop it in the indicators folder (see [Indicators](indicators.md)).
- **Lab tool**: add its schema in `LabServer.Tools()`, its case in `LabServer.CallAsync` (`src/TradingHub.App/Lab/LabServer.cs`), and its name in `ClaudeCodeBridge.LabTools` (`src/TradingHub.App/Services/ClaudeCode.cs`) — the allowed tool list.

## Validation from code

`ValidationGate.Evaluate` (in `TradingHub.Strategies`) applies the backtesting rules to a walk-forward result. See `tests/TradingHub.Tests/RobustnessTests.cs` for examples.

```csharp
var gate = ValidationGate.Evaluate(new GateInput
{
    WalkForward = walkForward,   // result of Optimizer.WalkForward
    Optimization = optimization, // optional: for plateau and robustness checks
    Trials = 12,                 // configurations tested, for deflated Sharpe
    TrialVariance = 0.01,        // variance of their per-trade Sharpe
    MinTrades = 30,
});
Console.WriteLine($"{gate.StatusText} · Deflated Sharpe {gate.Deflated:P0}");
foreach (var c in gate.Checks) Console.WriteLine($"{c.Status} · {c.Name}: {c.Text}");
```

`LiveStrategyRunner.Guard` also accepts an `ILiveOrderGuard` — a limiter that checks each order that opens or increases a position before it is sent (orders that reduce a position always pass). The app doesn't ship one, but the tests show how it works.

## Rules

- **Never write passwords, tokens, or API keys to repo files.** The app stores them in the platform credential store (`Keychain` / `CredWrite` in `Connections.cs`); tests use `TRADINGHUB_KEYCHAIN_FILE` with fake data.
- **Never run tools against real data** (the data folder and `Documents\Trading Hub`): use the env vars above. Two processes writing the same files will corrupt them.
- **No confirmation dialogs blocking order buttons.** Safety is in risk limits (size, daily loss, drawdown, Stop All) and requiring real orders to be enabled per connection — not in adding friction between a click and an order. Order buttons must not move: the `trading+jitter` scene measures whether the bar changes height or the buy button shifts when the day's P&L changes.
- Always test with the simulated account, and with fake servers when touching a connection.
