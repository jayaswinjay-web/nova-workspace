# NOVA Office

A futuristic, native Windows office suite built with WinUI 3, .NET 10, and Windows App SDK 1.6.

## Applications

| App | Description | Accent Color |
|-----|-------------|--------------|
| **NOVA Hub** | Launcher & start screen | Violet (#8C50FF) |
| **NOVA Write** | Word processor (.docx, .rtf, .txt) | Electric Blue (#0078FF) |
| **NOVA Grid** | Spreadsheet (.xlsx, .csv) | Emerald (#00B45A) |
| **NOVA Deck** | Presentations (.pptx) | Amber (#FFA000) |
| **NOVA PDF** | PDF viewer & editor (.pdf) | Crimson (#DC143C) |

## Features

- **Native Windows**: WinUI 3 on Windows App SDK, no Electron/WebView2
- **Futuristic Fluent Design**: Mica/Acrylic backdrops, custom title bars, layered surfaces
- **Collapsible Ribbon**: Compact/full modes, Ctrl+F1 toggle
- **Command Palette**: Ctrl+Shift+P / Alt+Q for fuzzy command search
- **Backstage View**: File operations, templates, recent files, settings
- **High Performance**: Win2D virtualized rendering, 60fps scrolling
- **Cross-app**: Shared recent files, unified settings, consistent UX
- **Accessibility**: Full keyboard navigation, UI Automation, High Contrast support
- **Packaging**: MSIX with file associations, self-contained builds

## Requirements

- **OS**: Windows 10 19041+ (20H1) or Windows 11
- **Architecture**: x64, ARM64
- **SDK**: .NET 10 SDK (preview)
- **Visual Studio**: 2022 17.10+ with Windows App SDK workload
- **Windows App SDK**: 1.6+ (included via NuGet)

## Building

### Prerequisites

1. Install .NET 10 SDK preview
2. Install Visual Studio 2022 17.10+ with:
   - Universal Windows Platform development
   - .NET desktop development
   - Windows App SDK C# templates

### Build from Command Line

```bash
# Restore packages
dotnet restore NovaOffice.sln

# Build Debug
dotnet build NovaOffice.sln -c Debug

# Build Release
dotnet build NovaOffice.sln -c Release

# Run tests
dotnet test NovaOffice.sln -c Release
```

### Build from Visual Studio

1. Open `NovaOffice.sln`
2. Set configuration to `Release` and platform to `x64` or `ARM64`
3. Build solution (Ctrl+Shift+B)

### Packaging (MSIX)

```bash
# Create MSIX package
dotnet publish NovaOffice.sln -c Release -p:Platform=x64 --self-contained true
```

The MSIX package will be in `artifacts/msix/Release/`.

## Running

### Debug (F5 in Visual Studio)

Each app can be run independently:
- Set `Nova.Hub` as startup project for the launcher
- Set individual apps (`Nova.Write`, `Nova.Grid`, etc.) to test them directly

### Command Line

```bash
# Run Hub
dotnet run --project src/Nova.Hub/Nova.Hub.csproj

# Run Write
dotnet run --project src/Nova.Write/Nova.Write.csproj

# Run Grid
dotnet run --project src/Nova.Grid/Nova.Grid.csproj

# Run Deck
dotnet run --project src/Nova.Deck/Nova.Deck.csproj

# Run PDF
dotnet run --project src/Nova.Pdf/Nova.Pdf.csproj
```

## Project Structure

```
NovaOffice.sln
├── src/
│   ├── Nova.Core/          # Shared core (settings, undo/redo, autosave, crash recovery)
│   ├── Nova.UI.Shared/     # Shared UI (ribbon, command palette, backstage, theming)
│   ├── Nova.Formats/       # Format import/export (OpenXML, PDFsharp, PdfPig)
│   ├── Nova.Formula/       # Spreadsheet formula engine
│   ├── Nova.Hub/           # Launcher application
│   ├── Nova.Write/         # Word processor
│   ├── Nova.Grid/          # Spreadsheet
│   ├── Nova.Deck/          # Presentations
│   └── Nova.Pdf/           # PDF viewer/editor
└── tests/
    ├── Nova.Core.Tests/
    ├── Nova.Formats.Tests/
    ├── Nova.Formula.Tests/
    └── Nova.Pdf.Tests/
```

## Architecture

### Nova.Core
- `ISettingsService` - JSON-based settings with change notifications
- `IRecentFilesService` - Cross-app recent/pinned files with thumbnails
- `IUndoRedoService` - Command-pattern unlimited undo/redo with transactions
- `IAutosaveService` - Background autosave to `%LOCALAPPDATA%\NovaOffice\AutoRecover`
- `ICrashRecoveryService` - Session tracking and crash recovery

### Nova.UI.Shared
- `ThemeService` - Mica/Acrylic backdrops, Light/Dark/High Contrast
- `CustomTitleBar` - App icon, document title, search, save status
- `Ribbon` - Collapsible, tabbed, with groups and controls
- `CommandPalette` - Fuzzy search over all commands (Ctrl+Shift+P)
- `Backstage` - File menu with Home, New, Open, Save As, Export, Print, Settings
- `StatusBar` - App-specific status bars (word count, SUM/AVG, slide info, page info)
- `ZoomControl` - Slider, presets, fit width/page

### Nova.Formula
- Tokenizer → Parser → AST → Evaluator pipeline
- Dependency graph with topological sort for incremental recalc
- 50+ functions: SUM, VLOOKUP, XLOOKUP, IF, IFS, DATE, PMT, etc.
- Error handling: #DIV/0!, #REF!, #NAME?, #VALUE!, #N/A, #CIRC!
- Named ranges, cross-sheet references, array formulas

### Nova.Formats
- OpenXML import/export for .docx, .xlsx, .pptx
- PDF export via PDFsharp
- PDF text extraction/search via PdfPig
- CSV, TXT, RTF support

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Shift+P / Alt+Q | Command Palette |
| Ctrl+F1 | Toggle Ribbon Compact Mode |
| Ctrl+N | New Document |
| Ctrl+O | Open File |
| Ctrl+S | Save |
| Ctrl+Shift+S | Save As |
| Ctrl+P | Print |
| Ctrl+Z / Ctrl+Y | Undo / Redo |
| Ctrl+F | Find |
| Ctrl+H | Replace |
| F11 | Full Screen (Deck) |
| F5 | Slide Show (Deck) |
| Ctrl+= / Ctrl+- | Zoom In / Out |
| Ctrl+0 | Reset Zoom |

## Settings

Settings are stored in `%LOCALAPPDATA%\NovaOffice\Settings.json`:

```json
{
  "Theme": "System",
  "DefaultFontFamily": "Segoe UI Variable",
  "DefaultFontSize": 11,
  "AutosaveIntervalSeconds": 60,
  "DefaultSaveFolder": "C:\\Users\\...\\Documents",
  "Language": "en-US",
  "AnimationsEnabled": true,
  "RibbonCompactMode": false
}
```

Auto-recovery files: `%LOCALAPPDATA%\NovaOffice\AutoRecover\`

Logs: `%LOCALAPPDATA%\NovaOffice\Logs\`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes with tests
4. Ensure `dotnet build` and `dotnet test` pass
5. Submit a pull request

## License

MIT License - see LICENSE file for details.

## Acknowledgments

- WinUI 3 & Windows App SDK team
- CommunityToolkit team
- PDFsharp, PdfPig, LiveCharts, Win2D contributors
- Fluent Design System