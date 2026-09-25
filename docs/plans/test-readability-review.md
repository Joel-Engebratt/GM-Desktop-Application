# Test readability review

Address the five review comments without changing application or MCP behavior.

- Define stdio (standard input/output) on first use in both MCP documents.
- Replace tests combining independent behaviors with named focused cases.
- Extract repeated MCP client, session, and WPF rendering setup into small helpers.
- Review model, store, ViewModel, WPF, and MCP tests; preserve coverage and use
  data rows for independently reported input variants.
- Document one behavior per test and independent fixtures.
- Run full verification and the opt-in live MCP suite, then review the diff.

## Outcome

All five comments addressed. Both documents define stdio. MCP tests have focused
protocol, input, file-copy, and desktop cases. The application suite now reports
validation variants independently and separates storage, ViewModel, and rendering
behaviors. Test design conventions are recorded in docs/testing.md.

Final verification: 295 standard cases passed; 12 opt-in desktop cases passed in
a separate run. Release build had zero warnings and errors. No production behavior
changed, and no manual physical-input checks were performed.
