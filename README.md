# SteelSeries OLED LiveScore

Show **live football scores** on your SteelSeries OLED keyboard display (e.g. Apex Pro).
Pure PowerShell, no API key, no extra dependencies — data comes from ESPN's public endpoints.

![platform](https://img.shields.io/badge/platform-Windows-blue)
![language](https://img.shields.io/badge/PowerShell-5.1%2B-5391FE)

## Requirements

- Windows with a **SteelSeries OLED** device (Apex Pro / Apex 7 / etc.)
- **SteelSeries GG** (Engine) installed and running
- No API key needed

## Usage

1. Make sure **SteelSeries GG** is running.
2. Double-click **`Start.bat`** (or run `LiveScore.ps1` from PowerShell).
3. Type a team name (e.g. `Fenerbahce`, `Argentina`) **or** paste an ESPN match link.
4. Pick a match from the list.
5. The score appears on your keyboard's OLED and refreshes automatically.

Close the window to stop tracking.

## How it works

- `Start.bat` launches the script with the right PowerShell execution policy.
- `LiveScore.ps1`:
  - Talks to the SteelSeries **GameSense** local API to register a screen event and push text to the OLED.
  - Fetches match data from ESPN's public soccer endpoints.
  - Polls every few seconds and keeps the display alive.

## Configuration

Open `LiveScore.ps1` and edit the top of the file:

```powershell
$PollSec = 15          # how often to fetch fresh data (seconds)
$Leagues = @(...)      # leagues searched when looking up a team's matches
```

## License

[MIT](LICENSE)
