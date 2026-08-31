# setup-github.ps1  (v2)
# Puts the Cold-Chain solution on GitHub and explains any failure in plain terms.
#
# IMPORTANT: this file must sit in the SAME folder as ColdChainGateway.sln.
# It refuses to run anywhere else, so it cannot turn your Downloads folder into
# a repository by accident.
#
# To run: right click the file, Run with PowerShell. If Windows blocks that,
# open a terminal in the folder and run:
#   powershell -ExecutionPolicy Bypass -File .\setup-github.ps1
#
# It never asks for a password or token. GitHub sign-in happens in your browser,
# handled by Git Credential Manager.

# Native commands like git write ordinary messages to stderr. With "Stop" that
# gets treated as a crash, so failures are checked with $LASTEXITCODE instead.
$ErrorActionPreference = "Continue"

$RepoUrl = "https://github.com/ST10439941-Shane-Pelser/ColdChainGateway.git"

function Say([string]$text)  { Write-Host "`n$text" -ForegroundColor Cyan }
function Ok([string]$text)   { Write-Host "  OK  $text" -ForegroundColor Green }
function Warn([string]$text) { Write-Host "  !!  $text" -ForegroundColor Yellow }
function Die([string]$text)  { Write-Host "`nSTOPPED. $text" -ForegroundColor Red; Read-Host "`nPress Enter to close"; exit 1 }

Set-Location -LiteralPath $PSScriptRoot
Say "Working in $PSScriptRoot"

# ---------------------------------------------------------------- safety check

$solutions = @(Get-ChildItem -Path $PSScriptRoot -Filter *.sln -File -ErrorAction SilentlyContinue)

if ($solutions.Count -eq 0) {
    Write-Host ""
    Write-Host "There is no .sln file in this folder, so this is not the solution folder." -ForegroundColor Red
    Write-Host ""
    Write-Host "Move setup-github.ps1 into the folder that contains ColdChainGateway.sln"
    Write-Host "and run it from there. Your path is nested, so check both of these:"
    Write-Host "  C:\Users\shane\Downloads\ColdChainGateway"
    Write-Host "  C:\Users\shane\Downloads\ColdChainGateway\ColdChainGateway"
    Write-Host ""
    Write-Host "If a repo was created in the wrong folder, delete the .git folder there:"
    Write-Host "  Remove-Item -Recurse -Force <that folder>\.git"
    Die "Wrong folder."
}

Ok "Found $($solutions[0].Name)"

# ---------------------------------------------------------------- git present?

try { git --version | Out-Null }
catch { Die "Git is not installed, or not on PATH. Install Git for Windows from git-scm.com, then run this again." }

# ---------------------------------------------------------------- repository

if (-not (Test-Path ".git")) {
    Say "No git repository here yet. Creating one."
    git init | Out-Null
    Ok "Repository created"
} else {
    Ok "Git repository already present"
}

# ---------------------------------------------------------------- identity

$name  = git config user.name  2>$null
$email = git config user.email 2>$null

if ([string]::IsNullOrWhiteSpace($name)) {
    $name = Read-Host "Your name for commit messages (e.g. Shane Pelser)"
    git config user.name "$name" | Out-Null
}
if ([string]::IsNullOrWhiteSpace($email)) {
    $email = Read-Host "The email address on your GitHub account"
    git config user.email "$email" | Out-Null
}
Ok "Committing as $(git config user.name) <$(git config user.email)>"

# ---------------------------------------------------------------- gitignore

if (-not (Test-Path ".gitignore")) {
    Say "No .gitignore. Writing one so build output stays out of the repo."
@"
bin/
obj/
.vs/
*.user
ColdChain.Api/Uploads/
"@ | Set-Content -Path ".gitignore" -Encoding UTF8
    Ok ".gitignore created"
} else {
    Ok ".gitignore already present"
}

# If build output was committed on an earlier attempt, stop tracking it.
$tracked = git ls-files "bin" "obj" "*/bin/*" "*/obj/*" 2>$null

if ($tracked) {
    Say "Build output was committed before. Removing it from tracking (files stay on disk)."
    git rm -r --cached --ignore-unmatch --quiet bin obj */bin */obj 2>$null
    Ok "Build output untracked"
}

# ---------------------------------------------------------------- remote

$remotes = @(git remote 2>$null)

if ($remotes -notcontains "origin") {
    git remote add origin $RepoUrl 2>$null
    Ok "Remote 'origin' set to $RepoUrl"
} else {
    $existing = (git remote get-url origin 2>$null | Select-Object -First 1)

    if ([string]::IsNullOrWhiteSpace($existing)) {
        git remote set-url origin $RepoUrl 2>$null
        Ok "Remote 'origin' set to $RepoUrl"
    } elseif ($existing.Trim().TrimEnd('/') -ne $RepoUrl.TrimEnd('/')) {
        Warn "Remote was $($existing.Trim())"
        git remote set-url origin $RepoUrl 2>$null
        Ok "Remote corrected to $RepoUrl"
    } else {
        Ok "Remote already correct"
    }
}

# ---------------------------------------------------------------- access check

Say "Checking whether your credentials can see the repository."
Write-Host "  A browser window may open asking you to sign in. Use the account that owns the repo."

git ls-remote $RepoUrl 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Cannot see that repository with the credentials Windows is sending." -ForegroundColor Red
    Write-Host ""
    Write-Host "GitHub says 'not found' rather than 'access denied' for private repos, so this"
    Write-Host "almost always means a different GitHub account is cached on this machine."
    Write-Host ""
    Write-Host "Fix it, then run this script again:" -ForegroundColor Yellow
    Write-Host "  1. Run:  git credential-manager github logout"
    Write-Host "     If that is not recognised, open Credential Manager from the Start menu,"
    Write-Host "     go to Windows Credentials, and delete every entry starting with"
    Write-Host "     git:https://github.com"
    Write-Host "  2. In Visual Studio, File then Account Settings, and remove any GitHub account"
    Write-Host "     that is not ST10439941-Shane-Pelser."
    Write-Host "  3. Open the repo in a browser signed in as that account. If there is no Settings"
    Write-Host "     tab, you do not own it and cannot push to it."
    Die "Credentials need sorting first."
}

Ok "Repository is visible to you"

# ---------------------------------------------------------------- commit

Say "Staging and committing."
git add -A 2>$null

$staged = @(git diff --cached --name-only 2>$null)

if ($staged.Count -eq 0) {
    Ok "Nothing new to commit"
} else {
    git commit -m "Cold-chain gateway: API, WinForms client and web client" --quiet 2>$null
    Ok "Committed $($staged.Count) file(s)"
}

# ---------------------------------------------------------------- push

git branch -M main 2>$null
Say "Pushing to GitHub."

git push -u origin main

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "The push was refused." -ForegroundColor Red
    Write-Host "If the message mentions rejected or non-fast-forward, the GitHub repo already has"
    Write-Host "commits on it, usually a README added when it was created. Merge them first:"
    Write-Host "  git pull origin main --allow-unrelated-histories"
    Write-Host "then run this script again."
    Die "See the message above."
}

Write-Host ""
Ok "Pushed. Your code is at https://github.com/ST10439941-Shane-Pelser/ColdChainGateway"
Write-Host "Future pushes are just: git push" -ForegroundColor Green
Read-Host "`nPress Enter to close"
