# COMMANDS.md

## Build & test

```powershell
dotnet build                                                             # build the whole solution
dotnet build --configuration Release
dotnet test                                                              # run all tests
dotnet test --filter "FullyQualifiedName~ComposeParserTests"             # run one test class
dotnet test --filter "FullyQualifiedName~ComposeParserTests.MethodName"  # run one test
```

## Run the CLI

```powershell
dotnet run --project src/WslContainerCompose.Cli -- --help
dotnet run --project src/WslContainerCompose.Cli -- up
dotnet run --project src/WslContainerCompose.Cli -- ps
dotnet run --project src/WslContainerCompose.Cli -- logs <service>
dotnet run --project src/WslContainerCompose.Cli -- down

# custom compose file / project name (global options come before the subcommand)
dotnet run --project src/WslContainerCompose.Cli -- -f deploy/docker-compose.yml -p myapp up
```

## Publish (what the release workflow does)

```powershell
dotnet publish src/WslContainerCompose.Cli/WslContainerCompose.Cli.csproj `
  --configuration Release --runtime win-x64 --self-contained true `
  -p:Version=<version> -p:PublishSingleFile=true `
  --output ./artifacts/publish/win-x64
```

`dotnet pack` on this project **fails** (`NETSDK1146`) — `PackAsTool` doesn't support the Windows-specific TFM. Don't use pack for distribution; the publish command above is the only working path.

## Coverage

```powershell
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

Produces a Cobertura report per test project under `./coverage/`. CI renders this into a job summary + HTML artifact (`coverage-report`) via `danielpalme/ReportGenerator-GitHub-Action`; to do the same locally, install `dotnet tool install --global dotnet-reportgenerator-globaltool` then run `reportgenerator "-reports:./coverage/**/coverage.cobertura.xml" "-targetdir:./coverage/report" "-reporttypes:Html"`.

## Releasing

- Push to a `release-*` branch → build+test (with coverage) only (validation).
- Push a tag matching `v[0-9]+.[0-9]+.[0-9]+` (e.g. `v0.1.0`) → build+test, then publish a `win-x64` zip and create a GitHub Release. See `.github/workflows/release.yml`.
- Tag format matters: `v.0.1.0` (stray dot) is invalid and will break the version-parsing step — use `v0.1.0`.

```powershell
git tag v0.1.0
git push origin v0.1.0
```
