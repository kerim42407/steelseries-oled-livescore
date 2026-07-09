# OledLiveScore

Show **live football scores** on your SteelSeries OLED keyboard display (e.g. Apex Pro).
Runs quietly in the system tray. No API key, no configuration — match data comes from ESPN's public endpoints.

![platform](https://img.shields.io/badge/platform-Windows-blue)

## Features

- Live score on the OLED, refreshed automatically.
- **Goal animation** — flashes `GOAL!`, blinks the scoring team's number, then shows the scorer and minute.
- Pick a match by **league / competition** (World Cup, Champions League, Premier League, La Liga, Süper Lig, …) or by **team name**.
- Lives in the system tray — no console window. Optional "Start with Windows".

## Requirements

- Windows 10 or 11
- A SteelSeries **OLED** device (Apex Pro / Apex 7 / etc.)
- **SteelSeries GG** installed and running

## Install

1. Go to the [**Releases**](https://github.com/kerim42407/steelseries-oled-livescore/releases) page.
2. Download **`OledLiveScore-Setup.exe`** from the latest release.
3. Run it. The app is not code-signed, so Windows SmartScreen may warn you —
   click **More info → Run anyway**.
4. It installs for the current user (no admin needed) and can launch right away.

## Usage

1. Make sure **SteelSeries GG** is running.
2. Start **OledLiveScore** — a green "LS" icon appears in the system tray (near the clock).
3. Right-click the icon (or double-click it) → **Pick match…**
4. Choose a **league** from the dropdown, or type a **team name** and press Search.
5. Select a match from the list → **Track**.
6. The score appears on your keyboard's OLED and updates automatically. When a goal is
   scored, the goal animation plays.

Tray menu: **Pick match…**, **Stop**, **Start with Windows**, **Quit**.

## Uninstall

Windows **Settings → Apps → Installed apps → OledLiveScore → Uninstall**,
or run the uninstaller from the install folder.

## How it works

- Talks to the SteelSeries **GameSense** local API to push text to the OLED.
- Fetches match data from ESPN's public soccer endpoints.
- Re-pushes the frame every second so GameSense keeps the display on your screen,
  while fetching fresh data every 15 seconds.

## Build from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (any recent version).

```sh
dotnet build src/OledLiveScore.csproj -c Release
```

The exe is produced at `src/bin/Release/net48/OledLiveScore.exe`.

To build the installer, install [Inno Setup 6](https://jrsoftware.org/isinfo.php) and run:

```sh
ISCC installer/setup.iss
```

## License

[MIT](LICENSE)
