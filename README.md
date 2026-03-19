# TA Label Printer

A small **WPF** app (.NET 8) for printing **2×1** labels with part number, description, bin/location, and FIFO date.

## What it does

- Loads part and location data from **SQL Server** (connection string in code).
- Builds **ZPL**, uses the **Labelary** API for a **PNG** (preview + print).
- Sends the PNG to a **Windows printer** you select.

## Requirements

- **Windows**, **SQL Server** with the `PartsAndLocations`-shaped data the app expects.
- **Internet** (Labelary).
- At least one **installed printer**.

## Architecture and design patterns

For developers, reviewers, and employers.

### Flow

**MainWindow** binds to **MainViewModel**. The view model uses **EF Core** to query/filter parts, builds a **Labelary URL** (ZPL in the query) for preview. **printManager** downloads the same label to **label.png**, then prints with **System.Drawing** (`PrintDocument`, GDI+).

### MVVM

- **View**: `MainWindow` — UI only; `DataContext = new MainViewModel()`.
- **ViewModel**: `MainViewModel` — fields, collections, **`PrintCommand`** (`RelayCommand`).
- **Model**: **`PartsAndLocation`**.

**ViewModelBase** (`INotifyPropertyChanged`, `SetProperty`) and **RelayCommand** (`ICommand`, `CommandManager` for `CanExecute`). **No DI** — view creates the view model (typical for a small tool).

### Data

**PartsAndLocationsContext** → SQL Server; **PartsAndLocation** is **keyless** (`HasNoKey()`), suited to views/read-only shapes. Queries often use **`Sloc == "1000"`** and **Material** matching. Connection string lives in **`OnConfiguring`** (not config files yet).

### Preview vs print

Preview: **`BitmapImage`** from the Labelary URI. Print: **`HttpClient`** → **`label.png`** → **`PrintDocument`** with a custom **2×1** paper size. **ZPL is built in two places** (view model + `printManager`) — worth merging if the layout changes.

### UI and files

Styles and templates are in **MainWindow** resources (green theme, rounded controls). **printer_settings.txt** saves the last printer; **label.png** is the print artifact.

### Solution layout

| Item | Role |
|------|------|
| `DesktopLabelPrinter` | WPF app |
| `Data/` | EF Core context + entity |
| `ViewModels/` | View model, base, commands |
| `printManager.cs` | Download + print |
| `LabelPrinterSetup` | VS Installer (optional) |

### Stack

**.NET 8**, **WPF**, **EF Core** (SQL Server), **System.Drawing.Common**, **HttpClient**. **Dapper** / **EF6** in the csproj are **unused** (cleanup candidates).

## Database (important)

No database is **bundled** with the release (no `.db` in the package for offline use).

**Planned:** database hosting and configuration will be **migrated or reworked** later; until then, the app expects SQL Server as wired in code.

Without that server/database, users get **connection/load errors** unless you point the connection string at a **shared SQL Server** or **change the app** (e.g. **SQLite** + a shipped `.db`).

## Building

Open `DesktopLabelPrinter.sln`, build **DesktopLabelPrinter**. Optional: **LabelPrinterSetup** with the Visual Studio Installer extension.
