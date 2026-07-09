# Project Structure

Reflects the current repo layout, scaffolded on 2026-07-09 per [[Plan]]. Update this note as the solution evolves.

## Repo root (current)

```
wsl-container-compose/
├── .editorconfig
├── .gitattributes
├── .gitignore
├── global.json                    # pins .NET SDK 10.0.301
├── README.md
├── WSL-Container-Compose.slnx     # references all three projects below
├── src/
│   ├── WslContainerCompose.Cli/
│   └── WslContainerCompose.Core/
├── tests/
│   └── WslContainerCompose.Core.Tests/
└── obsidian/
    └── wsl-container-compose/     # this vault
```

## Solution layout (scaffolded, logic still thin)

```
src/
├── WslContainerCompose.Cli/               net10.0-windows10.0.19041.0, System.CommandLine 2.0.9
│   └── Program.cs                          up/down/ps/logs/stop/start/restart wired to ComposeProject
└── WslContainerCompose.Core/               net10.0-windows10.0.19041.0
    ├── Compose/
    │   ├── ComposeFile.cs / ServiceDefinition.cs   compose model (PortMapping, BindMount records)
    │   ├── ComposeParser.cs                        YamlDotNet-based parsing of the v1-supported subset
    │   ├── EnvInterpolation.cs                     ${VAR} / ${VAR:-default}, .env + OS environment
    │   └── ComposeParseException.cs
    ├── Runtime/
    │   ├── IContainerRuntime.cs                    abstraction the orchestrator depends on
    │   ├── ContainerSpec.cs                        ContainerSpec / ContainerStatus records
    │   └── NotImplementedContainerRuntime.cs       placeholder used by Cli until the real adapter exists
    ├── State/
    │   ├── ProjectState.cs
    │   └── ProjectStateStore.cs                    JSON file per project, e.g. .wsl-compose/<project>.json
    └── Orchestration/
        ├── DependencyOrder.cs                      topological sort of depends_on (+ reverse for stop/down)
        └── ComposeProject.cs                        up/down/ps/logs/stop/start/restart against IContainerRuntime

tests/
└── WslContainerCompose.Core.Tests/         net10.0-windows10.0.19041.0, xUnit
    ├── Compose/ComposeParserTests.cs
    └── Orchestration/
        ├── DependencyOrderTests.cs
        ├── ComposeProjectTests.cs
        └── FakeContainerRuntime.cs                 in-memory IContainerRuntime test double
```

15 tests pass (`dotnet test`); the whole solution builds cleanly (`dotnet build`).

## Key architectural facts

- `WslContainerCompose.Core` references `Microsoft.WSL.Containers` directly (no separate adapter project) → both `Cli` and `Core` target `net10.0-windows10.0.19041.0` (confirmed against the package's nuspec, which declares `net8.0-windows10.0.19041`). Solution is Windows-only.
- Orchestration logic (`ComposeProject`, `DependencyOrder`) depends only on `IContainerRuntime`, not on `Session`/`Container` directly, so it's unit-tested today without any real WSL dependency.
- `Cli` stays thin: `Program.cs` only parses arguments and prints output; all logic lives in `Core`.
- `Cli`'s global options (`--file`, `--project-name`) must precede the subcommand — a System.CommandLine behavior that happens to match real `docker compose`'s own convention.
- The real `IContainerRuntime` adapter over `Session`/`Container`/`Process` does not exist yet — see [[Progress]] "Next up".

## Related

[[Plan]] · [[Index]]
