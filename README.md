# GM Desktop Application

A Windows desktop application built with C# and WPF on .NET 10.
The current application is an empty starter window; product features are not implemented yet.

## Prerequisites

- Windows for building, running, and testing the WPF application.
- .NET SDK 10.0.401 or a newer patch in the 10.0.4xx feature band, selected by `global.json`.
- Windows PowerShell 5.1 or PowerShell 7.
- Optional: a Visual Studio version supporting .NET 10, with the .NET desktop development workload.

## Build and verify

From the repository root:

```powershell
./scripts/verify.ps1
```

The script restores and builds the solution in Release, then runs its tests.
It stops on any failed command. It also checks the standard Windows .NET installation
when `dotnet` is missing from PATH. Use `-Configuration Debug` for a Debug build.

## Run

```powershell
dotnet run --project "GM Desktop Application/GM Desktop Application/GM Desktop Application.csproj"
```

If `dotnet` is not on PATH, use the installed executable directly:

```powershell
& "$env:ProgramFiles/dotnet/dotnet.exe" run --project "GM Desktop Application/GM Desktop Application/GM Desktop Application.csproj"
```

Alternatively, open `GM Desktop Application/GM Desktop Application.slnx` in Visual Studio.

## Development

- [Agent instructions](AGENTS.md)
- [Architecture conventions](docs/architecture.md)
- [Automated tests and UI smoke test](docs/testing.md)

Define acceptance criteria, implement a focused change, run verification, and review the diff.
GitHub Actions runs the same verification script on Windows for pushes and pull requests.
Repository branch protection must be configured separately if required checks are desired.
