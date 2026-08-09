# OD.Planner

A lightweight, Windows-native task planner built with WPF and .NET. OD.Planner keeps your to-do list close at hand in a small, always-ready window and makes sure deadlines don't slip by — with audible alarms and color-coded urgency.

## Features

- **Task management** — add, edit, reorder, complete and delete tasks from a compact, resizable window.
- **Flexible deadlines** — deadlines can be a fixed date or a number of days from creation, and can be pushed back by one day or one week with a single click.
- **Urgency at a glance** — tasks are sorted and color-coded by deadline, with a blinking highlight for tasks that are about to become overdue (animations can be reduced or disabled).
- **Categories** — organize tasks into categories and filter the list with one click.
- **Audible alarms** — optional sound alerts the day before a deadline (J-1), on the due day (J0), and when a task is overdue. Each alarm can be enabled or disabled independently.
- **System tray friendly** — minimize to the background; alarms and midnight rollovers keep working while the app is in the background.
- **Dark & light themes** — switch at any time; the choice is remembered between sessions.
- **Persistent layout** — the window remembers its position and size. On first launch it docks to the top-right of the primary screen.
- **SQLite storage** — your tasks live in a portable `tasks.db` file that you can relocate from the settings.

## Requirements

- Windows 10 or later
- [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) (self-contained publishing also supported)

## Building

```powershell
git clone https://github.com/odahan/OD.Planner.git
cd OD.Planner
dotnet build OD.Planner.slnx -c Release
```

Run the app:

```powershell
dotnet run --project src/OD.Planner -c Release
```

You can also open `OD.Planner.slnx` in Visual Studio 2022 or later.

## Usage

- `Ctrl+N` — new task
- `F2` — edit selected task
- `Delete` — delete selected task
- Click **+1 day** / **+1 week** on a task to push its deadline back.
- Open **Settings** (gear icon) to configure theme, alarms, categories, autostart, and the database location.

## Data

Tasks are stored in a SQLite database (`tasks.db`). By default the file is created next to the application; use **Settings → Database → Change…** to pick another location. Settings are saved as `settings.json` next to the application (or under `%LOCALAPPDATA%\OD.Planner` if the app folder is not writable).

## License

OD.Planner is released as **donationware**. See the [LICENSE](LICENSE) file for details. If you find the app useful, a small donation is appreciated.

## Version

Current version: **1.1.0**
