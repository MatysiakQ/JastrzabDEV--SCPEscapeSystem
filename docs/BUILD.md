# Building and validation

The plugin targets .NET Framework 4.8 and is compiled against EXILED 9.12.6. Install the .NET SDK (8.0 or newer); on Windows also install the .NET Framework 4.8 Developer Pack. A newer EXILED/server combination still needs a server smoke test; version numbers alone do not establish compatibility.

Copy the following **matching server DLLs** into `../REFERENCES` relative to the repository, or specify a different directory with `ReferencesDirectory`:

- `Exiled.API.dll`, `Exiled.Events.dll`, `Exiled.CustomRoles.dll` from the same EXILED distribution.
- `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `UnityEngine.CoreModule.dll`, `Mirror.dll` from that server's managed assemblies/reference set.

Do not mix DLLs from unrelated game builds. Game libraries and their redistribution are not part of this repository. On non-Windows systems the SDK may need NuGet access to obtain Microsoft's .NET Framework reference assemblies.

Some Unity reference sets expose their core types through netstandard. The project uses the SDK's .NET Framework compatibility facade when available. If your SDK installs it elsewhere, set `NetStandardFacade` to the matching framework facade, for example with `-p:NetStandardFacade=/path/to/net461/lib/netstandard.dll`. This is a build reference; do not copy it into the server's plugin folder.

```sh
dotnet build SCPRUN.csproj --configuration Release -p:ReferencesDirectory=/absolute/path/to/server-references
dotnet run --project tests/Regression/Regression.csproj --configuration Release
```

The output is `bin/Release/net48/ScpEscapeSystem.dll`. Only this plugin DLL goes into the server's `EXILED/Plugins` directory. Remove the previous plugin DLL, including an old copy named `SCPRUN.dll`, before installing it to avoid loading two copies.

The regression executable has no external test packages or game libraries. It compiles the actual plugin source against a small simulated API and models event dispatch, deferred callbacks, denial, native escape counters and CustomRole cleanup. GitHub Actions runs these tests. It does not build a deployable plugin from the stubs, and does not establish Unity/Mono or client compatibility.

Before release, also test on a server: MTF/Chaos arrivals, denied role changes, the normal and cancelled death of the last physical SCP, SCP-079 recontainment timing, disconnects, plugin reloads and every custom SCP's teardown. The automatic tests do not run the native recontainment/CASSIE queue or third-party ability/schematic code.

## Validation of the included 1.4.0 DLL

- Actual project build: .NET Framework 4.8 / AnyCPU against available EXILED 9.12.6 references, successful.
- Additional actual project build: .NET Framework 4.8 / x64 against available EXILED 9.13.2 references, successful.
- Regression project: .NET 8, warnings treated as errors; 38 scenarios passed.
- The plugin builds emit MSB3277 for the netstandard 2.0/2.1 reference mismatch in the available native DLLs. The compatibility facade resolves compilation; this is not evidence of runtime compatibility. The warning is retained rather than suppressed. Validate on the intended server/Mono setup before public release.
- The DLL in `dist` is the AnyCPU build against 9.12.6. Its hash and validation limits are recorded beside it. No server or client session was run for this build, including on EXILED 9.15.2.
