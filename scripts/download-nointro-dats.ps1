param(
	[string]$OutputPath = "C:\~reference-roms\dats\nointro",
	[string]$StatePath = "$env:LOCALAPPDATA\SeedLists\state\nointro-download-state.json",
	[string]$BaseUrl = "https://datomatic.no-intro.org",
	[string]$DownloadPageUrl = "https://datomatic.no-intro.org/index.php?page=download&s=64",
	[switch]$Testing,
	[switch]$WhatIfOnly
)

$ErrorActionPreference = "Stop"

$stateDir = Split-Path -Parent $StatePath
if (-not (Test-Path $stateDir)) {
	New-Item -ItemType Directory -Path $stateDir -Force | Out-Null
}
if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$now = [DateTimeOffset]::UtcNow
$cooldown = [TimeSpan]::FromHours(24)
$lastDownload = $null

if (Test-Path $StatePath) {
	$state = Get-Content $StatePath -Raw | ConvertFrom-Json
	if ($state.lastDownloadUtc) {
		$lastDownload = [DateTimeOffset]::Parse($state.lastDownloadUtc)
	}
}

if (-not $Testing -and $lastDownload) {
	$elapsed = $now - $lastDownload
	if ($elapsed -lt $cooldown) {
		$remaining = $cooldown - $elapsed
		throw "No-Intro cooldown active. Try again in $remaining"
	}
}

$response = Invoke-WebRequest -Uri $DownloadPageUrl -UseBasicParsing
$html = $response.Content

$unique = @{}
foreach ($line in ($html -split "`n")) {
	if ($line.Contains("?page=download&op=dat&s=") -and $line.Contains("</a>")) {
		$anchorStart = $line.IndexOf("?page=download&op=dat&s=", [StringComparison]::OrdinalIgnoreCase)
		if ($anchorStart -lt 0) {
			continue
		}

		$idStart = $anchorStart + 24
		$idEnd = $line.IndexOf('"', $idStart)
		if ($idEnd -le $idStart) {
			continue
		}

		$id = $line.Substring($idStart, $idEnd - $idStart).Trim()
		$labelStart = $line.IndexOf('>', $idEnd)
		$labelEnd = $line.IndexOf("</a>", $labelStart, [StringComparison]::OrdinalIgnoreCase)
		if ($labelStart -lt 0 -or $labelEnd -le $labelStart) {
			continue
		}

		$name = $line.Substring($labelStart + 1, $labelEnd - $labelStart - 1).Trim()
		if (-not [string]::IsNullOrWhiteSpace($id) -and -not [string]::IsNullOrWhiteSpace($name) -and -not $unique.ContainsKey($id)) {
			$unique[$id] = $name
		}
	}
}

Write-Host "Found $($unique.Count) No-Intro systems" -ForegroundColor Cyan
if ($WhatIfOnly) {
	return
}

# Keep this conservative to avoid aggressive traffic.
$maxDownloads = 5
$index = 0
foreach ($entry in $unique.GetEnumerator()) {
	if ($index -ge $maxDownloads) {
		break
	}

	$id = $entry.Key
	$name = $entry.Value -replace '[\\/:*?"<>|]', '_'
	$downloadUrl = "$BaseUrl/index.php?page=download&op=dat&s=$id"
	$outFile = Join-Path $OutputPath "$name.dat"

	Invoke-WebRequest -Uri $downloadUrl -OutFile $outFile -UseBasicParsing
	$index++
	Start-Sleep -Milliseconds 500
}

@{
	lastDownloadUtc = $now.ToString("O")
} | ConvertTo-Json | Set-Content -Path $StatePath

Write-Host "Downloaded $index No-Intro DAT files. State updated: $StatePath" -ForegroundColor Green
