# Testing

## Automated verification

Run `./scripts/verify.ps1` on Windows. CI runs this same command.
Both MSTest projects under `tests` are included in the solution. Application tests
cover campaign validation, portable JSON storage, failure recovery, sorting, and
navigation. Nonparallel STA rendering tests check individual view states, bindings,
table selection, and input limits without opening desktop windows. They write
preview PNGs under the test output UiSmoke folder.

Add tests for domain rules, ViewModel state transitions, validation, and regression cases
as features arrive. Avoid opening WPF windows in ordinary unit tests. Tests requiring
WPF threading need an explicit STA/dispatcher strategy. Use isolated temporary data for I/O tests.

## Test design

- Test one behavior per case and name the test after its expected outcome. Split
  independent behaviors even when they happen in the same workflow.
- Keep arrange, act, and assert steps visually separate. Several assertions are
  appropriate when they jointly describe one outcome, such as a rejected save
  retaining input and leaving storage unchanged.
- Use data rows or dynamic data for input variants so each failure is reported
  independently. Do not loop through unrelated scenarios inside a test.
- Extract repeated setup, transport, polling, rendering, and cleanup into small
  helpers with descriptive names. Keep scenario decisions and outcome assertions
  visible in the test. Helpers should not hide a sequence of unrelated checks.
- Give each case fresh mutable state. Every MCP desktop test launches and closes
  its own disposable app. WPF rendering shares only the process-wide Application,
  dispatcher, and production resources; each case gets fresh views and ViewModels.
- Keep a multi-step integration scenario only when the steps establish a single
  outcome, such as reopening a saved campaign after restart. Avoid collecting
  unrelated validation, capture, and lifecycle checks in that same test.

## Test project structure

Group tests by the application area they cover, mirroring the source structure:

```text
tests/GM.Desktop.Tests/
|-- Models/
|   `-- CampaignDraftTests.cs
|-- Services/
|   `-- CampaignStoreTests.cs
|-- ViewModels/
|   |-- LibraryViewModelTests.cs
|   |-- CreateCampaignViewModelTests.cs
|   |-- ShellViewModelTests.cs
|   `-- CampaignTestData.cs
|-- Views/
|   |-- WpfRenderingTests.cs
|   |-- WpfRenderFixture.cs
|   `-- WpfTestDispatcher.cs
|-- MSTestSettings.cs
`-- GM.Desktop.Tests.csproj
```

- Match namespaces to folders, for example `GM.Desktop.Tests.Services`.
- Prefer one test class per production class; library, creation, and shell tests
  have separate classes.
- Keep domain validation tests in `Models/`; create additional area folders only when needed.
- Keep helpers beside their tests. Introduce `TestSupport/` only for helpers shared across areas.
- Keep assembly-wide configuration, including `MSTestSettings.cs`, at the project root.
- Keep STA rendering tests in `Views/`, separate from ordinary storage and ViewModel tests.

## Manual desktop smoke test

After UI changes:

1. Launch the application using the README instructions.
2. Confirm the main window opens without an error dialog.
3. Resize, minimize, and restore it; verify that layout remains usable.
4. Exercise the changed feature using its acceptance criteria, including invalid input where relevant.
5. Close the window and confirm the application exits.

Campaign launch acceptance checks:

- Empty library: creation action is available; Open is disabled.
- Create: blank fields show inline errors; choose Other / custom and enter a system name.
- Cancel/Back: no file is saved, and reopening the form starts blank.
- Save: campaign page opens only after success; Back restores the library and selects the new campaign.
- Create multiple campaigns, including duplicate and long names. Sort all columns in both directions;
  check visible direction, preserved selection, ellipsis/tooltips, and date/time readability.
- Exercise Tab, access keys, row arrows, Enter, double-click, and visible focus. Check the system dropdown.
- Resize to the minimum supported size, minimize, restore, and check scrolling and reachable form actions.
- Restart and confirm persistence. Launch from a different working directory; verify no data is written there.
- Close and move a disposable application/data copy to another writable folder; reopen and confirm the same campaigns.
- With disposable fixtures, add malformed metadata and confirm other campaigns still load with a folder warning.
- Exercise inaccessible root and failed-save states; Retry should recover and failed saves must retain form entries.
- Close the main window and confirm the process exits.
Record checks performed and any checks not run in the task or pull request summary.

## Development MCP tests

The solution also contains `GM.Development.Mcp.Tests`. Normal verification checks
stdio (standard input/output) tool discovery, tool error responses, argument bounds,
and exclusion of user data from disposable app copies. These cases are grouped in
`McpIntegrationTests`, `DevelopmentOptionsTests`, `SessionTests`, and `SessionFilesTests`.
`scripts/test-mcp-ui.ps1` opts into focused live cases in `DesktopMcpTests`, supported
by `McpTestClient` for shared transport and UI setup. See [development-mcp.md](development-mcp.md)
for its scope and the distinction between UI Automation and manual input checks.

### Verification record: test readability review (2026-09-25)

- Reviewed all application and MCP test classes for independent behaviors combined
  in one method. Split the combined cases and kept related outcome assertions together.
- `scripts/verify.ps1`: Release build passed with zero warnings and errors;
  275 application cases and 20 standard MCP cases passed. The 12 opt-in desktop
  cases were skipped in this run and passed separately via `scripts/test-mcp-ui.ps1`.
- The larger count primarily reflects existing validation inputs becoming separate
  data-driven cases. MCP desktop cases use independent disposable sessions; rendering
  cases use fresh views and load production resources without invoking App startup.
- No product behavior or visual layout was changed by this review. Manual physical
  input checks were not performed.

### Verification record: initial development MCP (2026-09-25)

- `scripts/verify.ps1`: Release build succeeded with zero warnings; 50 application
  tests and 3 MCP tests passed. The opt-in desktop test was skipped in this run.
- `scripts/test-mcp-ui.ps1`: passed separately on the normal Windows desktop.
  Exercised launch, bounded inspection, disabled-control rejection, combo expansion
  and collapse, stale references, blank-form validation, campaign creation, minimum
  window resize/capture, restart persistence, row selection, opening, and close.
- Visually reviewed the populated library PNG at minimum window size; controls and
  campaign text were rendered and reachable. Private-desktop captures were blank;
  the server now rejects uniform blank captures and documents that limitation.
- Manual keyboard/mouse, paste, minimize/restore, and the remaining desktop checklist
  were not performed. The earlier manual-test limitations remain historical records.

## Verification record: campaign launch milestone

The automated suite and offscreen WPF rendering are run during implementation. Generated renders are
inspected for the populated library, creation form, and minimum-size layouts. These checks are not
manual desktop testing. Native interaction, actual window resize/minimize/restore, application restart,
and moved-executable smoke checks remain pending because the Computer Use runtime cannot start
(`windows sandbox failed: helper_unknown_error: setup refresh had errors`). Storage relocation itself
is covered by an automated test. Record a new result when the desktop checklist is performed.

## Input safeguard regression coverage

Model tests cover exact and excessive field lengths, controls and bidi characters in both fields,
unpaired surrogates, valid Unicode/emoji/joiners, and code-looking literal names. Store tests verify
that direct writes cannot bypass validation, invalid saved text is reported and preserved, literal
injection-like names remain in GUID folders, and the 64 KiB boundary is enforced without losing
other campaigns. ViewModel tests check inline errors and retained input; STA rendering verifies
both text fields use the shared limits. Native paste/typing checks remain pending with the desktop
runtime unavailable; no additional manual interaction results are claimed.
