# WSL Container Compose

A docker-compose / podman-compose equivalent for Windows, built in C# .NET 10. It orchestrates real containers via Microsoft's native WSL container feature (`Microsoft.WSL.Containers`), rather than wrapping Docker or Podman.

> **Status:** solution scaffolded, builds and tests pass. The real adapter to the WSL container API isn't written yet, so `up`/`down`/etc. parse and orchestrate correctly but fail once they'd actually talk to WSL. See `obsidian/wsl-container-compose/Progress.md` for the current state.

## What it does

Point it at a `docker-compose.yml` and it drives the [WSL container API](https://learn.microsoft.com/en-us/windows/wsl/wsl-container?tabs=csharp) to create a `Session` and start/stop the described services as real containers, respecting `depends_on` ordering, `ports`, environment variables, bind-mount `volumes`, and `networks:` for service-name discovery.

v1 is intentionally scoped below full docker-compose parity — see `obsidian/wsl-container-compose/Plan.md` for the full list of what's supported and what's explicitly deferred (`build:`, named volumes, `exec`/`pull`/`config`/`top`).

## Prerequisites

- Windows with the WSL container feature installed (ships `wslc.exe`).
- .NET SDK 10.0.301 (pinned in `global.json`).

## Installing a release build

Tagged releases (`v*.*.*`) publish a self-contained, single-file `win-x64` build — no separate .NET runtime install required, just the WSL container feature.

1. Download `wsl-compose-<version>-win-x64.zip` from the [Releases page](https://github.com/Vatthanachai/wsl-container-compose/releases).
2. Extract it somewhere permanent and add that folder to your `PATH` (once).
3. Run `wsl-compose` from anywhere:

```powershell
wsl-compose up
wsl-compose ps
wsl-compose down
```

All commands and options below work identically — just replace `dotnet run --project src/WslContainerCompose.Cli --` with `wsl-compose`.

> Not currently distributed as a `dotnet tool` (`dotnet tool install -g`) — `PackAsTool` doesn't support the Windows-specific target framework this project requires, so the zip above is the only packaged distribution for now.

## Usage

```powershell
dotnet run --project src/WslContainerCompose.Cli -- <command> [options]
```

### Global options

| Option | Default | Description |
|---|---|---|
| `--file`, `-f` | `docker-compose.yml` | Path to the compose file. |
| `--project-name`, `-p` | compose file's directory name | Project name, used to namespace containers and state. |

### Commands

| Command | Description |
|---|---|
| `up` | Create and start all services. |
| `down` | Stop and remove all services. |
| `ps` | List this project's containers. |
| `logs <service>` | Show logs for a service. |
| `stop` | Stop all services without removing them. |
| `start` | Start previously-stopped services. |
| `restart` | Stop then start all services. |

```powershell
dotnet run --project src/WslContainerCompose.Cli -- up
dotnet run --project src/WslContainerCompose.Cli -- ps
dotnet run --project src/WslContainerCompose.Cli -- logs web
dotnet run --project src/WslContainerCompose.Cli -- down

# Custom file/project name
dotnet run --project src/WslContainerCompose.Cli -- -f deploy/docker-compose.yml -p myapp up
```

### Supported compose file syntax

v1 parses a subset of the Compose Specification. Per service:

- `image:` (required — `build:` is not supported yet)
- `command:` — string or list
- `ports:` — `"host:container"` (optional `/tcp` or `/udp` suffix, ignored)
- `volumes:` — bind mounts only (`./host/path:/container/path`); named volumes are rejected
- `environment:` — list (`KEY=value`) or map form
- `depends_on:` — list or map form; only start ordering is honored, not health conditions
- `networks:` — list or bare map form; see [Networks](#networks) below

Top-level named `volumes:` are not supported. A `.env` file next to the compose file is loaded automatically for variable interpolation (`${VAR}`).

```yaml
services:
  web:
    image: nginx:alpine
    ports:
      - "8080:80"
    environment:
      - MESSAGE=${MESSAGE}
    volumes:
      - ./html:/usr/share/nginx/html
    depends_on:
      - api

  api:
    image: myapi:latest
    environment:
      PORT: "3000"
```

State (container IDs, service status) is tracked per-project in a `.wsl-compose/` directory next to the compose file.

### Networks

> **Provisional:** this hasn't been validated against a real WSL install yet (the real container adapter isn't written — see the status note at the top of this file). See `obsidian/wsl-container-compose/Plan.md` ("Networks (provisional)") for the full design and its unconfirmed assumptions.

Services can reach each other by compose service name. A service with no `networks:` key joins an implicit shared `default` network with every other network-less service — this matches real docker-compose and is a superset of always-worked behavior, so a plain compose file with no `networks:` section at all needs no changes.

To scope which services can resolve which others, declare networks explicitly:

```yaml
networks:
  frontend: {}
  backend: {}

services:
  web:
    image: nginx:alpine
    networks:
      - frontend

  api:
    image: myapi:latest
    networks:
      - frontend
      - backend

  db:
    image: postgres:16
    networks:
      - backend
```

Here `web` can resolve `api` but not `db`; `api` can resolve both; a service given explicit `networks:` no longer gets the implicit `default` network.

**Important limitation:** network membership only controls which peers appear in a container's resolvable hostnames (via generated `/etc/hosts` entries) — it is **not** enforced network isolation. Any container can still reach any other directly by IP, regardless of `networks:` membership, because the underlying WSL container API has no concept of isolated network groups.

**Not supported** (fails to parse with a clear error rather than being silently ignored):
- Referencing a network in a service that isn't declared in the top-level `networks:` block.
- `driver:`/`external:` options on a top-level network entry.
- `aliases:` on a service's network entry.

## Documentation

Full design decisions, project structure, progress log, and how-to guides live in the project's Obsidian vault at `obsidian/wsl-container-compose/`. Start at `Index.md`.

## Solution layout

```
src/
├── WslContainerCompose.Cli/    # CLI entry point (System.CommandLine)
└── WslContainerCompose.Core/   # compose parsing + orchestration (net10.0-windows10.0.19041.0)

tests/
└── WslContainerCompose.Core.Tests/   # 30 unit tests, all against a fake IContainerRuntime
```

## Building and testing

```powershell
dotnet build
dotnet test
dotnet run --project src/WslContainerCompose.Cli -- --help
```
