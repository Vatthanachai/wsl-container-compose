# Progress

Living status log. Add a new entry at the top each time meaningful progress happens; don't rewrite history.

## 2026-07-09 — Solution scaffolded, builds and tests pass

- Added `WslContainerCompose.Core`, `WslContainerCompose.Cli`, and `WslContainerCompose.Core.Tests` to `WSL-Container-Compose.slnx` (`dotnet sln add` works directly on `.slnx`). Both `Core` and `Cli` target `net10.0-windows10.0.19041.0` (confirmed required — `Microsoft.WSL.Containers` 2.9.3's nuspec declares `net8.0-windows10.0.19041` as its TFM).
- `Core` has: `Compose/` (`ComposeFile`, `ServiceDefinition`, `ComposeParser` using YamlDotNet, `EnvInterpolation` for `${VAR:-default}`), `Runtime/` (`IContainerRuntime` abstraction + `ContainerSpec`/`ContainerStatus`), `State/` (`ProjectState` + `ProjectStateStore`, JSON file per project), `Orchestration/` (`DependencyOrder` topological sort, `ComposeProject` driving up/down/ps/logs/stop/start/restart against `IContainerRuntime`).
- `Cli` wires `up`/`down`/`ps`/`logs`/`stop`/`start`/`restart` via System.CommandLine 2.0.9. Verified manually: `--help` renders correctly; `wsl-compose --file <compose.yml> up` parses a real compose file, resolves `${VAR}` interpolation, and reaches orchestration correctly (fails at `CreateSessionAsync` as expected — see below).
- 15 unit tests in `Core.Tests` pass: parser (image/ports/environment/depends_on/bind-mounts/interpolation, rejecting `build:`/named volumes), `DependencyOrder` (ordering, circular/unknown dependency errors), `ComposeProject` (up/down lifecycle, partial-failure handling leaves prior containers running per [[Plan]]) — all against a `FakeContainerRuntime` test double, no real WSL needed.
- **Deliberately not implemented yet:** the real adapter from `IContainerRuntime` to `Microsoft.WSL.Containers`' `Session`/`Container`/`Process`. `Cli` currently constructs a `NotImplementedContainerRuntime` that throws a clear `NotImplementedException` naming the unimplemented member — this is why `up` fails after successfully parsing/ordering. Confirmed by manually running `up` against a sample compose file.
- Note: global CLI options (`--file`, `--project-name`) must come *before* the subcommand (`wsl-compose --file x.yml up`, not `wsl-compose up --file x.yml`) — matches real `docker compose`'s own convention, so left as-is.

## 2026-07-09 — Planning complete, no code yet

- Repo initialized (`7c0590d Initial new project`): `.editorconfig`, `.gitattributes`, `.gitignore`, `global.json` (SDK 10.0.301), `WSL-Container-Compose.slnx`, empty `src/` and `tests/`.
- Ran a full grilling session to design the tool end to end. Outcome captured in [[Plan]].
- Obsidian vault set up with [[Index]], [[Plan]], [[Project Structure]], [[How To]], [[References]].
- **Not started yet:** no projects added to the `.slnx`, no code written, no compose parser, no `IContainerRuntime` abstraction, no CLI commands.

## Next up

- Implement the real `IContainerRuntime` adapter over `Microsoft.WSL.Containers`' `Session`/`Container`/`Process`, and swap it in for `NotImplementedContainerRuntime` in `Cli`'s `LoadProject`.
- Verify end-to-end against a real WSL container feature install (everything so far has only been verified with the fake runtime).
- Consider friendlier CLI error output instead of a raw stack trace when orchestration throws.

## Related

[[Plan]] · [[Index]]
