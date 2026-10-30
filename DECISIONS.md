# NOVA Office - Design Decisions Log

## Phase 1 Decisions
- **Target Framework**: .NET 10 (LTS) with Windows App SDK 2.x (latest stable)
- **UI Framework**: WinUI 3 (not WinUI 2/UWP)
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog with file sink (rolling daily)
- **Theming**: Mica Alt on Windows 11, Acrylic fallback on Windows 10
- **Title Bar**: Custom title bar using ExtendsContentIntoTitleBar
- **Ribbon**: Custom implementation using WinUI 3 controls (not Microsoft Ribbon control)
- **Command Palette**: Fuzzy search using simple string matching (no external library)
- **Backstage**: Custom Flyout/Page implementation
- **Settings**: JSON file in %LOCALAPPDATA%\NovaOffice\Settings.json
- **NOVA Hub**: Separate executable, launches other apps via protocol activation or process start

## Phase 2 Decisions (Nova.Formula)
- **Parser**: Recursive descent parser with precedence climbing
- **AST**: Immutable expression trees
- **Evaluator**: Visitor pattern with dependency tracking
- **Dependency Graph**: Directed acyclic graph with topological sort for recalc
- **Circular Reference**: Detection via DFS during evaluation
- **Functions**: Implemented as static methods with metadata attributes
- **Error Values**: Custom ErrorValue struct wrapping error types
- **Named Ranges**: Dictionary per workbook with scope (workbook/worksheet)

## Phase 3 Decisions (NOVA Grid)
- **Rendering**: Win2D (Microsoft.Graphics.Win2D) for 60fps virtualized rendering
- **Storage**: Sparse dictionary (Dictionary<CellAddress, Cell>) with row/col indexes
- **Virtualization**: Only render visible cells + small buffer
- **Formula Bar**: Separate TextBox bound to active cell
- **Fill Handle**: Custom adorner on cell selection
- **Charts**: LiveChartsCore.SkiaSharpView.WinUI
- **Clipboard**: TSV format compatible with Excel
- **Export**: PDFsharp for PDF export

## Phase 4 Decisions (NOVA Write)
- **Editor**: WinUI RichEditBox with ITextDocument
- **Pagination**: Custom paginator using TextLayout (Windows.UI.Text)
- **Print Layout**: Custom control with rulers, margins, page shadows
- **Spell Check**: Windows.Globalization.ISpellChecker
- **Styles**: Style gallery using ITextCharacterFormat/ITextParagraphFormat
- **Export PDF**: PDFsharp with layout preservation

## Phase 5 Decisions (NOVA Deck)
- **Slide Canvas**: Win2D for high-performance rendering
- **Objects**: Immutable object model with transformation matrices
- **Animations**: Composition API for smooth 60fps animations
- **Transitions**: Custom transition engine with morph-style matching
- **Presenter View**: Separate window on second monitor using ProjectionManager
- **Export**: PDFsharp for PDF, SkiaSharp for PNG

## Phase 6 Decisions (NOVA PDF)
- **Rendering**: Windows.Data.Pdf (built-in) for page rendering
- **Text Extraction**: UglyToad.PdfPig for text layer and search
- **Editing/Annotations**: PDFsharp for writing annotations/forms
- **Page Cache**: LRU cache of rendered bitmaps (max 10 pages)
- **Background Rendering**: ThreadPool with priority queue
- **Forms**: AcroForm support via PDFsharp

## Phase 7 Decisions
- **PDF Export**: Shared PDF export service using PDFsharp
- **Print**: Windows.Graphics.Printing.PrintManager with preview

## Phase 8 Decisions
- **Undo/Redo**: Command pattern with ICommand (Execute/Undo/Redo)
- **Autosave**: Background timer, save to %LOCALAPPDATA%\NovaOffice\AutoRecover\
- **Crash Recovery**: Recovery file per document, prompt on startup
- **Tabs**: TabView control with tear-off via drag
- **MSIX**: Single MSIX bundle with multiple apps
- **Icons**: SVG source, generate PNG at 16, 24, 32, 48, 64, 128, 256, 512, 1024

## Phase 9 Decisions
- **Animations**: Implicit animations via Composition API
- **Reduced Motion**: Check UISettings.AnimationsEnabled
- **Accessibility**: AutomationProperties.Name on all interactive elements
- **High DPI**: Per-monitor DPI awareness v2
- **Testing**: Performance benchmarks in test suite