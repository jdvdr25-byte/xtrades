<div align="center">

# xTrades

**Free, open-source order flow trading platform for Windows and macOS.**
Footprint charts, DOM trading, professional anti-overfit backtesting, and AI-assisted strategies —
with your data and credentials always on your machine.

Based on [Trading Hub](https://github.com/ralphtradesz/trading-hub) by [@ralphtradesz](https://x.com/ralphtradesz)

[![License: MIT + Commons Clause](https://img.shields.io/badge/license-MIT%20%2B%20Commons%20Clause-amber.svg)](LICENSE)
![Windows + macOS](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-blue.svg)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)
![Avalonia](https://img.shields.io/badge/UI-Avalonia-8B44AC.svg)

</div>

---

## What it is

xTrades is a desktop trading platform that runs entirely locally: the app, market data, strategies, backtests, and
broker credentials (stored in the platform secret store, never in files). No servers, no accounts to create, no
telemetry.

Supports **CME futures** (Rithmic, Tradovate, Databento history), **crypto** (Binance and Bybit, free data without
keys), **forex/CFDs** (OANDA, cTrader, CSV import), and **US equities** (Alpaca).

AI assistance comes from **Claude Code with your own subscription** (no API keys): an assistant that writes
strategies in C#, compiles them, runs backtests, and builds reports.

## Features

### Charts
- Time, tick, volume, and range candles; candles, bars, line, or Heikin Ashi.
- **Footprint** configurable column-by-column (bid/ask, delta, volume, POC, per-candle profile) with its own editor.
- DOM, tape, drawings, and templates.
- Built-in indicators: moving averages, VWAP with deviations, Bollinger, RSI, MACD, volume, delta, CVD,
  session volume profile, and big trades. Add your own in C# (auto-recompiled on save), ATAS-API-compatible.
- **Replay** any saved day at any speed.

### Trading
- **Chart Trader** and DOM with market, limit, and stop orders, brackets (stop + targets), and OCO.
- Simulated and live accounts; account history and statistics.
- No confirmation dialogs blocking order clicks — safety is in risk limits.

### Data
- Everything you view is recorded tick by tick with aggressor side.
- Downloads: Binance/Bybit public files, Rithmic history, Databento tick-by-tick CME futures (pay-per-use, shows cost before download). Databento data is kept separate; backtests can use Rithmic only, Databento only, or both combined.
- CSV import: Dukascopy, HistData, MetaTrader, TradingView.

### Backtesting
- C# strategy editor with Roslyn in-app compilation.
- Anti-overfit backtesting, walk-forward optimization, and a Monte Carlo simulator.
- Claude Code assistant that writes, tests, and iterates strategies in the backtest chat.

### Indicators (v1 roadmap)
- RTH/ETH session split for volume profiles (user-configurable times per instrument)
- Monthly volume profile period
- Developing POC (real-time POC line on chart)
- Naked POC tracking (20 sessions back)
- Split bid/ask delta bars (stacked green/red per price level)

## Getting started

See [`docs/getting-started.md`](docs/getting-started.md).

**Windows:** The Windows port is actively in progress — see [`WINDOWS_PORT.md`](WINDOWS_PORT.md).

**macOS (Apple Silicon):**
```bash
git clone https://github.com/your-username/xtrades
cd xtrades
./build-app.sh --install
open "/Applications/Trading Hub.app"
```

Requires macOS 12+, .NET 10 SDK. No admin password needed.

## More indicators

Clone the [ATAS public repo](https://github.com/AtasPlatform/Indicators) into `third_party/ATAS` to add ~250
ATAS-compatible indicators.

## Building from source

```bash
dotnet build TradingHub.slnx
dotnet test tests/TradingHub.Tests
```

See [`docs/development.md`](docs/development.md) for the full developer guide.

## License

MIT + Commons Clause — free to use, modify, and share; cannot be sold or used as the basis of a paid service.
See [`LICENSE`](LICENSE).

Original Trading Hub created by [@ralphtradesz](https://x.com/ralphtradesz).
