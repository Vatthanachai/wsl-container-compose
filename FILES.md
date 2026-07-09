# FILES.md

Directory map for AI agents. For narrative design/architecture, see `CLAUDE.md` and `obsidian/wsl-container-compose/Project Structure.md`.

```
WSL-Container-Compose.slnx          # solution file (.slnx format, not .sln)
global.json                         # pins .NET SDK 10.0.301

src/
├── WslContainerCompose.Cli/        # System.CommandLine entry point
│   └── Program.cs                  # all CLI wiring lives here - options, commands, LoadProject
└── WslContainerCompose.Core/       # compose parsing + orchestration (net10.0-windows10.0.19041.0)
    ├── Compose/                    # ComposeParser, ComposeFile, ServiceDefinition, EnvInterpolation
    ├── Runtime/                    # IContainerRuntime abstraction, ContainerSpec, NotImplementedContainerRuntime
    ├── Orchestration/              # DependencyOrder (topological sort), ComposeProject (lifecycle driver)
    └── State/                      # ProjectState, ProjectStateStore (JSON state in .wsl-compose/)

tests/
└── WslContainerCompose.Core.Tests/
    ├── Compose/ComposeParserTests.cs
    └── Orchestration/              # DependencyOrderTests, ComposeProjectTests, FakeContainerRuntime

.github/workflows/release.yml       # build+test on release-* branches; publish+release on v[0-9]+.[0-9]+.[0-9]+ tags

obsidian/wsl-container-compose/     # design record - start at Index.md
├── Plan.md                        # authoritative v1 scope, what's supported/deferred and why
├── Progress.md                    # living status log + "Next up"
├── Project Structure.md
├── How To.md
└── References.md                  # external API docs this design is based on
```

## Root-level agent references

- `CLAUDE.md` — for Claude Code specifically.
- `AGENTS.md` — same content, for other agents.
- `FILES.md` — this file.
- `COMMANDS.md` — build/test/run/release commands.
- `TOOLS.md` — key libraries and why.
- `MEMORY.md` — gotchas worth knowing before acting.
