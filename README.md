# PC Booster Pro — WPF Application

A dark-themed Windows desktop app that monitors and boosts PC performance in real time.

---

## Features

| Feature | Details |
|---|---|
| **Live CPU / RAM bars** | Polled every 2 s via `PerformanceCounter` |
| **Disk & Temperature** | Simulated (extendable via WMI / OpenHardwareMonitor) |
| **Animated score ring** | Arc drawn on a `Canvas`; animates digit-by-digit |
| **Junk Cleaner** | Deletes `%TEMP%`, `INetCache`, Recent files |
| **RAM Optimizer** | Trims all process working sets + forces .NET GC |
| **Startup Manager** | Reads HKCU/HKLM Run keys; opens Task Manager Startup tab |
| **One-click Boost** | Runs all three actions in sequence |
| **Custom title bar** | Draggable; no default chrome |

---

## Project Structure

```
PCBooster/
├── App.xaml / App.xaml.cs          — Application entry point
├── MainWindow.xaml                 — UI layout (sidebar, tiles, actions)
├── MainWindow.xaml.cs              — Code-behind (metrics, animations, logic)
├── Converters/
│   └── Converters.cs               — PercentToColor, BoolToVisibility, ActionDone
├── Services/
│   ├── SystemMetricsService.cs     — CPU / RAM / Disk / Temp readings
│   ├── JunkCleanerService.cs       — Temp file scanner + deleter
│   ├── RamOptimizerService.cs      — Working-set trimmer + GC
│   └── StartupManagerService.cs    — Registry startup reader/disabler
└── PCBooster.csproj                — .NET 6 Windows / WPF project
```

---

## Requirements

- **Windows 10 / 11**
- **.NET 6 SDK** (or .NET 8 — change `TargetFramework` in `.csproj`)
- **Visual Studio 2022** or `dotnet CLI`

---

## Quick Start

### Option A — Visual Studio
1. Open `PCBooster.csproj` in Visual Studio 2022.
2. Press **F5** to build and run.

### Option B — dotnet CLI
```bash
cd PCBooster
dotnet restore
dotnet run
```

### Option C — Publish as single EXE
```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -o ./publish
```

---

## Extending the App

### Add real temperature readings
Install [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) NuGet:
```bash
dotnet add package LibreHardwareMonitorLib
```
Then in `SystemMetricsService.cs`:
```csharp
using LibreHardwareMonitor.Hardware;

var computer = new Computer { IsCpuEnabled = true };
computer.Open();
// read sensors...
```

### Add a second window (e.g. Startup Manager list)
1. Create `Views/StartupManagerWindow.xaml`
2. Populate a `ListView` with `StartupManagerService.GetStartupEntries()`
3. Open from `Nav_Startup` click handler:
   ```csharp
   new StartupManagerWindow().Show();
   ```

### Persist settings
```csharp
// In App.xaml.cs:
Properties["RunOnStartup"] = true;
```
Or use `System.Configuration.ConfigurationManager` / JSON file.

---

## Notes

- The **Junk Cleaner** skips locked files silently — no crash on in-use files.
- The **RAM Optimizer** calls `SetProcessWorkingSetSize` via P/Invoke; elevated processes are skipped gracefully.
- The **Startup Manager** currently opens Task Manager for safety — to auto-disable entries, use `StartupManagerService.DisableUserEntry(name)`.
- Score ring is drawn on a WPF `Canvas` using `StreamGeometry` arcs — no third-party chart library needed.
