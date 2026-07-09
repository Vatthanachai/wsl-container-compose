# TOOLS.md

Key libraries and platform dependencies, and why they're here. For the fuller rationale behind these choices, see `obsidian/wsl-container-compose/Plan.md`.

## Runtime dependency

- **`Microsoft.WSL.Containers`** (NuGet, v2.9.3) — the C# projection over the native WSL container feature (`Session` → `Container` → `Process`). This is the actual container engine the tool drives; there is no Docker/Podman involved. Its nuspec only declares a Windows-specific TFM (`net8.0-windows10.0.19041`), which is why every project in this solution targets `net10.0-windows10.0.19041.0` and the whole solution is Windows-only.

## CLI

- **`System.CommandLine`** (v2.0.9) — argument parsing for `WslContainerCompose.Cli`. All commands/options are wired in `Program.cs`; global options (`--file`, `--project-name`) must precede the subcommand, matching real `docker compose`'s convention.

## Compose parsing

- **`YamlDotNet`** (v18.1.0) — deserializes `docker-compose.yml` into untyped dictionaries, which `ComposeParser` then maps into `ComposeFile`/`ServiceDefinition`. No custom schema; the parser targets compatibility with the real Compose Specification (scoped down per `Plan.md`).

## Tests

- **xUnit** (v2.9.3) + **xunit.runner.visualstudio** — test framework.
- **coverlet.collector** — code coverage collection.
- All tests run against `FakeContainerRuntime`, never real WSL — see `CLAUDE.md` for why (`Session`/`Container` are concrete WinRT types, not mockable).

## CI/CD

- **GitHub Actions** (`.github/workflows/release.yml`) — `actions/checkout`, `actions/setup-dotnet` (pinned via `global.json`), `softprops/action-gh-release` for creating the GitHub Release and attaching the published zip.

## Explicitly not used

- **Docker / Podman** — this tool talks to the WSL container feature directly; it does not wrap either engine.
- **`dotnet tool` packaging** — `PackAsTool` is incompatible with the Windows-specific TFM this project requires (`NETSDK1146`). Distribution is a self-contained `win-x64` publish/zip instead (see `COMMANDS.md`).
