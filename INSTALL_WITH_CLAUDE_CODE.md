# Install xTrades with Claude Code

The easiest way to install xTrades is to ask your own Claude Code: it compiles the app, installs it, and connects it
to your Claude subscription. No programming knowledge needed.

## Before you start

- **macOS**: Apple Silicon (M1 or newer), macOS 12 or newer.
- **Windows**: Windows 10/11 64-bit, [Git](https://git-scm.com), [.NET 10 SDK](https://dotnet.microsoft.com/download).
- [Claude Code](https://claude.com/claude-code) installed and signed in (Pro or Max plan).

> **Note:** The Windows version is currently being ported. See `WINDOWS_PORT.md` for progress.

## Steps

1. Clone the project:
   ```bash
   git clone https://github.com/your-username/xtrades.git
   ```
   or from GitHub: **Code → Download ZIP** and extract it.

2. Open a terminal in that folder and start Claude Code:
   ```bash
   cd xtrades
   claude
   ```

3. Paste this message:

---

```
Install xTrades by following the instructions in CLAUDE.md in this folder:
check that my system is compatible, install the .NET 10 SDK if missing,
build and install the app, verify that the app can find my Claude Code,
open it, and give me a brief overview of how to get started.
```

---

## What happens

- Claude Code checks your system (Apple Silicon + macOS 12+, or Windows 10/11 64-bit).
- If the .NET 10 SDK is missing, it downloads it from the official Microsoft site to `~/.dotnet` (macOS) or without
  admin rights (Windows) — nothing else on your system is touched.
- Builds xTrades (a few minutes the first time) and installs it.
- Verifies the app can find your Claude Code: since it uses your existing installation and session, the AI features
  (Backtesting chat, lab, and live risk agent) connect to **your** subscription with no API keys.
- Opens the app. It starts with a simulated account — nothing trades real money until you enable it in Connections.

## Update

Pull the new version (`git pull` or download the new ZIP) and ask Claude Code: "update xTrades". Your data, connections,
and strategies are preserved (they live outside this folder).

## Troubleshooting

Ask Claude Code to show you the error and fix it. Common issues:

- **App can't find Claude Code**: ask Claude Code to create the link to the Claude CLI.
- **Build fails**: confirm that `dotnet --list-sdks` shows a version starting with `10.`.
- **macOS says it can't verify the app**: the app is signed ad hoc during compilation; if you copied it from another
  machine, build it again on yours.

More details in [docs/installation.md](docs/installation.md).
