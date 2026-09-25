# Architecture

## Current state

The solution contains a WPF application with App.xaml as its entry point. Startup constructs the campaign store and shell ViewModel, shows MainWindow, and asynchronously loads campaigns. Nullable reference types and implicit usings are enabled.

## Conventions for new features

- Views own layout and WPF-specific interaction. Code-behind may handle view-only behavior.
- ViewModels own presentation state and commands. Keep them independent of concrete windows.
- Models represent domain data and rules. Services perform I/O and external integration.
- Introduce `Views`, `ViewModels`, `Models`, and `Services` folders when they contain real code.
- Keep business rules independently testable; extract a non-WPF class library when the
  application has enough logic to justify it, rather than adding empty architectural layers.
- Use asynchronous I/O and marshal bound UI state changes to the dispatcher when needed.
- Use portable JSON storage for campaign metadata as described below; choose additional persistence only for concrete product needs.
- Keep runtime data outside source control. The existing `.gitignore` excludes `Data` folders.

Document consequential architecture decisions here, including the reason and tradeoffs.

## Development inspection

`tools/GM.Development.Mcp` is a separate Windows stdio MCP server using the official
C# SDK and UI Automation. It launches its own disposable copy of the built app and
excludes existing Data folders. This exercises real controls and normal application
storage without introducing an MCP dependency or listener into the product. Stable
automation IDs in the views support inspection and native control-pattern actions.
The server cannot attach to arbitrary processes. See [development-mcp.md](development-mcp.md)
for setup, lifecycle, and the limits of this approach compared with physical input.

## Campaign launch flow

The shell switches between library, creation, and campaign ViewModels using WPF data templates.
The retained library owns sorting and selection. Views handle layout, focus, and translating table
sort/open gestures into ViewModel operations. ViewModels are independent of concrete windows.
Commands prevent conflicting loads, saves, and navigation. Awaited ViewModel operations retain the
WPF synchronization context; the store performs directory and file work on background tasks.

`ICampaignStore` lists valid campaigns plus per-folder issues and creates campaigns from validated drafts.
`JsonCampaignStore` accepts a storage root and `TimeProvider` for tests. Production storage is
`AppContext.BaseDirectory/Data/Campaigns`, with one GUID directory per campaign and no separate index.
JSON uses camelCase properties and format version 1. Metadata includes ID, name, system ID,
custom system name, and UTC creation/update timestamps. Names are not paths, and future asset paths
must be relative to the campaign directory. The rules catalog initially contains only `custom`.

Creation flushes a temporary document in the same directory before renaming it to `campaign.json`.
It never overwrites another campaign. Failed saves attempt to remove only their temporary file and
empty new directory. Invalid files and unsupported versions are retained and reported while other
campaigns load. Root access errors propagate to the library error state. There is no AppData fallback.
The whole application folder is portable; build output is not a durable backup. One running instance
per data folder is assumed. Editing, migration from older formats, and distribution packaging are deferred.

## Campaign input safeguards

Creation and loading share `CampaignText` validation: campaign names allow at most 200 UTF-16
code units and custom system names 100, measured before trimming. WPF fields use the same limits.
Reject C0/C1 controls (including tabs/newlines), Unicode line/paragraph separators, explicit bidi
marks/embeddings/overrides/isolates and deprecated directional controls, and unpaired surrogates.
Allow ordinary punctuation, accents, RTL scripts, combining marks, and emoji including joiners.
Do not interpret names as code or paths; serialize them with System.Text.Json and display plain text.

Each metadata file is limited to 64 KiB. Check its length before reading and bound the read to
limit-plus-one bytes before parsing, so a changing file cannot cause an unbounded allocation.
The shared validators also reject invalid text from manually edited saves. Existing files that
violate these limits are reported and left unchanged, not truncated or migrated automatically.
These per-field and per-file bounds do not impose a total library size limit or sandbox local files.
