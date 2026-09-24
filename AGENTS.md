# Project instructions

## Context

GM Desktop Application is a C# WPF application targeting `net10.0-windows`.
Read `README.md` for commands and `docs/architecture.md` before structural changes.
The solution is `GM Desktop Application/GM Desktop Application.slnx`.
Application sources are in `GM Desktop Application/GM Desktop Application/`.

## Working conventions

- Keep changes focused on the task and its acceptance criteria. Preserve unrelated edits.
- Follow `.editorconfig`, nullable annotations, and existing naming conventions.
- Use MVVM as features are introduced: keep business logic out of window code-behind.
- Keep UI operations on the WPF dispatcher; avoid blocking I/O on the UI thread.
- Do not add dependencies or abstractions without a concrete use in the task.
- Do not commit secrets, local user data, `.vs`, `bin`, `obj`, or test results.
- For substantial work, record a short plan and acceptance criteria before implementation.
- Use `codex/` branches for agent changes. Use separate worktrees for concurrent tasks.
- Do not create commits, push, merge, or publish unless the task authorizes it.

## Verification and completion

- Run `./scripts/verify.ps1` from PowerShell on Windows after code or build changes.
- Add meaningful tests for new logic and bug fixes; avoid tests that only repeat implementation.
- Add every new project to the solution so local verification and CI include it.
- For UI changes, follow `docs/testing.md` and report which manual checks were performed.
- Review the diff for unintended changes and update affected documentation.
- Report changes, verification results, and limitations. Never describe unrun checks as passing.
