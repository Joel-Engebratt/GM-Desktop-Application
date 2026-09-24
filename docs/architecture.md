# Architecture

## Current state

The solution contains a WPF application with `App.xaml` as its entry point and
`MainWindow.xaml` as the startup window. Nullable reference types and implicit
usings are enabled. There is no business logic or persistence implementation yet.

## Conventions for new features

- Views own layout and WPF-specific interaction. Code-behind may handle view-only behavior.
- ViewModels own presentation state and commands. Keep them independent of concrete windows.
- Models represent domain data and rules. Services perform I/O and external integration.
- Introduce `Views`, `ViewModels`, `Models`, and `Services` folders when they contain real code.
- Keep business rules independently testable; extract a non-WPF class library when the
  application has enough logic to justify it, rather than adding empty architectural layers.
- Use asynchronous I/O and marshal bound UI state changes to the dispatcher when needed.
- Select persistence based on product requirements. No database or storage format is prescribed yet.
- Keep runtime data outside source control. The existing `.gitignore` excludes `Data` folders.

Document consequential architecture decisions here, including the reason and tradeoffs.
