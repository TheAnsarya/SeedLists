param(
	[string]$SourceRoot = "C:\~reference-roms\roms",
	[string]$OutputPath = "C:\~reference-roms\dats\goodtools",
	[switch]$IncludeArchives
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

if (-not (Test-Path $SourceRoot)) {
	throw "Source root does not exist: $SourceRoot"
}

$patterns = @("*.dat")
if ($IncludeArchives) {
	$patterns += @("*.zip", "*.7z")
}

$copied = 0
foreach ($pattern in $patterns) {
	$files = Get-ChildItem -Path $SourceRoot -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue
	foreach ($file in $files) {
		$target = Join-Path $OutputPath $file.Name
		Copy-Item -Path $file.FullName -Destination $target -Force
		$copied++
	}
}

Write-Host "Collected $copied GoodTools candidate files to $OutputPath" -ForegroundColor Green
