# How To

Practical workflows for this project. As of 2026-07-09 the solution is scaffolded and builds/tests run — but `up`/`down`/etc. still fail once they reach the real WSL container feature, since that adapter isn't written yet (see [[Progress]]). Sections marked **(planned)** genuinely don't work end to end yet.

## Prerequisites

- Windows with WSL, and the WSL container feature installed (ships `wslc.exe`; verify with `wslc image ls`). See [[References]].
- .NET SDK **10.0.301** — pinned in `global.json` at the repo root, so `dotnet --version` inside the repo should resolve to it automatically.

## Open the solution

```
WSL-Container-Compose.slnx
```

References all three projects — see [[Project Structure]].

## Build

```powershell
dotnet build
```

## Run tests

```powershell
dotnet test
```
Unit tests live in `WslContainerCompose.Core.Tests` and fake `IContainerRuntime` — they don't require a real WSL container feature to run. See [[Plan]] for why. 15 tests, all passing.

## Run the CLI locally (without installing it)

```powershell
dotnet run --project src/WslContainerCompose.Cli -- --help
dotnet run --project src/WslContainerCompose.Cli -- --file path\to\docker-compose.yml up
```
Global options (`--file`, `--project-name`) go **before** the subcommand, same convention as `docker compose -f file.yml up`. `up` today will parse the compose file correctly and then throw a clear `NotImplementedException` once it reaches the container runtime — expected until the real adapter is written.

## Use the tool (planned)

Once packaged as a .NET global tool (see [[Plan]] → Distribution):

```powershell
dotnet tool install -g WslContainerCompose.Cli
wsl-compose --file docker-compose.yml up      # start all services, in dependency order
wsl-compose ps      # list containers for this project
wsl-compose logs <service>
wsl-compose stop
wsl-compose start
wsl-compose restart
wsl-compose down    # stop and remove, reverse dependency order
```

Point it at a real `docker-compose.yml` — see [[Plan]] for which directives are supported in v1 (no `build:`, bind-mount volumes only, no custom networks).

## Working on the vault itself

This vault lives at `obsidian/wsl-container-compose` inside the repo (not the usual flat `AI Research` vault). Keep it flat, Title Case filenames, link related notes with `[[wikilinks]]`.

## Related

[[Plan]] · [[Project Structure]] · [[Progress]] · [[Index]]
