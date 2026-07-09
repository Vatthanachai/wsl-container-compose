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
| `networks:` | **Supported (provisional)** — see [[Plan#Networks (provisional)]] below. Service-name discovery via generated `/etc/hosts` entries; network membership is a logical grouping only, not enforced isolation. `ports:` (host port mapping) is still honored — it's a documented per-container setting. |
| `volumes:` | **Bind mounts only** (`./host:/container` syntax → `WslcContainerVolume`). Named volumes (top-level `volumes:` block) are rejected with "not supported yet." |
| `depends_on` | Ordering only — topological start order, reverse order on `down`/`stop`. `condition: service_healthy` is accepted but treated as plain ordering (no real health wait). |
| CLI commands | `up`, `down`, `ps`, `logs`, `stop`, `start`, `restart` only. No `exec`, `pull`, `config`, `top` in v1. |

### Failure handling

If `up` fails partway through (e.g. service 3 of 4 fails to start), the containers that **did** start are left running and the failure is reported clearly — matches docker-compose's default behavior. No automatic rollback.

### State tracking

The WSL container API doesn't document a way to list or reconnect to sessions/containers from a previous process run (no `docker ps`-equivalent across restarts). So the tool maintains its own local state file per project (e.g. `.wsl-compose/<project>.json`) recording the session and each container's identifier. Every command (`ps`, `logs`, `down`, etc.) reads this file to know what it's operating on.

## Networks (provisional)

Design reached via a grilling session on 2026-07-09, **before** confirming against a real WSL install — treat the flagged assumptions below as risks to validate once the real adapter exists, not settled facts.

**Facts pulled from the [API reference](https://wsl.dev/api-reference/) during the session:**
- `ContainerNetworkingMode` has exactly two values: `None` and `Bridged`. No per-network isolation, no multiple named bridge networks at the platform level.
- `Container` exposes no IP-address property (`Id`, `InitProcess`, `State` only). The only lead is `Inspect()`, which returns an undocumented raw string payload — **assumed** (not confirmed) to contain a parseable IP.
- No built-in DNS or service-discovery between containers is documented anywhere.

**Scope:** service-name discovery (a container can reach another by its compose service name) plus network membership as a *logical* grouping. Explicitly out of scope: `external:` networks, network `aliases`.

**Mechanism — service-name discovery:** generate `/etc/hosts` entries per container, rather than running a real DNS resolver or requiring users to work with raw IPs. Regenerated after `up`, `start`, and `restart` (container IPs can change on restart).

**Isolation is logical only, not enforced:** since the platform has no concept of isolated network groups, a service's `networks:` membership only controls which peers appear in *its* `/etc/hosts` — it does **not** stop it from reaching another container directly by IP. This is documented as a real limitation, not glossed over.

**Default network:** a service with no `networks:` key joins an implicit shared default network with every other network-less service — matches real compose semantics and is a strict superset of today's "everything reaches everything" behavior, so nothing currently working breaks.

**Validation is strict, matching the parser's existing style** (it already hard-rejects `build:` and named volumes rather than guessing):
- A service referencing a network not declared in the top-level `networks:` block → `ComposeParseException`.
- A top-level network entry specifying `driver:`/`external:` (unsupported) → `ComposeParseException`, not silently ignored.
- A service-level `networks:` map-form entry specifying `aliases:` (unsupported) → `ComposeParseException`, not silently discarded. (Contrast with `depends_on`'s `condition:`, which *is* silently discarded — the difference is that discarding a `depends_on` condition still yields correct, just weaker, behavior, whereas discarding `aliases:` would silently give the user something that doesn't do what the file says.)

**Orchestration — two-phase `up`:** `ComposeProject.UpAsync` currently creates and starts each service one at a time, strictly in dependency order (`ComposeProject.cs`). That's kept as phase 1. A new phase 2 runs once the whole batch is up: fetch every container's IP, then write each container's `/etc/hosts` peers via an exec'd process — because bind-mounting a generated hosts file at creation time can't work (two independent services may be created in either order, so an early container can't yet know a later one's IP, and `Container` has no way to add a mount after creation). This also assumes the container image has a shell available to run the write through.

**Failure handling:** if the wiring pass fails for a container (unparseable `Inspect()` output, no shell to exec into, etc.), the container itself stays running — it already started successfully — and the failure is reported in `UpResult.Failures` exactly like any other `up` failure. No new failure category, matching the existing "leave it running, tell the user" contract (see "Failure handling" above).

**`IContainerRuntime` additions:** two new single-purpose methods, matching the interface's existing granularity (`PullImageAsync`, `CreateContainerAsync`, `StartContainerAsync` are already separate steps):
- `Task<string?> GetContainerIpAddressAsync(sessionId, containerId, ct)`
- `Task WriteHostsEntriesAsync(sessionId, containerId, IReadOnlyDictionary<string,string> hostnameToIp, ct)`

**Sequencing:** built next, unit-tested against `FakeContainerRuntime` — same test-first pattern as every other feature so far — **ahead of** the real `Microsoft.WSL.Containers` adapter (supersedes the "Next up" ordering in [[Progress]] at the time of the prior entry).

**Left alone for now:** `ps` output is unchanged — no network-membership column. Easy to add later without touching this design.

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
- `external:` networks and network `aliases` — see [[Plan#Networks (provisional)]] for what *is* now planned (service discovery + logical isolation).
- Named volumes.
- `exec`, `pull`, `config`, `top` CLI commands.

## Related

[[Project Structure]] · [[Progress]] · [[How To]] · [[References]]
