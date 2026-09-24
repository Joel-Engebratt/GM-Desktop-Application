# Testing

## Automated verification

Run `./scripts/verify.ps1` on Windows. CI runs this same command.
The MSTest project under `tests` is included in the solution and references the application.
It covers campaign validation, portable JSON storage, failure recovery, sorting, and navigation. A separate nonparallel STA test renders the WPF views without opening desktop windows, checks binding errors and real table selection, and writes preview PNGs under the test output UiSmoke folder.

Add tests for domain rules, ViewModel state transitions, validation, and regression cases
as features arrive. Avoid opening WPF windows in ordinary unit tests. Tests requiring
WPF threading need an explicit STA/dispatcher strategy. Use isolated temporary data for I/O tests.

## Test project structure

Group tests by the application area they cover, mirroring the source structure:

```text
tests/GM.Desktop.Tests/
|-- Models/
|   `-- CampaignDraftTests.cs
|-- Services/
|   `-- CampaignStoreTests.cs
|-- ViewModels/
|   `-- CampaignViewModelTests.cs
|-- Views/
|   `-- WpfRenderingTests.cs
|-- MSTestSettings.cs
`-- GM.Desktop.Tests.csproj
```

- Match namespaces to folders, for example `GM.Desktop.Tests.Services`.
- Prefer one test class per production class. As ViewModel coverage grows, split the current
  campaign tests into library, creation, and shell test classes.
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
