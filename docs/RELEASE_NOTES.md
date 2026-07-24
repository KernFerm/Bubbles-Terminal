# Bubbles CMD 0.0.4

This `0.0.4` release focuses on terminal polish, settings reliability, richer custom profiles, and stronger release automation on top of the existing MVP foundation.

## Highlights

- Terminal output parsing now feeds bracketed paste state, BEL notifications, and OSC window-title updates back into the app.
- Terminal appearance settings now affect more of the hosted terminal experience, including theme colors, selection color, cursor style, and spacing.
- Custom profiles now support startup commands, tab title templates, icon glyph overrides, and per-profile environment overrides.
- Command discovery now uses caching and fuzzy search ranking, and the command browser shows richer command details.
- Settings persistence now uses atomic saves and repaired-settings fallback behavior when corrupted JSON is encountered.
- The automated test suite now uses xUnit with `dotnet test`, and CI validates both test results and packaged release artifacts.

## User-Facing Changes

- The top toolbar is less crowded, with secondary actions leaning more on the command palette and keyboard shortcuts.
- Search, selection export, and output save behavior are closer to what the UI implies when working with terminal content.
- Workspace persistence can now remember more tab/profile startup context.

## Developer-Facing Changes

- The old custom executable-style tests were replaced by an xUnit suite.
- GitHub Actions now uploads test results and explicitly checks that ZIP/MSI release artifacts were produced.
- Versioned release/install/package references were synchronized to `0.0.4`.

## Known Gaps

- No signed MSI/MSIX installer yet; Inno Setup and PowerShell installer scaffolding are still included for local packaging.
- Fully integrated elevated ConPTY tabs are still future work; administrator-requested profiles currently prompt for an elevated app restart.
- Plugin support is still manifest/catalog scaffolding only; plugin execution is not enabled.
- Advanced nested pane layouts are still future work.
