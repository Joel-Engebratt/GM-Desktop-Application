# Testing

## Automated verification

Run `./scripts/verify.ps1` on Windows. CI runs this same command.
The MSTest project under `tests` is included in the solution and references the application.
It is prepared for future behavior tests; the starter window has no business behavior to test.
An empty test suite is not evidence of functional coverage.

Add tests for domain rules, ViewModel state transitions, validation, and regression cases
as features arrive. Avoid opening WPF windows in ordinary unit tests. Tests requiring
WPF threading need an explicit STA/dispatcher strategy. Use isolated temporary data for I/O tests.

## Manual desktop smoke test

After UI changes:

1. Launch the application using the README instructions.
2. Confirm the main window opens without an error dialog.
3. Resize, minimize, and restore it; verify that layout remains usable.
4. Exercise the changed feature using its acceptance criteria, including invalid input where relevant.
5. Close the window and confirm the application exits.

For the initial empty window, steps 2, 3, and 5 are the applicable checks.
Record checks performed and any checks not run in the task or pull request summary.
