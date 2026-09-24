# GM Desktop Application

A Windows desktop application built with C# and WPF on .NET 10.
The launch view lists your campaigns, supports sorting by name, rules system, and last updated, and lets you create and open campaigns.

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

## Portable campaign data

Campaigns are saved beside the executable in `Data/Campaigns/<campaign-id>/campaign.json`.
Move or back up the whole application folder, including `Data`, to retain your campaigns.
The launch working directory does not affect storage. There is no AppData fallback.
When running from source, data lives beside the executable in the build output directory;
back it up before cleaning or replacing that directory. Distribution packaging is not yet provided.
Use a writable location and only one application instance per data folder.

Create a campaign with a name and **Other / custom** rules system, then enter its system name.
Custom systems provide campaign organization only; no character sheets or rules assistance are included.
Click column headings to sort, select a row and choose **Open campaign**, or double-click/press Enter on a row.
Opening a campaign does not update its modification timestamp. Editing and deletion are not yet implemented.
Unreadable campaigns are reported individually without changing their files. A library read error offers Retry;
a save failure keeps the form entries so you can correct the storage problem and retry.

Campaign names are limited to 200 characters and system names to 100 (UTF-16 code units;
some emoji use multiple units). Control characters, line breaks, and explicit text-direction controls
are rejected. Normal Unicode names and punctuation are supported. Campaign metadata files larger
than 64 KiB or containing invalid text are reported without changing the files.
