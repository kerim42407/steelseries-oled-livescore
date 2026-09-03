# Changelog

## v1.1.0

### Fixed

- **Scores and match lists work again.** ESPN stopped serving `site.api.espn.com`
  (403 on every path), which left v1.0.0 unable to load matches or update scores.
  All calls now go to `site.web.api.espn.com`, which serves the same data.

### Added

- **Automatic update checks.** The app checks GitHub Releases on startup and every
  6 hours. A tray notification appears when a new version is out; confirm it and the
  app downloads the installer, updates itself and returns to the tray. Also available
  as **Check for updates…** in the tray menu.
- Starting the app from the Start menu, a shortcut or the exe opens the match picker
  right away instead of dropping silently into the tray.
- Launching the app while it is already running brings up the picker of the running
  copy instead of adding a second tray icon.

### Changed

- A Windows login start still goes quietly to the tray — the startup entry now passes
  `--silent`.
- The match picker window uses the app icon.

## v1.0.0

First public release.

- Live score on the OLED, refreshed automatically.
- Goal animation: flashes `GOAL!`, blinks the scoring team's number, shows the scorer.
- Pick a match by league / competition or by team name.
- System tray only, no console window. Optional "Start with Windows".
