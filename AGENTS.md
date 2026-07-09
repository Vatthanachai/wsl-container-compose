# AGENTS.md

Guidance for AI coding agents working in this repository. This mirrors `CLAUDE.md` — if you're Claude Code, read that instead; this file exists for agents that only look for `AGENTS.md`.

## Quick facts

- Windows-only .NET 10 solution (`net10.0-windows10.0.19041.0` everywhere — required by `Microsoft.WSL.Containers`, not optional). No cross-platform build.
- Build/test: `dotnet build`, `dotnet test`. Run the CLI: `dotnet run --project src/WslContainerCompose.Cli -- <command>`. Full command list in `COMMANDS.md`.
- `dotnet pack` on the CLI project is broken (`NETSDK1146` — `PackAsTool` + Windows TFM conflict). Don't attempt to "fix" this without discussing it; releases ship as a self-contained `win-x64` zip instead, not a `dotnet tool`.
- The real `IContainerRuntime` adapter over `Microsoft.WSL.Containers` **has not been written yet**. `up`/`down`/etc. parse and orchestrate correctly but throw `NotImplementedException` once they'd talk to real WSL. All tests run against `FakeContainerRuntime` — that's the established pattern for new features too, not a shortcut.
- The compose parser fails loudly on anything outside v1's supported subset (`build:`, named volumes, unsupported port/volume syntax) rather than guessing. Follow that precedent for new parsing code.

## Where decisions live

- `CLAUDE.md` — architecture overview and command reference.
- `obsidian/wsl-container-compose/Plan.md` — authoritative scope: what's supported, what's deferred, and why.
- `obsidian/wsl-container-compose/Progress.md` — living status log and "Next up" list.
- `MEMORY.md` — load-bearing gotchas worth knowing before you act.

Don't duplicate that content into new files — read it directly.
