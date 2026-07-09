# Live football score on the SteelSeries OLED keyboard display.
# Requires SteelSeries GG (Engine) running. No API key needed.

$PollSec = 15
$Leagues = @("fifa.world","fifa.cwc","club.friendly","uefa.champions","uefa.europa",
             "tur.1","eng.1","esp.1","ita.1","ger.1","fra.1","rus.1","fifa.friendly")

$GameId    = "LIVESCORE"
$GameName  = "Live Score"
$EventName = "SCORE"
$Screen    = "screened-128x40"
$Agent     = "Mozilla/5.0"

function Get-EngineBase {
    $core = Join-Path $env:PROGRAMDATA "SteelSeries\SteelSeries Engine 3\coreProps.json"
    if (-not (Test-Path $core)) { throw "SteelSeries GG is not running." }
    return "http://" + (Get-Content $core -Raw | ConvertFrom-Json).address
}

function Send-GameSense($endpoint, $obj) {
    $json = $obj | ConvertTo-Json -Depth 12 -Compress
    Invoke-RestMethod -Uri ($script:Base + $endpoint) -Method Post -Body $json -ContentType "application/json" | Out-Null
}

function Register-Game {
    Send-GameSense "/game_metadata" @{ game = $GameId; game_display_name = $GameName; developer = "LiveScore" }
    Send-GameSense "/bind_game_event" @{
        game = $GameId; event = $EventName; value_optional = $true
        handlers = @(@{
            "device-type" = $Screen; mode = "screen"
            datas = @(@{ lines = @(
                @{ "has-text" = $true; "context-frame-key" = "top"; bold = $true },
                @{ "has-text" = $true; "context-frame-key" = "bottom" }
            )})
        })
    }
}

function Update-Screen($top, $bottom) {
    $trim = { param($s) if ($s.Length -gt 21) { $s.Substring(0,21) } else { $s } }
    Send-GameSense "/game_event" @{
        game = $GameId; event = $EventName
        data = @{ frame = @{ top = (& $trim $top); bottom = (& $trim $bottom) } }
    }
}

function Get-Json($url) {
    return Invoke-RestMethod -Uri $url -Headers @{ "User-Agent" = $Agent } -Method Get -TimeoutSec 12
}

function Find-Team($query) {
    $q = [uri]::EscapeDataString($query)
    $res = Get-Json "https://site.web.api.espn.com/apis/common/v3/search?query=$q&limit=10&sport=soccer"
    $team = $res.items | Where-Object { $_.type -eq 'team' } | Select-Object -First 1
    if (-not $team) { return $null }
    return [pscustomobject]@{ Id = [string]$team.id; Name = $team.displayName; League = $team.defaultLeagueSlug }
}

function Get-CompetitorScore($c) {
    if ($null -ne $c.score.value) { return [int]$c.score.value }
    if ($c.score) { return [int]$c.score }
    return 0
}

function Add-Match($store, $e) {
    $c = $e.competitions[0]
    $h = $c.competitors | Where-Object { $_.homeAway -eq 'home' }
    $a = $c.competitors | Where-Object { $_.homeAway -eq 'away' }
    if (-not $h -or -not $a) { return }
    $store[[string]$e.id] = [pscustomobject]@{
        Id     = [string]$e.id
        When   = [datetime]::Parse($e.date).ToLocalTime()
        Home   = $h.team.abbreviation
        Away   = $a.team.abbreviation
        HScore = Get-CompetitorScore $h
        AScore = Get-CompetitorScore $a
        Status = $c.status.type.shortDetail
        State  = $c.status.type.state
    }
}

function Get-TeamMatches($team) {
    $store = @{}
    try {
        $sch = Get-Json "https://site.api.espn.com/apis/site/v2/sports/soccer/$($team.League)/teams/$($team.Id)/schedule"
        foreach ($e in $sch.events) { Add-Match $store $e }
    } catch {}
    foreach ($lig in $Leagues) {
        try { $sb = Get-Json "https://site.api.espn.com/apis/site/v2/sports/soccer/$lig/scoreboard" } catch { continue }
        foreach ($e in $sb.events) {
            if (($e.competitions[0].competitors.team.id) -contains $team.Id) { Add-Match $store $e }
        }
    }
    $now = Get-Date
    return $store.Values |
        Where-Object { $_.State -ne 'post' -and $_.When -le $now.AddDays(45) } |
        Sort-Object When
}

function Get-LiveById($id) {
    $s = Get-Json "https://site.api.espn.com/apis/site/v2/sports/soccer/all/summary?event=$id"
    $c = $s.header.competitions[0]
    $h = $c.competitors | Where-Object { $_.homeAway -eq 'home' }
    $a = $c.competitors | Where-Object { $_.homeAway -eq 'away' }
    $hs = if ($h.score) { $h.score } else { 0 }
    $as = if ($a.score) { $a.score } else { 0 }
    $top = "{0} {1}-{2} {3}" -f $h.team.abbreviation, $hs, $as, $a.team.abbreviation
    if ($c.status.type.state -eq 'pre') {
        $bottom = [datetime]::Parse($c.date).ToLocalTime().ToString("dd MMM HH:mm", [Globalization.CultureInfo]::InvariantCulture)
    } else {
        $bottom = "$($c.status.type.shortDetail)"
    }
    return @($top, $bottom)
}

# --- startup ---
$script:Base = Get-EngineBase
Register-Game
Write-Host "Connected: $script:Base" -ForegroundColor Green
Write-Host ""
Write-Host "Enter a team name (e.g. Argentina, Fenerbahce) or an ESPN match link:" -ForegroundColor Cyan
$query = Read-Host "Team or link"

$eventId = $null
if ($query -match '(\d{6,})') { $eventId = $matches[1] }

if (-not $eventId) {
    $team = Find-Team $query
    if (-not $team) { Write-Host "Team not found." -ForegroundColor Red; exit }
    Write-Host "Team: $($team.Name)" -ForegroundColor Green
    $list = @(Get-TeamMatches $team)
    if ($list.Count -eq 0) { Write-Host "No matches found for this team." -ForegroundColor Red; exit }

    Write-Host ""
    for ($i = 0; $i -lt $list.Count; $i++) {
        $m = $list[$i]
        $when = $m.When.ToString("dd MMM HH:mm", [Globalization.CultureInfo]::InvariantCulture)
        "{0,2}) {1}  {2} {3}-{4} {5}   {6}" -f ($i + 1), $when,
            $m.Home, $m.HScore, $m.AScore, $m.Away, $m.Status | Write-Host
    }
    Write-Host ""
    $pick = Read-Host "Pick a match number"
    $idx = [int]$pick - 1
    if ($idx -lt 0 -or $idx -ge $list.Count) { $idx = 0 }
    $eventId = $list[$idx].Id
}

Write-Host "Tracking match $eventId. Close this window to stop." -ForegroundColor DarkGray
Write-Host ""

# Push to the OLED every few seconds so it stays on screen,
# but only fetch fresh data every $PollSec seconds.
$refresh = 4
$fetchEvery = [Math]::Max(1, [int]($PollSec / $refresh))
$cache = @("Loading...", "")
$tick = 0
while ($true) {
    if ($tick % $fetchEvery -eq 0) {
        try {
            $cache = Get-LiveById $eventId
            Write-Host ("{0} | {1}" -f $cache[0], $cache[1])
        } catch {
            Write-Host ("Error: " + $_.Exception.Message) -ForegroundColor Red
        }
    }
    try { Update-Screen $cache[0] $cache[1] } catch {}
    $tick++
    Start-Sleep -Seconds $refresh
}
