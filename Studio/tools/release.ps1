# The GitHub release of the zips package.ps1 made: Studio\tools\release.ps1 [-Draft | -Publish] [-PreRelease]
# With neither switch it only checks, and says what it would run. It never pushes: push the branch first, yourself.
# The tag is put on the very commit the exe was built from, so the source GitHub shows is the exe's own.
param([switch]$Draft, [switch]$Publish, [switch]$PreRelease)
$ErrorActionPreference = 'Stop'
$studio = Split-Path -Parent $PSScriptRoot
$repo = Split-Path -Parent $studio
$dist = Join-Path $repo 'dist'
[xml]$xml = Get-Content (Join-Path $studio 'RvStudio.App\RvStudio.App.csproj')
$version = ($xml.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
$tag = "v$version"
$zip = Join-Path $dist "RvCarStudio-$version-win-x64.zip"
$src = Join-Path $dist "RvCarStudio-$version-source.zip"
$tgz = Join-Path $dist "RvCarStudio-$version-linux-x64.tar.gz"
$notes = Join-Path $studio 'RELEASE_NOTES.md'
$exe = Join-Path $dist "RvCarStudio-$version-win-x64\RvCarStudio.exe"
$wrong = @()

foreach ($f in @($zip, $tgz, $src, $notes, $exe)) { if (-not (Test-Path $f)) { $wrong += "Missing: $f (run package.ps1)" } }
$head = (git -C $repo rev-parse HEAD).Trim()
if (git -C $repo status --porcelain -- Studio docs README.md RvCarStudio/data/readme RvCarStudio/data/Tips) { $wrong += 'Studio has changes that are not committed' }
if (Test-Path $exe) {
    $built = (Get-Item $exe).VersionInfo.ProductVersion
    if ($built -ne "$version+$head") { $wrong += "The exe is $built, the commit is ${head}: run package.ps1 again" }
}
if ((Test-Path $zip) -and (Test-Path $exe) -and ((Get-Item $zip).LastWriteTime -lt (Get-Item $exe).LastWriteTime)) { $wrong += 'The zip is older than the exe' }

# What GitHub has: the commit (the tag goes on it), and no such tag or release yet
$name = (gh repo view --json nameWithOwner --jq .nameWithOwner)
if ($LASTEXITCODE -ne 0) { throw 'gh cannot see the repository: gh auth login' }
git -C $repo fetch origin --quiet
$on = git -C $repo branch -r --contains $head
if (-not $on) { $wrong += 'This commit is not on GitHub yet: push its branch first' }
if (git -C $repo ls-remote --tags origin "refs/tags/$tag") { $wrong += "$tag is already on GitHub" }
$there = gh release list --json tagName --jq '.[].tagName'
if ($there -contains $tag) { $wrong += "There is a release $tag already (a draft, perhaps)" }

# Checksums of the archives, to download beside them
$sums = Join-Path $dist 'SHA256SUMS.txt'
if ((Test-Path $zip) -and (Test-Path $src) -and (Test-Path $tgz)) {
    $lines = foreach ($f in @($zip, $tgz, $src)) { (Get-FileHash $f -Algorithm SHA256).Hash.ToLower() + '  ' + (Split-Path -Leaf $f) }
    [IO.File]::WriteAllText($sums, ($lines -join "`n") + "`n")
}

$args2 = @('release', 'create', $tag, $zip, $tgz, $src, $sums, '--repo', $name, '--target', $head, '--title', "RV Car Studio $version", '--notes-file', $notes)
if (-not $Publish) { $args2 += '--draft' }
if ($PreRelease) { $args2 += '--prerelease' } else { $args2 += '--latest' }

Write-Host "Repository $name, $tag on $($head.Substring(0, 7))"
Get-Content $sums -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $_" }
if ($wrong) {
    Write-Host 'Not ready:' -ForegroundColor Yellow
    $wrong | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    exit 1
}
if (-not $Draft -and -not $Publish) {
    Write-Host 'Ready. -Draft makes it as a draft only you see, -Publish makes it public. It would run:'
    Write-Host ('  gh ' + ($args2 -join ' '))
    exit 0
}
& gh @args2
if ($LASTEXITCODE -ne 0) { throw "gh release create failed ($LASTEXITCODE)" }
