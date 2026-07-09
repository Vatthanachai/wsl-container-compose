# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```powershell
dotnet build                                                    # build the whole solution (WSL-Container-Compose.slnx)
dotnet test                                                     # run all tests
dotnet test --filter "FullyQualifiedName~ComposeParserTests"    # run one test class
dotnet test --filter "FullyQualifiedName~ComposeParserTests.MethodName"  # run one test
dotnet run --project src/WslContainerCompose.Cli -- <command> [options]  # run the CLI, e.g. `-- up`, `-- --help`
```

SDK version is pinned in `global.json` (currently `10.0.301`) — `dotnet --version` should match.

`dotnet pack` on `WslContainerCompose.Cli` **does not work** — `PackAsTool` is incompatible with the project's Windows-specific TFM (`NETSDK1146`). Don't try to fix this by changing `PackAsTool`/TFM without discussing it; it's a known constraint. Release builds are distributed as a self-contained `win-x64` publish/zip instead (see `.github/workflows/release.yml`), not a `dotnet tool`.

## Architecture

Three projects, both `net10.0-windows10.0.19041.0` (required — `Microsoft.WSL.Containers` only ships a Windows-TFM nuspec, so the whole solution is Windows-only, no cross-platform build):

- **`WslContainerCompose.Cli`** — thin `System.CommandLine` entry point (`Program.cs`). Parses global `--file`/`--project-name` options and dispatches `up`/`down`/`ps`/`logs`/`stop`/`start`/`restart` to a `ComposeProject`.
- **`WslContainerCompose.Core`** — all real logic, split into:
  - `Compose/` — `ComposeParser` (YamlDotNet) turns compose YAML into `ComposeFile`/`ServiceDefinition`. Parses a deliberately narrow subset of the Compose Specification and **fails loudly** (via `ComposeParseException`) on anything unsupported rather than guessing — `build:`, named volumes, and unsupported port/volume formats all throw with a specific message. `EnvInterpolation` handles `${VAR}`/`${VAR:-default}` from a `.env` file next to the compose file plus the OS environment.
  - `Runtime/` — `IContainerRuntime` is the abstraction boundary over `Microsoft.WSL.Containers`' `Session`/`Container`/`Process` types (which are concrete WinRT types, not mockable). The real adapter **has not been written yet** — `Cli`'s `LoadProject` currently wires up `NotImplementedContainerRuntime`, which throws `NotImplementedException` naming the unimplemented member on first call. This is expected: `up`/`down`/etc. parse and orchestrate correctly today but fail once they'd actually talk to WSL.
  - `Orchestration/` — `DependencyOrder` does a topological sort over `depends_on` for start order (reverse for stop). `ComposeProject` drives the full lifecycle against `IContainerRuntime`, persisting progress after every step via `ProjectStateStore` so a crash mid-`up` doesn't lose track of what's already running. `up`'s failure handling is deliberate: if service N of M fails, services 1..N-1 are left running and the failure is reported — no automatic rollback (matches docker-compose's default behavior).
  - `State/` — `ProjectStateStore` persists `ProjectState` (session ID + container IDs by service) as JSON in a `.wsl-compose/` directory next to the compose file. This exists because the WSL container API has no documented way to list/reconnect to sessions across process runs — every command (`ps`, `logs`, `down`, ...) reads this file to know what it's operating on, rather than asking the platform.
- **`WslContainerCompose.Core.Tests`** — xUnit tests, all against `FakeContainerRuntime` (`tests/.../Orchestration/FakeContainerRuntime.cs`), never real WSL. This is the established pattern for new features too: build and test against the fake first (see `obsidian/wsl-container-compose/Progress.md` "Next up" — the real adapter is intentionally still pending).

## Where the design record lives

Full design decisions, rationale, and current status live in the Obsidian vault at `obsidian/wsl-container-compose/` — start at `Index.md`. In particular:

- **`Plan.md`** — the authoritative scope document: what compose features v1 supports vs. explicitly defers, and why (e.g. `build:` is unsupported because the WSL container API has no build method; named volumes are rejected in favor of bind mounts only). Check it before assuming a docker-compose feature should "just work."
- **`Progress.md`** — living status log with a "Next up" section; check it before picking what to implement next.

Don't duplicate that content here — read it directly when you need the reasoning behind a scope decision.
