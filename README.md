# WSL Container Compose

A docker-compose / podman-compose equivalent for Windows, built in C# .NET 10. It orchestrates real containers via Microsoft's native WSL container feature (`Microsoft.WSL.Containers`), rather than wrapping Docker or Podman.

> **Status:** solution scaffolded, builds and tests pass. The real adapter to the WSL container API isn't written yet, so `up`/`down`/etc. parse and orchestrate correctly but fail once they'd actually talk to WSL. See `obsidian/wsl-container-compose/Progress.md` for the current state.

## What it does

Point it at a `docker-compose.yml` and it drives the [WSL container API](https://learn.microsoft.com/en-us/windows/wsl/wsl-container?tabs=csharp) to create a `Session` and start/stop the described services as real containers, respecting `depends_on` ordering, `ports`, environment variables, and bind-mount `volumes`.

v1 is intentionally scoped below full docker-compose parity — see `obsidian/wsl-container-compose/Plan.md` for the full list of what's supported and what's explicitly deferred (`build:`, custom networks, named volumes, `exec`/`pull`/`config`/`top`).

## Prerequisites

- Windows with the WSL container feature installed (ships `wslc.exe`).
- .NET SDK 10.0.301 (pinned in `global.json`).

## Documentation

Full design decisions, project structure, progress log, and how-to guides live in the project's Obsidian vault at `obsidian/wsl-container-compose/`. Start at `Index.md`.

## Solution layout

```
src/
├── WslContainerCompose.Cli/    # CLI entry point (System.CommandLine)
└── WslContainerCompose.Core/   # compose parsing + orchestration (net10.0-windows10.0.19041.0)

tests/
└── WslContainerCompose.Core.Tests/   # 15 unit tests, all against a fake IContainerRuntime
```

## Building and testing

```powershell
dotnet build
dotnet test
dotnet run --project src/WslContainerCompose.Cli -- --help
```
