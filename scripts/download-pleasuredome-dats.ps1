param(
	[string]$OutputPath = "C:\~reference-roms\dats\pleasuredome-remote",
	[string]$MameIndexUrl = "https://pleasuredome.github.io/pleasuredome/mame/index.html",
	[string]$NonMameIndexUrl = "https://pleasuredome.github.io/pleasuredome/nonmame/index.html",
	[string[]]$CategorySlugs = @("demul", "fbneo", "fruitmachines", "hbmame", "kawaks", "pinball", "pinmame", "raine"),
	[int]$MaxDownloadsPerCategory = 1,
	[int]$RequestTimeoutSeconds = 120,
	[switch]$SkipMame,
	[switch]$SkipExisting
)

$ErrorActionPreference = "Stop"

function Get-WebHtml([string]$Url, [int]$TimeoutSeconds) {
	$response = Invoke-WebRequest -Uri $Url -TimeoutSec $TimeoutSeconds
	if ([string]::IsNullOrWhiteSpace($response.Content)) {
		throw "No HTML content returned from $Url"
	}

	return $response.Content
}

function Resolve-AbsoluteUrl([string]$BaseUrl, [string]$Href) {
	$base = [Uri]::new($BaseUrl)
	return [Uri]::new($base, $Href).ToString()
}

function Get-ZipLinks([string]$PageUrl, [string]$Html) {
	$pattern = @'
href\s*=\s*["''](?<href>[^"'']+\.zip)["'']
'@
	$regexMatches = [regex]::Matches($Html, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
	$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
	$links = [System.Collections.Generic.List[string]]::new()

	foreach ($match in $regexMatches) {
		$href = $match.Groups["href"].Value
		if ([string]::IsNullOrWhiteSpace($href)) {
			continue
		}

		$absolute = Resolve-AbsoluteUrl -BaseUrl $PageUrl -Href $href
		if ($seen.Add($absolute)) {
			$links.Add($absolute)
		}
	}

	return $links
}

function Get-NonMameCategoryPages([string]$IndexUrl, [string[]]$Slugs, [int]$TimeoutSeconds) {
	$html = Get-WebHtml -Url $IndexUrl -TimeoutSeconds $TimeoutSeconds
	$pattern = @'
href\s*=\s*["''](?<href>[^"'']*/nonmame/(?<slug>[a-z0-9\-]+)/index\.html)["'']
'@
	$regexMatches = [regex]::Matches($html, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
	$requested = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
	foreach ($slug in $Slugs) {
		if (-not [string]::IsNullOrWhiteSpace($slug)) {
			[void]$requested.Add($slug.Trim().ToLowerInvariant())
		}
	}

	$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
	$pages = [System.Collections.Generic.List[object]]::new()

	foreach ($match in $regexMatches) {
		$slug = $match.Groups["slug"].Value.Trim().ToLowerInvariant()
		if ($requested.Count -gt 0 -and -not $requested.Contains($slug)) {
			continue
		}

		$href = $match.Groups["href"].Value
		$absolute = Resolve-AbsoluteUrl -BaseUrl $IndexUrl -Href $href
		if (-not $seen.Add($absolute)) {
			continue
		}

		$pages.Add([pscustomobject]@{
			Slug = $slug
			Url = $absolute
		})
	}

	if ($pages.Count -eq 0) {
		foreach ($slug in $requested) {
			$url = Resolve-AbsoluteUrl -BaseUrl $IndexUrl -Href "$slug/index.html"
			if ($seen.Add($url)) {
				$pages.Add([pscustomobject]@{
					Slug = $slug
					Url = $url
				})
			}
		}
	}

	return $pages
}

function Save-RemoteFile([string]$Url, [string]$DestinationRoot, [int]$TimeoutSeconds, [switch]$SkipIfExists) {
	$uri = [Uri]::new($Url)
	$fileName = [Uri]::UnescapeDataString([IO.Path]::GetFileName($uri.AbsolutePath))
	if ([string]::IsNullOrWhiteSpace($fileName)) {
		throw "Unable to determine destination file name for $Url"
	}

	if (-not (Test-Path $DestinationRoot)) {
		New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
	}

	$destination = Join-Path $DestinationRoot $fileName
	if ($SkipIfExists -and (Test-Path $destination)) {
		return [pscustomobject]@{
			Url = $Url
			Path = $destination
			Downloaded = $false
		}
	}

	Invoke-WebRequest -Uri $Url -OutFile $destination -TimeoutSec $TimeoutSeconds
	return [pscustomobject]@{
		Url = $Url
		Path = $destination
		Downloaded = $true
	}
}

if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$results = [System.Collections.Generic.List[object]]::new()

if (-not $SkipMame) {
	$mameHtml = Get-WebHtml -Url $MameIndexUrl -TimeoutSeconds $RequestTimeoutSeconds
	$mameLinks = Get-ZipLinks -PageUrl $MameIndexUrl -Html $mameHtml
	if ($MaxDownloadsPerCategory -gt 0) {
		$mameLinks = $mameLinks | Select-Object -First $MaxDownloadsPerCategory
	}

	foreach ($url in $mameLinks) {
		$download = Save-RemoteFile -Url $url -DestinationRoot (Join-Path $OutputPath "mame") -TimeoutSeconds $RequestTimeoutSeconds -SkipIfExists:$SkipExisting
		$results.Add([pscustomobject]@{
			Category = "mame"
			Url = $download.Url
			Path = $download.Path
			Downloaded = $download.Downloaded
		})
	}
}

$categoryPages = Get-NonMameCategoryPages -IndexUrl $NonMameIndexUrl -Slugs $CategorySlugs -TimeoutSeconds $RequestTimeoutSeconds
foreach ($page in $categoryPages) {
	$pageHtml = Get-WebHtml -Url $page.Url -TimeoutSeconds $RequestTimeoutSeconds
	$links = Get-ZipLinks -PageUrl $page.Url -Html $pageHtml
	if ($MaxDownloadsPerCategory -gt 0) {
		$links = $links | Select-Object -First $MaxDownloadsPerCategory
	}

	foreach ($url in $links) {
		$download = Save-RemoteFile -Url $url -DestinationRoot (Join-Path (Join-Path $OutputPath "nonmame") $page.Slug) -TimeoutSeconds $RequestTimeoutSeconds -SkipIfExists:$SkipExisting
		$results.Add([pscustomobject]@{
			Category = "nonmame/$($page.Slug)"
			Url = $download.Url
			Path = $download.Path
			Downloaded = $download.Downloaded
		})
	}
}

$downloadedCount = @($results | Where-Object { $_.Downloaded }).Count
$existingCount = @($results | Where-Object { -not $_.Downloaded }).Count

Write-Host "Pleasuredome DAT download summary:" -ForegroundColor Green
Write-Host "  OutputPath: $OutputPath"
Write-Host "  Requested entries: $($results.Count)"
Write-Host "  Downloaded: $downloadedCount"
Write-Host "  Skipped existing: $existingCount"

$results | Sort-Object Category, Url | Format-Table -AutoSize
