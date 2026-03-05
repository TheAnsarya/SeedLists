param(
	[string]$OutputPath = "D:\Roms\TOSEC",
	[switch]$Force,
	[string]$BaseUrl = "https://www.tosecdev.org",
	[string]$DatfilesUrl = "https://www.tosecdev.org/downloads/category/22-datfiles"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

Write-Host "TOSEC DAT downloader" -ForegroundColor Cyan
Write-Host "Output: $OutputPath" -ForegroundColor DarkGray

$response = Invoke-WebRequest -Uri $DatfilesUrl -UseBasicParsing
$html = $response.Content

$matches = [regex]::Matches($html, 'href="([^"]*(TOSEC|tosec)[^"]*\.(zip|7z))"')
if ($matches.Count -eq 0) {
	throw "No TOSEC archive links found."
}

$downloaded = 0
$skipped = 0
foreach ($match in $matches) {
	$link = $match.Groups[1].Value
	if (-not $link.StartsWith("http")) {
		$link = "$BaseUrl$link"
	}

	$fileName = [System.IO.Path]::GetFileName($link)
	$outFile = Join-Path $OutputPath $fileName

	if ((Test-Path $outFile) -and -not $Force) {
		$skipped++
		Write-Host "Skipped existing: $fileName" -ForegroundColor DarkGray
		continue
	}

	Invoke-WebRequest -Uri $link -OutFile $outFile -UseBasicParsing
	$downloaded++
	Write-Host "Downloaded: $fileName" -ForegroundColor Green
}

Write-Host "Done. Downloaded=$downloaded Skipped=$skipped" -ForegroundColor Cyan
