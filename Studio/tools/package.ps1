# Builds the portable zip and the source zip: Studio\tools\package.ps1 [-Keep]
# A self-contained single-file RvCarStudio.exe for win-x64 (no .NET install needed) with the native DLLs beside it,
# the readme templates, README, licence and notices; and, for the GPL, the source of the commit it was built from.
# The source zip holds the working files, so it always matches the exe.
# The build goes outside Dropbox (RVS_OBJ and the artifacts path in %TEMP%), so a sync client doesn't fight it.
param([switch]$Keep)
$ErrorActionPreference = 'Stop'
$studio = Split-Path -Parent $PSScriptRoot
$repo = Split-Path -Parent $studio
$project = Join-Path $studio 'RvStudio.App\RvStudio.App.csproj'
[xml]$xml = Get-Content $project
$version = ($xml.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
if (-not $version) { throw 'No <Version> in RvStudio.App.csproj' }
$name = "RvCarStudio-$version-win-x64"
$dist = Join-Path $repo 'dist'
$work = Join-Path $env:TEMP 'RvCarStudio-package'
$stage = Join-Path $dist $name
$zip = Join-Path $dist "$name.zip"
$src = Join-Path $dist "RvCarStudio-$version-source.zip"
function Retry([scriptblock]$do) {
    for ($try = 1; ; $try++) {
        try { & $do; return }
        catch { if ($try -ge 15) { throw }; Start-Sleep -Seconds 2 }
    }
}
foreach ($old in @($stage, $zip, $src, $work)) { Retry { if (Test-Path $old) { Remove-Item $old -Recurse -Force -Confirm:$false } } }
New-Item -ItemType Directory -Force $stage | Out-Null

$env:RVS_OBJ = Join-Path $work 'obj'
dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=false -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none -p:DebugSymbols=false -o (Join-Path $work 'publish') --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }
$published = Join-Path $work 'publish'
if (-not (Test-Path (Join-Path $published 'RvCarStudio.exe'))) { throw "No exe in $published" }

Copy-Item (Join-Path $published '*') $stage -Recurse
Get-ChildItem $stage -Filter *.pdb -Recurse | Remove-Item -Force -Confirm:$false
Copy-Item (Join-Path $studio 'README.md') $stage
Copy-Item (Join-Path $studio 'THIRD_PARTY.md') $stage
Copy-Item (Join-Path $repo 'LICENSE') (Join-Path $stage 'LICENSE.txt')

# The source: the files the exe was built from, as they are now (tracked and new, not what git ignores)
$head = (git -C $repo rev-parse --short HEAD).Trim()
$dirty = git -C $repo status --porcelain -- Studio RvCarStudio/data/readme RvCarStudio/data/Tips
if ($dirty) { $head += '+changes' }
$files = git -C $repo ls-files -co --exclude-standard -- Studio RvCarStudio/data/readme RvCarStudio/data/Tips LICENSE
if ($LASTEXITCODE -ne 0) { throw "git ls-files failed ($LASTEXITCODE)" }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Retry {
    if (Test-Path $src) { Remove-Item $src -Force -Confirm:$false }
    $archive = [System.IO.Compression.ZipFile]::Open($src, 'Create')
    try {
        foreach ($f in $files) {
            $path = Join-Path $repo $f
            if (-not (Test-Path $path -PathType Leaf)) { continue }   # deleted but not yet committed
            if ($f -match '/(bin|obj)/') { continue }
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $path, "RvCarStudio-$version-source/$f", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally { $archive.Dispose() }
}

Retry {
    if (Test-Path $zip) { Remove-Item $zip -Force -Confirm:$false }
    $archive = [System.IO.Compression.ZipFile]::Open($zip, 'Create')
    try {
        foreach ($file in Get-ChildItem $stage -Recurse -File) {
            $entry = "$name/" + $file.FullName.Substring($stage.Length + 1).Replace([char]92, [char]47)
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally { $archive.Dispose() }
}
if (-not $Keep) { Retry { if (Test-Path $work) { Remove-Item $work -Recurse -Force -Confirm:$false } } }

$info = (Get-Item (Join-Path $stage 'RvCarStudio.exe')).VersionInfo
"{0} {1} (commit {2})" -f $info.ProductName, $info.ProductVersion, $head
Get-ChildItem $stage -Recurse -File | ForEach-Object { "{0,12:N0}  {1}" -f $_.Length, $_.FullName.Substring($stage.Length + 1) }
foreach ($z in @($zip, $src)) { "{0,12:N0}  {1}" -f (Get-Item $z).Length, $z }
