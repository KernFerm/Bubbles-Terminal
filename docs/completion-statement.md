# Completion Statement

`do.md` describes the target product for Bubbles CMD. Version `0.0.3` is complete as a buildable MVP foundation, not as a finished Windows Terminal replacement.

## Complete In 0.0.3

- Native Windows WPF app.
- Versioned assemblies and app metadata.
- Per-monitor DPI aware application manifest.
- ConPTY shell-hosting layer for real installed Windows shells.
- Dynamic shell profile detection.
- Dynamic local command discovery.
- CMD internal command discovery from installed `cmd.exe` help output.
- Tab/session management, tab move left/right, basic split panes with duplicate pane and pane zoom, workspace restore, snippets, command palette, settings, appearance, local diagnostics, and manual test plan.
- Paste review for multiline content, destructive/security-sensitive command patterns, and hidden control characters.
- Bracketed paste mode tracking and paste wrapping when requested by the active terminal program.
- BEL handling with visible app notification.
- Console-style screen buffer for prompt-line echo, carriage return, newline, backspace, cursor-left/right, erase-line, insert-blank, and delete-character behavior.
- High-contrast console-readable terminal defaults with migration from older saved settings.
- Lightweight ANSI/SGR rendering.
- OSC window-title sequence handling for tab title updates.
- ZIP packaging script for framework-dependent release artifacts.
- WiX MSI installer generation.
- Plugin manifest/catalog scaffold without plugin execution.

## Not Complete Yet

- Full VT terminal emulator.
- Advanced pane management: nested layouts, resize handles, pane swapping, and pane-to-tab conversion.
- Elevated ConPTY sessions.
- UI Automation-specific accessibility implementation.
- Plugin sandbox/execution runtime.
- Optional persistent command history index.
- Automatic update system.

These gaps are intentionally documented because claiming full completion would be misleading. The current codebase is ready for iterative development toward those larger pieces.
