# Plan

Design decisions reached via a grilling session on 2026-07-09. This is the working plan for v1; revisit and update this note as decisions change.

## Runtime (fact, not a decision)

Containers are real OCI containers, run via Microsoft's native WSL container feature:
- `wslc.exe` — built-in WSL CLI for building/running/interacting with Linux containers.
- `Microsoft.WSL.Containers` NuGet package — C#/C++ API following a `WslcService` → `Session` → `Container` → `Process` hierarchy.
- Not separate WSL distros per service, not wrapping Docker/Podman engines.

See [[References]] for the source docs.

## Compose compatibility

Parse real `docker-compose.yml` files (Compose Specification), not a custom schema — maximizes compatibility with compose files people already have.

Supports `${VAR}` / `${VAR:-default}` interpolation, sourced from a `.env` file (if present) and the OS environment, same precedence as docker-compose.

## v1 scope and known gaps

The WSL container API is missing some things a full docker-compose implementation would need. v1 draws the line here:

| Compose feature | v1 behavior |
|---|---|
| `build:` | **Unsupported.** No build-from-Dockerfile method in the API (only pull/import/load/tag/push/delete). Compose files using `build:` instead of `image:` fail to parse with a clear error. |
| `networks:` | Parsed but **ignored**. All containers in a project share the session's default networking mode. `ports:` (host port mapping) is still honored — it's a documented per-container setting. |
| `volumes:` | **Bind mounts only** (`./host:/container` syntax → `WslcContainerVolume`). Named volumes (top-level `volumes:` block) are rejected with "not supported yet." |
| `depends_on` | Ordering only — topological start order, reverse order on `down`/`stop`. `condition: service_healthy` is accepted but treated as plain ordering (no real health wait). |
| CLI commands | `up`, `down`, `ps`, `logs`, `stop`, `start`, `restart` only. No `exec`, `pull`, `config`, `top` in v1. |

### Failure handling

If `up` fails partway through (e.g. service 3 of 4 fails to start), the containers that **did** start are left running and the failure is reported clearly — matches docker-compose's default behavior. No automatic rollback.

### State tracking

The WSL container API doesn't document a way to list or reconnect to sessions/containers from a previous process run (no `docker ps`-equivalent across restarts). So the tool maintains its own local state file per project (e.g. `.wsl-compose/<project>.json`) recording the session and each container's identifier. Every command (`ps`, `logs`, `down`, etc.) reads this file to know what it's operating on.

## Project layout

Three projects — see [[Project Structure]] for detail:
- `WslContainerCompose.Cli` — thin entry point, argument parsing via **System.CommandLine**.
- `WslContainerCompose.Core` — compose parsing (**YamlDotNet**) + orchestration logic, wraps `Microsoft.WSL.Containers` directly.
- `WslContainerCompose.Core.Tests` — unit tests against Core.

**Consequence:** Core references the WinRT-projected `Microsoft.WSL.Containers` package directly, so both Cli and Core target a Windows-specific TFM (`net10.0-windows`). The whole solution is Windows-only — no cross-platform build.

## Testability

`Session`/`Container` are concrete WinRT types, not naturally mockable. Core defines a small abstraction (`IContainerRuntime`, with methods like `CreateSession`, `PullImage`, `CreateContainer`, `Start`, `Stop`) that the real code implements as a thin adapter, and tests fake. Keeps orchestration logic (dependency ordering, state-file updates, error handling) unit-testable without touching real WSL.

## Distribution

Packaged as a **.NET global tool** (`dotnet tool install -g`), giving a `wsl-compose` command on PATH. Requires the .NET 10 SDK/runtime on the machine.

## Decisions explicitly deferred (not v1)

- Building images from a Dockerfile (`build:`) — revisit once it's clear whether/how the WSL container API will grow a build method, or whether to shell out to an external builder.
- Custom networks / service-discovery-by-name — revisit once a spike confirms what inter-container reachability the WSL container API's "networking mode" actually provides.
- Named volumes.
- `exec`, `pull`, `config`, `top` CLI commands.

## Related

[[Project Structure]] · [[Progress]] · [[How To]] · [[References]]
