# References

External documentation this project's design is based on.

- [WSL container | Microsoft Learn](https://learn.microsoft.com/en-us/windows/wsl/wsl-container?tabs=csharp) — overview of `wslc.exe` and the `Microsoft.WSL.Containers` NuGet package, including C#/C++ sample code for `Session`/`Container`/`Process`.
- [WSL container API reference](https://wsl.dev/api-reference/) — full API surface: `WslcService`, `Session`, `Container`, `Process`, image operations (pull/import/load/tag/push/delete/list), `WslcContainerPortMapping`, `WslcContainerVolume` / `WslcContainerNamedVolume`, `WslcSetContainerSettingsNetworkingMode`.
- [Compose Specification](https://github.com/compose-spec/compose-spec) — the docker-compose YAML schema v1 targets compatibility with.
- `Microsoft.WSL.Containers` — NuGet package name for the C# projection used by `WslContainerCompose.Core`.

## Related

[[Plan]] · [[Index]]
