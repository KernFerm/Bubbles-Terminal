# Security Policy

## Supported Versions

| Version | Supported |
| --- | --- |
| 0.0.2 | Yes |

## Reporting A Vulnerability

For now, report security issues through the GitHub repository issue tracker:

```text
https://github.com/KernFerm/BubblesTerminal/issues
```

Please avoid posting sensitive secrets, passwords, private terminal output, or private file paths in public issues.

## Security Model

Bubbles CMD:

- Hosts real installed shells instead of reimplementing command behavior.
- Does not replace Windows system executables.
- Does not silently elevate privileges.
- Does not bypass UAC, PowerShell execution policy, AppLocker, antivirus, Windows Defender Application Control, or Group Policy.
- Does not upload commands, output, clipboard contents, settings, diagnostics, or crash reports.
- Shows paste warnings for multiline content, risky command patterns, and hidden control characters when enabled.

## Administrator Sessions

Administrator profile support is planned, but version `0.0.2` does not silently elevate or reuse elevated tokens.

## Plugin Runtime

Plugin manifest/catalog validation exists as scaffolding. Plugin execution is not enabled in version `0.0.2`.
