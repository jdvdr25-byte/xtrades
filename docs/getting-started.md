[← Back to index](README.md)

# Getting started

A walkthrough of the main window so you know where everything is. If you haven't installed yet, start at
[Installation](installation.md).

## First launch

When xTrades opens for the first time it shows the **Trading** section with a `SIM-BTC` chart in 5-minute footprint mode,
with the Big Trades and Delta indicators. That instrument comes from the **Simulator** — a simulated market that works
without internet, useful for practice.

All trades go to the **simulated account**. Nothing touches real money until you enable it manually, connection by
connection, in [Connections](connections.md).

These connections are pre-created:

| Connection | On startup | What it provides |
|---|---|---|
| Simulator | enabled | SIM-BTC, SIM-ETH, SIM-ES, SIM-NQ simulated instruments |
| Binance Futures | enabled | Public crypto data, no account needed |
| Binance Spot | enabled | Public crypto data, no account needed |
| Bybit | enabled | Public USDT perpetuals data |
| Rithmic | disabled | CME futures (requires your credentials) |

## The main window

### Sections — in the title bar

Navigation is integrated into the title bar, next to the window buttons:

| Section | Shortcut | Purpose |
|---|---|---|
| Trading | Cmd/Ctrl+1 | Charts, DOM, tape, and Chart Trader. |
| Account | Cmd/Ctrl+5 | Balance, positions, orders, executions, trades, and statistics. |
| Backtesting | Cmd/Ctrl+2 | C# strategies, backtests, optimization, and the Claude chat. |
| Live | Cmd/Ctrl+7 | Running automated strategies, portfolios, and the Stop All button. |
| Data | Cmd/Ctrl+3 | Saved history, downloads, file import, Databento, and replay. |
| Connections | Cmd/Ctrl+4 | Markets, brokers, and accounts. |
| Settings | Cmd/Ctrl+, | Appearance, timezone, shortcuts, sleep guard, and folders. |

In a narrow window (under 1360 px wide) the bar shows icons only; names appear on hover. Drag the bar to move the
window; double-click to maximize.

### Right side of the title bar

- **Simulated 0.00 · 10,000.00**: today's P&L and the simulated account balance. Click to go to Account.
- **Sun or moon**: toggle light/dark mode.
- **Connection pill**: the active chart's connection and its status. Click for "Reconnect now" and "Connections…".

| Status | Meaning |
|---|---|
| connected | Data is arriving. |
| connecting… / reconnecting… (attempt N) | Connecting; after a gap, missing data backfills automatically. |
| no data for N min | Connection is up, but nothing has arrived for a while. |
| market closed · opens (day and time) | Futures outside trading hours: not a disconnect, data resumes automatically. |
| error / disconnected | Check the connection in Connections. |

After a sleep/wake cycle, xTrades reconnects automatically and backfills the gap.

### Status bar

At the bottom. The left side shows the latest notice: orders sent, history loads, price alerts, and errors (hover to
see the full message). The right side shows a summary of the active chart: "N trades · N bars · since dd/MM HH:mm",
or "Loading history N%" while loading.

## The Trading section

1. **Chart bar** (top): instrument, history, periods, chart type, indicators, footprint, colors, strategies, templates,
   layout, panels, float window, and new tab. Details in [Charts](charts.md).
2. **Tabs**: the tab strip appears when there is more than one. Each tab can show 1–6 charts.
3. **The chart**, with its drawing toolbar on the left and its Chart Trader on the right (collapse with the arrow).
4. **Right panel**: Chart Trader, DOM, and tape, stacked. Drag the panel border to resize; drag dividers to resize
   each section. Hide any section with its × or pop it out to a floating window.
5. **Replay bar** (bottom): appears when the active chart is in replay mode (see [Data](data.md#replay)).

To trade, see [Trading](trading.md).

## The simulated account

- Starts with a 10,000 balance. Change it in **Account → Starting balance**.
- Works with any market: the Simulator or real data from Binance, Bybit, Rithmic, and other connections.
- Fills orders against the real market: market orders fill at the best bid or ask; limit orders fill when the price
  trades through your price or when more volume than your queue position trades at your price; stop orders fill when
  the trigger price trades.
- Balance and history are saved — they don't reset when you close the app. To start fresh, use "Reset simulated
  account" in Account.

## Opening a chart

1. In Trading, click the instrument selector (first button in the chart bar) or press Cmd/Ctrl+K.
2. At the top, pick a connection or **Favorites** to see only starred instruments.
3. Type to search (BTC, ETH, NQ…). Use arrow keys to navigate the list.
4. Enter or double-click opens it in the active chart. Cmd/Ctrl+Enter opens it in a new tab.
5. Click the star on any row to mark it as a favorite.

When a chart opens, recent history loads automatically (how much depends on each connection). To load further back, use
the history button in the chart bar. Open a new tab with Cmd/Ctrl+T or the **+** button — it copies the active chart,
unless you've set a template for new tabs. Cmd/Ctrl+W closes the active chart; at least one tab always stays open.

To see real markets with your accounts, add them in [Connections](connections.md).

## Keyboard shortcuts

In addition to menu shortcuts, **Settings → Keyboard shortcuts** has configurable shortcuts. They work in Trading and
in floating chart windows, over the active chart. To change one, click its button and press the combination; Esc
cancels and Backspace clears the assignment. "Restore defaults" resets to the originals.

| Default shortcut | Action |
|---|---|
| Alt+1 to Alt+6 | 1-minute, 5-minute, 15-minute, 1-hour, 4-hour, and daily period |
| Alt+H | Draw a horizontal line |
| Alt+T | Show or hide the Chart Trader |
| Ctrl+Tab / Ctrl+Shift+Tab | Next / previous tab |

Trading shortcuts (buy, sell, close, reverse, cancel, partials, breakeven, and others) are unassigned by default —
choose your own. See [Trading](trading.md#trading-shortcuts).

On the chart, these also work:

| Key | Action |
|---|---|
| Esc | Return to cursor, cancel an order drag, or clear a trade highlight |
| Backspace or Delete | Delete selected drawings |
| Home or End | Jump to latest with auto-scale |
| ← / → | Scroll the chart |
| + / − | Zoom in or out |

## Settings worth checking

- **General**: appearance (dark or light), chart timezone, and alert sounds.
- **Trading**: "Confirm real orders" (see [Trading](trading.md#safety)).
- **Live strategies**: keep running after window close, resume strategies on open, auto-select contract size (minis or micros).
- **Sleep guard**: prevent the machine from sleeping while live strategies are running.
- **Data**: "Record live data as it arrives" and folder shortcuts.

## What's next

- [Connections](connections.md): add markets and brokers.
- [Charts](charts.md): periods, footprint, drawings, indicators, and templates.
- [Trading](trading.md): Chart Trader, DOM, orders, stops and targets, and the Account section.
- [Data](data.md): saved history, downloads, import, Databento, and replay.
