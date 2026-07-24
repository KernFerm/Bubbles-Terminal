# Bubbles CMD Manual Test Plan

## Terminal Polish Smoke Checklist

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\terminal-smoke.ps1
```

Then manually verify available tools inside Bubbles CMD:

- `vim --version` and a short full-screen `vim` session when installed.
- `git commit` editor behavior in a test repository.
- `ssh -V` and an interactive SSH prompt when configured.
- `python` REPL input and exit behavior.
- `node` REPL input and exit behavior.
- `diskpart` launch and exit behavior from an administrator session.

These checks cover the native Windows terminal behaviors that are best validated in a real desktop session.

## Shell Hosting

1. Run the app:

   ```powershell
   $env:DOTNET_CLI_HOME="$PWD/.dotnet"
   $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1"
   dotnet run --project .\src\BubblesCmd.App\BubblesCmd.App.csproj
   ```

2. Start `Command Prompt`.
3. Run `echo BUBBLES_CONPTY_OK`.
4. Confirm the output appears inside the Bubbles CMD terminal view.
5. Confirm the startup banner and prompt, such as `C:\Users\<name>`, are readable against the terminal background.
6. Type `dir` at the prompt and confirm the characters appear immediately after `C:\Users\<name>` rather than at the bottom of the terminal.
7. Use Backspace, Delete, Left Arrow, and Right Arrow while typing and confirm they edit the current prompt line in place without duplicating or smearing characters.
8. Run `ver`, `dir`, and `echo %COMSPEC%`.
9. Run a batch syntax check:

   ```cmd
   for /L %i in (1,1,3) do @echo item-%i
   ```

10. Start `Windows PowerShell`.
11. Run `$PSVersionTable.PSEdition`, `Get-Command Get-ChildItem`, and `Get-Module -ListAvailable | Select-Object -First 3`.
12. Start the PowerShell 7 profile if detected and repeat the PowerShell checks.

## Terminal UI

1. Open multiple tabs with `New Tab`.
2. Duplicate the active tab.
3. Move a tab left and right with toolbar buttons and `Ctrl+Shift+Left/Right`, then confirm selection and terminal focus stay on the moved tab.
4. Restart a tab and confirm a new shell starts.
5. Rename a tab and confirm the title changes.
6. Pin a tab, close it, and confirm the pinned-tab warning appears.
7. Close a running tab and confirm the close warning appears.
8. Reopen the recently closed tab.
9. Force terminate a running session and confirm the termination warning appears.
10. Select terminal output and use `Copy`.
11. Put multiline text on the clipboard, use `Paste`, and confirm the review warning appears.
12. Put text containing an escape/control character on the clipboard, use `Paste`, and confirm the hidden-control-character review warning appears.
13. In a shell or editor that enables bracketed paste, paste multiline text and confirm it is bracket-wrapped instead of delivered as plain typed input.
14. Emit BEL with `cmd /c echo ^G` or `[char]7` from PowerShell and confirm the status area reports a bell without printing a raw control character.
15. Use `Clear` and confirm only visible scrollback is cleared.
16. Search for prior output with the search field and `Find`.
17. Open the command palette and run `Save output`.
18. Drag a file or folder into the terminal and confirm its quoted path is inserted without executing.
19. Run a command that emits ANSI colors and confirm colors render in output.
20. Run `cls` in `cmd.exe` and confirm the visible terminal clears.
21. Run `echo` or PowerShell output that writes OSC `0`/`2` title sequences and confirm the tab title updates without printing the raw sequence.
22. Open Commands from a CMD tab and confirm CMD built-ins such as `dir`, `cd`/`chdir`, and `copy` appear before PATH application entries.
23. Open Commands from a PowerShell tab and confirm PowerShell commands such as `Get-ChildItem` appear.
24. Select a command and confirm only the command name is inserted, without an automatic Enter.
25. Use `Split V` and confirm a second side-by-side shell pane starts.
26. Use `Split H` and confirm panes are rebuilt as stacked shells.
27. Use `Dup Pane` or `Ctrl+Shift+D` and confirm a new independent pane starts with the same profile as the focused pane.
28. Click different panes and confirm paste, search, snippets, commands, and terminate target the focused pane.
29. Use `Next Pane` or `Ctrl+Tab` and confirm focus moves to the next pane.
30. Use `Zoom Pane` or `Ctrl+Shift+Z` and confirm the active pane fills the tab, then use it again and confirm the split layout is restored.
31. Use `Close Pane` and confirm only the focused pane closes unless it is the final pane in the tab.

## Workspace Restore

1. Leave two running tabs open.
2. Close Bubbles CMD.
3. Reopen Bubbles CMD.
4. Confirm matching shell profiles are reopened without replaying previous commands.

## Settings And Snippets

1. Open settings.
2. Add a custom shell profile that points to an existing executable.
3. Save settings and confirm the profile appears in the profile picker.
4. Add a snippet.
5. Open snippets, select the new snippet, and confirm it is inserted into the active shell input without an automatic Enter.
6. Remove the snippet and custom profile.
7. Export settings to a JSON file.
8. Reset settings and confirm defaults return.
9. Import the exported settings and confirm snippets/custom profiles return.
10. Change terminal font size and confirm open tabs update after saving.
11. Enable high contrast and confirm terminal foreground/background switch to high-contrast colors.
12. Change foreground, background, and accent colors and confirm new terminal output uses them.

## Diagnostics And Privacy

1. Open About and confirm the settings and diagnostics paths are shown.
2. Enable local diagnostic logging in settings.
3. Start and close a tab.
4. Open diagnostics log from the command palette.
5. Confirm log entries include event names and profile/process metadata, not command text, clipboard text, or terminal output.
6. Clear diagnostics log from the command palette.

## Privacy And Safety

1. Confirm settings are stored under `%LOCALAPPDATA%\BubblesCmd\settings.json`.
2. Confirm no terminal output files are created by default.
3. Confirm no network access is required for normal shell use.
