# Development MCP plan

Build a Windows-only MCP server using standard input/output (stdio), separate from
the shipped WPF application. Stdio is the transport: the client sends messages to
the server's input stream and reads replies from its output stream.
Use the official C# MCP SDK and Windows UI Automation. Launch a disposable copy of
the application's build output, excluding Data, and scope every operation to that
owned process. Preserve session files for diagnosis; do not attach to normal user
instances or expose arbitrary execution through tools.

## Acceptance criteria

- Discover tools and launch/close an isolated GM Desktop session through MCP.
- Inspect a bounded control tree with stable automation IDs, enabled state, values,
  supported patterns, and temporary element references.
- Invoke buttons, set text, select rows/items, resize the window, and capture PNGs.
- Exercise validation, creation, navigation, persistence, and screenshots through
  focused MCP tests, with an independent session for each desktop case.
- Document setup, session lifetime, limitations, and what testing was performed.
- Run scripts/verify.ps1 and review the diff. Do not commit or publish.

## Outcome

Implemented ten tools exposed over standard input/output, project-local setup,
and stable UI automation IDs. Initial verification passed (53 tests, one opt-in
skip), and the original live MCP smoke test passed. Review revisions split the
tests into focused cases; current verification results are recorded in docs/testing.md.
A populated-library screenshot was visually reviewed.
The server is registered locally as gmDesktop; Codex must reload its MCP tools.
Manual physical-input checks remain outstanding; see docs/testing.md.
