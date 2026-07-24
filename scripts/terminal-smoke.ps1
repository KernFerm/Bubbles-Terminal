$ErrorActionPreference = "Stop"

$checks = @(
    @{ Name = "vim"; Command = "vim --version" },
    @{ Name = "git"; Command = "git --version" },
    @{ Name = "ssh"; Command = "ssh -V" },
    @{ Name = "python"; Command = "python --version" },
    @{ Name = "node"; Command = "node --version" },
    @{ Name = "diskpart"; Command = "diskpart /?" }
)

Write-Host "Bubbles CMD terminal polish checklist"
Write-Host "Run these manually inside Bubbles CMD where available:"
Write-Host ""

foreach ($check in $checks) {
    $available = $false
    try {
        $null = Get-Command $check.Name -ErrorAction Stop
        $available = $true
    }
    catch {
    }

    $status = if ($available) { "available" } else { "not found" }
    Write-Host ("[{0}] {1} - {2}" -f $status, $check.Name, $check.Command)
}

Write-Host ""
Write-Host "Manual verification targets:"
Write-Host "- Backspace, Delete, arrows, Home, End, Space, Tab"
Write-Host "- Full-screen TUIs such as vim or git commit editor"
Write-Host "- SSH prompts and password prompts"
Write-Host "- Python and Node REPL input"
Write-Host "- Diskpart or other admin/interactive console tools"
