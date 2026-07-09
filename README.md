# WSL Container Compose

A docker-compose / podman-compose equivalent for Windows, built in C# .NET 10. It orchestrates real containers via Microsoft's native WSL container feature (`Microsoft.WSL.Containers`), rather than wrapping Docker or Podman.

> **Status:** solution scaffolded, builds and tests pass. The real adapter to the WSL container API isn't written yet, so `up`/`down`/etc. parse and orchestrate correctly but fail once they'd actually talk to WSL. See `obsidian/wsl-container-compose/Progress.md` for the current state.

## What it does

Point it at a `docker-compose.yml` and it drives the [WSL container API](https://learn.microsoft.com/en-us/windows/wsl/wsl-container?tabs=csharp) to create a `Session` and start/stop the described services as real containers, respecting `depends_on` ordering, `ports`, environment variables, and bind-mount `volumes`.

v1 is intentionally scoped below full docker-compose parity — see `obsidian/wsl-container-compose/Plan.md` for the full list of what's supported and what's explicitly deferred (`build:`, custom networks, named volumes, `exec`/`pull`/`config`/`top`).

## Prerequisites

- Windows with the WSL container feature installed (ships `wslc.exe`).
- .NET SDK 10.0.301 (pinned in `global.json`).

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

Top-level named `volumes:` and `networks:` are not supported. A `.env` file next to the compose file is loaded automatically for variable interpolation (`${VAR}`).

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
