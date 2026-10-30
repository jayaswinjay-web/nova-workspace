using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Numerics;

namespace Nova.Grid;

/// <summary>
/// High-performance virtualized spreadsheet grid using Win2D.
/// </summary>
public sealed class VirtualizedGrid : Control
{
    private CanvasVirtualControl? _canvas;
    private readonly Dictionary<(int row, int col), CellData> _cells = new();
    private readonly Dictionary<int, double> _rowHeights = new();
    private readonly Dictionary<int, double> _colWidths = new();
    private readonly HashSet<int> _hiddenRows = new();
    private readonly HashSet<int> _hiddenCols = new();
    private readonly List<MergeRange> _mergedCells = new();
    private readonly HashSet<int> _frozenRows = new();
    private readonly HashSet<int> _frozenCols = new();
    
    private double _scrollX = 0;
    private double _scrollY = 0;
    private double _zoom = 1.0;
    private int _viewportWidth = 0;
    private int _viewportHeight = 0;
    private Point _lastMousePos;
    private bool _isDragging;
    private (int row, int col)? _selectionStart;
    private (int row, int col)? _selectionEnd;
    private (int row, int col)? _activeCell;
    private CanvasTextFormat _defaultTextFormat;
    private CanvasTextFormat _headerTextFormat;
    private ICanvasResourceCreator? _resourceCreator;

    // Constants
    private const double DefaultRowHeight = 24;
    private const double DefaultColWidth = 80;
    private const double HeaderHeight = 30;
    private const double RowHeaderWidth = 40;
    private const int MaxRows = 1_000_000;
    private const int MaxCols = 16_384;

    public event EventHandler<CellActivatedEventArgs>? CellActivated;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<CellEditEventArgs>? CellEditRequested;

    public VirtualizedGrid()
    {
        DefaultStyleKey = typeof(VirtualizedGrid);
        InitializeFormats();
    }

    private void InitializeFormats()
    {
        _defaultTextFormat = new CanvasTextFormat
        {
            FontFamily = "Segoe UI Variable",
            FontSize = 12,
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Center,
            WordWrapping = CanvasWordWrapping.NoWrap,
            TrimmingGranularity = CanvasTextTrimmingGranularity.Character
        };

        _headerTextFormat = new CanvasTextFormat
        {
            FontFamily = "Segoe UI Variable",
            FontSize = 11,
            FontWeight = Windows.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center,
            WordWrapping = CanvasWordWrapping.NoWrap
        };
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _canvas = GetTemplateChild("Canvas") as CanvasVirtualControl;
        if (_canvas != null)
        {
            _canvas.CreateResources += OnCreateResources;
            _canvas.Draw += OnDraw;
            _canvas.PointerPressed += OnPointerPressed;
            _canvas.PointerMoved += OnPointerMoved;
            _canvas.PointerReleased += OnPointerReleased;
            _canvas.PointerWheelChanged += OnPointerWheelChanged;
            _canvas.SizeChanged += OnSizeChanged;
        }
    }

    private void OnCreateResources(CanvasVirtualControl sender, CanvasCreateResourcesEventArgs args)
    {
        _resourceCreator = sender;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewportWidth = (int)e.NewSize.Width;
        _viewportHeight = (int)e.NewSize.Height;
        Invalidate();
    }

    private void OnDraw(CanvasVirtualControl sender, CanvasRegionsInvalidatedEventArgs args)
    {
        var session = sender.CreateDrawingSession();
        if (session == null) return;

        // Clear background
        session.Clear(Colors.White);

        // Calculate visible range
        var visibleRange = GetVisibleRange();

        // Draw frozen/top-left corner
        DrawCorner(session);

        // Draw column headers
        DrawColumnHeaders(session, visibleRange);

        // Draw row headers
        DrawRowHeaders(session, visibleRange);

        // Draw frozen rows
        DrawFrozenRows(session, visibleRange);

        // Draw frozen columns
        DrawFrozenColumns(session, visibleRange);

        // Draw main grid
        DrawCells(session, visibleRange);

        // Draw selection
        DrawSelection(session);

        // Draw merge lines
        DrawMergeLines(session, visibleRange);

        // Draw grid lines
        DrawGridLines(session, visibleRange);
    }

    private (int startRow, int endRow, int startCol, int endCol) GetVisibleRange()
    {
        var startRow = GetRowAtY(_scrollY);
        var endRow = GetRowAtY(_scrollY + _viewportHeight / _zoom);
        var startCol = GetColAtX(_scrollX);
        var endCol = GetColAtX(_scrollX + _viewportWidth / _zoom);

        startRow = Math.Max(0, startRow - 1);
        endRow = Math.Min(MaxRows, endRow + 2);
        startCol = Math.Max(0, startCol - 1);
        endCol = Math.Min(MaxCols, endCol + 2);

        return (startRow, endRow, startCol, endCol);
    }

    private int GetRowAtY(double y)
    {
        double pos = 0;
        if (_frozenRows.Count > 0)
        {
            pos = HeaderHeight + _frozenRows.Count * DefaultRowHeight;
        }
        else
        {
            pos = HeaderHeight;
        }

        var adjustedY = y / _zoom - pos + _scrollY;

        if (adjustedY <= 0) return 0;

        double accumulated = 0;
        for (int r = _frozenRows.Count; r < MaxRows; r++)
        {
            if (_hiddenRows.Contains(r)) continue;
            var h = _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
            if (accumulated + h >= adjustedY) return r;
            accumulated += h;
        }
        return MaxRows - 1;
    }

    private int GetColAtX(double x)
    {
        double pos = 0;
        if (_frozenCols.Count > 0)
        {
            pos = RowHeaderWidth + _frozenCols.Count * DefaultColWidth;
        }
        else
        {
            pos = RowHeaderWidth;
        }

        var adjustedX = x / _zoom - pos + _scrollX;

        if (adjustedX <= 0) return 0;

        double accumulated = 0;
        for (int c = _frozenCols.Count; c < MaxCols; c++)
        {
            if (_hiddenCols.Contains(c)) continue;
            var w = _colWidths.GetValueOrDefault(c, DefaultColWidth);
            if (accumulated + w >= adjustedX) return c;
            accumulated += w;
        }
        return MaxCols - 1;
    }

    private void DrawCorner(CanvasDrawingSession session)
    {
        var rect = new Rect(0, 0, RowHeaderWidth, HeaderHeight);
        session.FillRectangle(rect, new CanvasSolidColorBrush(session, Color.FromArgb(255, 240, 240, 240)));
        session.DrawRectangle(rect, new CanvasSolidColorBrush(session, Colors.LightGray));
    }

    private void DrawColumnHeaders(CanvasDrawingSession session, (int startRow, int endRow, int startCol, int endCol) range)
    {
        double x = RowHeaderWidth;
        double y = 0;

        // Frozen columns
        if (_frozenCols.Count > 0)
        {
            for (int c = 0; c < _frozenCols.Count; c++)
            {
                if (_hiddenCols.Contains(c)) continue;
                var w = _colWidths.GetValueOrDefault(c, DefaultColWidth);
                var headerRect = new Rect(x, y, w, HeaderHeight);
                DrawHeaderCell(session, headerRect, GetColumnLetter(c));
                x += w;
            }
        }

        // Scrollable columns
        x = RowHeaderWidth + (_frozenCols.Count > 0 ? _frozenCols.Count * DefaultColWidth : 0) - _scrollX;
        for (int c = range.startCol; c <= range.endCol; c++)
        {
            if (_hiddenCols.Contains(c) || _frozenCols.Contains(c)) continue;
            var w = _colWidths.GetValueOrDefault(c, DefaultColWidth);
            if (x + w < RowHeaderWidth) { x += w; continue; }
            if (x > _viewportWidth) break;

            var headerRect = new Rect(x, y, w, HeaderHeight);
            DrawHeaderCell(session, headerRect, GetColumnLetter(c));
            x += w;
        }
    }

    private void DrawRowHeaders(CanvasDrawingSession session, (int startRow, int endRow, int startCol, int endCol) range)
    {
        double x = 0;
        double y = HeaderHeight;

        // Frozen rows
        if (_frozenRows.Count > 0)
        {
            for (int r = 0; r < _frozenRows.Count; r++)
            {
                if (_hiddenRows.Contains(r)) continue;
                var h = _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
                var headerRect = new Rect(x, y, RowHeaderWidth, h);
                DrawHeaderCell(session, headerRect, (r + 1).ToString());
                y += h;
            }
        }

        // Scrollable rows
        y = HeaderHeight + (_frozenRows.Count > 0 ? _frozenRows.Count * DefaultRowHeight : 0) - _scrollY;
        for (int r = range.startRow; r <= range.endRow; r++)
        {
            if (_hiddenRows.Contains(r) || _frozenRows.Contains(r)) continue;
            var h = _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
            if (y + h < HeaderHeight) { y += h; continue; }
            if (y > _viewportHeight) break;

            var headerRect = new Rect(x, y, RowHeaderWidth, h);
            DrawHeaderCell(session, headerRect, (r + 1).ToString());
            y += h;
        }
    }

    private void DrawFrozenRows(CanvasDrawingSession session, (int startRow, int endRow, int startCol, int endCol) range)
    {
        if (_frozenRows.Count == 0) return;

        double y = HeaderHeight;
        for (int r = 0; r < _frozenRows.Count; r++)
        {
            if (_hiddenRows.Contains(r)) continue;
            var h = _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
            DrawRowCells(session, r, y, h, range.startCol, range.endCol);
            y += h;
        }
    }

    private void DrawFrozenColumns(CanvasDrawingSession session, (int startRow, int endRow, int startCol, int endCol) range)
    {
        if (_frozenCols.Count == 0) return;

        double x = RowHeaderWidth;
        for (int c = 0; c < _frozenCols.Count; c++)
        {
            if (_hiddenCols.Contains(c)) continue;
            var w = _colWidths.GetValueOrDefault(c, DefaultColWidth);
            DrawColumnCells(session, c, x, w, range.startRow, range.endRow);
            x += w;
        }
    }

    private void DrawCells(CanvasDrawingSession session, (int startRow, int endRow, int startCol, int endCol) range)
    {
        double y = HeaderHeight + (_frozenRows.Count > 0 ? _frozenRows.Count * DefaultRowHeight : 0) - _scrollY;

        for (int r = range.startRow; r <= range.endRow; r++)
        {
            if (_hiddenRows.Contains(r) || _frozenRows.Contains(r)) continue;
            var h = _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
            if (y + h < HeaderHeight) { y += h; continue; }
            if (y > _viewportHeight) break;

            DrawRowCells(session, r, y, h, range.startCol, range.endCol);
            y += h;
        }
    }

    private void DrawRowCells(CanvasDrawingSession session, int row, double y, double rowHeight, int startCol, int endCol)
    {
        double x = RowHeaderWidth + (_frozenCols.Count > 0 ? _frozenCols.Count * DefaultColWidth : 0) - _scrollX;

        for (int c = startCol; c <= endCol; c++)
        {
            if (_hiddenCols.Contains(c) || _frozenCols.Contains(c)) continue;
            var w = _colWidths.GetValueOrDefault(c, DefaultColWidth);
            if (x + w < RowHeaderWidth) { x += w; continue; }
            if (x > _viewportWidth) break;

            var cellRect = new Rect(x, y, w, rowHeight);
            DrawCell(session, row, c, cellRect);
            x += w;
        }
    }

    private void DrawColumnCells(CanvasDrawingSession session, int col, double x, double colWidth, int startRow, int endRow)
    {
        double y = HeaderHeight + (_frozenRows.Count > 0 ? _frozenRows.Count * DefaultRowHeight : 0) - _scrollY;

        for (int r = startRow; r <= endRow; r++)
        {
            if (_hiddenRows.Contains(r) || _frozenRows.Contains(r)) continue;
            var h = _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
            if (y + h < HeaderHeight) { y += h; continue; }
            if (y > _viewportHeight) break;

            var cellRect = new Rect(x, y, colWidth, h);
            DrawCell(session, r, col, cellRect);
            y += h;
        }
    }

    private void DrawCell(CanvasDrawingSession session, int row, int col, Rect rect)
    {
        var key = (row, col);
        var brush = new CanvasSolidColorBrush(session, Colors.White);

        // Check if cell is part of a merge
        var merge = _mergedCells.FirstOrDefault(m => m.Contains(row, col));
        if (merge != null && (merge.StartRow != row || merge.StartCol != col))
        {
            // This cell is merged into another - don't draw content
            session.FillRectangle(rect, brush);
            session.DrawRectangle(rect, new CanvasSolidColorBrush(session, Colors.LightGray));
            return;
        }

        // Background
        if (_activeCell.HasValue && _activeCell.Value.row == row && _activeCell.Value.col == col)
        {
            brush.Color = Color.FromArgb(255, 230, 240, 255);
        }
        else if (IsCellSelected(row, col))
        {
            brush.Color = Color.FromArgb(255, 200, 220, 255);
        }
        session.FillRectangle(rect, brush);

        // Cell content
        if (_cells.TryGetValue(key, out var cellData) && !string.IsNullOrEmpty(cellData.DisplayText))
        {
            var textLayout = new CanvasTextLayout(session, cellData.DisplayText, _defaultTextFormat, (float)rect.Width - 8, (float)rect.Height);
            session.DrawTextLayout(textLayout, (float)rect.X + 4, (float)rect.Y + (float)(rect.Height - textLayout.LayoutBounds.Height) / 2, new CanvasSolidColorBrush(session, Colors.Black));
        }

        // Border
        session.DrawRectangle(rect, new CanvasSolidColorBrush(session, Colors.LightGray));
    }

    private void DrawHeaderCell(CanvasDrawingSession session, Rect rect, string text)
    {
        session.FillRectangle(rect, new CanvasSolidColorBrush(session, Color.FromArgb(255, 240, 240, 240)));
        var textLayout = new CanvasTextLayout(session, text, _headerTextFormat, (float)rect.Width, (float)rect.Height);
        session.DrawTextLayout(textLayout, (float)rect.X, (float)rect.Y + (float)(rect.Height - textLayout.LayoutBounds.Height) / 2, new CanvasSolidColorBrush(session, Colors.Black));
        session.DrawRectangle(rect, new CanvasSolidColorBrush(session, Colors.LightGray));
    }

    private void DrawSelection(CanvasDrawingSession session)
    {
        if (!_selectionStart.HasValue || !_selectionEnd.HasValue) return;

        var start = _selectionStart.Value;
        var end = _selectionEnd.Value;
        var minRow = Math.Min(start.row, end.row);
        var maxRow = Math.Max(start.row, end.row);
        var minCol = Math.Min(start.col, end.col);
        var maxCol = Math.Max(start.col, end.col);

        var startRect = GetCellRect(minRow, minCol);
        var endRect = GetCellRect(maxRow, maxCol);

        if (startRect.HasValue && endRect.HasValue)
        {
            var selRect = new Rect(
                startRect.Value.X,
                startRect.Value.Y,
                endRect.Value.X + endRect.Value.Width - startRect.Value.X,
                endRect.Value.Y + endRect.Value.Height - startRect.Value.Y);

            session.DrawRectangle(selRect, new CanvasSolidColorBrush(session, Color.FromArgb(255, 0, 120, 215)), 2);
        }
    }

    private void DrawMergeLines(CanvasDrawingSession session, (int startRow, int endRow, int startCol, int endCol) range)
    {
        foreach (var merge in _mergedCells)
        {
            if (merge.StartRow < range.startRow || merge.StartRow > range.endRow ||
                merge.StartCol < range.startCol || merge.StartCol > range.endCol)
                continue;

            var startRect = GetCellRect(merge.StartRow, merge.StartCol);
            var endRect = GetCellRect(merge.EndRow, merge.EndCol);

            if (startRect.HasValue && endRect.HasValue)
            {
                var mergeRect = new Rect(
                    startRect.Value.X,
                    startRect.Value.Y,
                    endRect.Value.X + endRect.Value.Width - startRect.Value.X,
                    endRect.Value.Y + endRect.Value.Height - startRect.Value.Y);

                session.DrawRectangle(mergeRect, new CanvasSolidColorBrush(session, Color.FromArgb(255, 0, 120, 215)), 3);
            }
        }
    }

    private void DrawGridLines(CanvasDrawingSession session, (int startRow, int endRow, int startCol, int endCol) range)
    {
        var gridBrush = new CanvasSolidColorBrush(session, Colors.LightGray);

        // Vertical lines
        double x = RowHeaderWidth;
        for (int c = 0; c <= MaxCols; c++)
        {
            if (c > 0)
            {
                if (_hiddenCols.Contains(c - 1)) continue;
                x += _colWidths.GetValueOrDefault(c - 1, DefaultColWidth);
            }
            if (x < RowHeaderWidth || x > _viewportWidth) continue;
            session.DrawLine((float)x, 0, (float)x, (float)_viewportHeight, gridBrush);
        }

        // Horizontal lines
        double y = HeaderHeight;
        for (int r = 0; r <= MaxRows; r++)
        {
            if (r > 0)
            {
                if (_hiddenRows.Contains(r - 1)) continue;
                y += _rowHeights.GetValueOrDefault(r - 1, DefaultRowHeight);
            }
            if (y < HeaderHeight || y > _viewportHeight) continue;
            session.DrawLine(0, (float)y, (float)_viewportWidth, (float)y, gridBrush);
        }
    }

    private Rect? GetCellRect(int row, int col)
    {
        if (_hiddenRows.Contains(row) || _hiddenCols.Contains(col)) return null;

        double y = HeaderHeight;
        if (row < _frozenRows.Count)
        {
            for (int r = 0; r < row; r++)
                y += _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
        }
        else
        {
            y += _frozenRows.Count * DefaultRowHeight;
            for (int r = _frozenRows.Count; r < row; r++)
            {
                if (_hiddenRows.Contains(r)) continue;
                y += _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
            }
            y -= _scrollY;
        }

        double x = RowHeaderWidth;
        if (col < _frozenCols.Count)
        {
            for (int c = 0; c < col; c++)
                x += _colWidths.GetValueOrDefault(c, DefaultColWidth);
        }
        else
        {
            x += _frozenCols.Count * DefaultColWidth;
            for (int c = _frozenCols.Count; c < col; c++)
            {
                if (_hiddenCols.Contains(c)) continue;
                x += _colWidths.GetValueOrDefault(c, DefaultColWidth);
            }
            x -= _scrollX;
        }

        var h = _rowHeights.GetValueOrDefault(row, DefaultRowHeight);
        var w = _colWidths.GetValueOrDefault(col, DefaultColWidth);

        return new Rect(x, y, w, h);
    }

    private string GetColumnLetter(int col)
    {
        var result = "";
        while (col >= 0)
        {
            result = (char)('A' + col % 26) + result;
            col = col / 26 - 1;
        }
        return result;
    }

    private bool IsCellSelected(int row, int col)
    {
        if (!_selectionStart.HasValue || !_selectionEnd.HasValue) return false;
        var start = _selectionStart.Value;
        var end = _selectionEnd.Value;
        var minRow = Math.Min(start.row, end.row);
        var maxRow = Math.Max(start.row, end.row);
        var minCol = Math.Min(start.col, end.col);
        var maxCol = Math.Max(start.col, end.col);
        return row >= minRow && row <= maxRow && col >= minCol && col <= maxCol;
    }

    // Pointer event handlers
    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(_canvas);
        _lastMousePos = pt.Position;
        _isDragging = true;

        var cell = HitTest(pt.Position);
        if (cell.HasValue)
        {
            _selectionStart = cell;
            _selectionEnd = cell;
            _activeCell = cell;
            CellActivated?.Invoke(this, new CellActivatedEventArgs(cell.Value.row, cell.Value.col));
            Invalidate();
        }
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(_canvas);
        var delta = pt.Position - _lastMousePos;

        if (_isDragging && pt.Properties.IsLeftButtonPressed)
        {
            var cell = HitTest(pt.Position);
            if (cell.HasValue)
            {
                _selectionEnd = cell;
                Invalidate();
            }
        }
        else if (pt.Properties.IsMiddleButtonPressed || (pt.Properties.IsLeftButtonPressed && pt.Properties.IsShiftKeyDown))
        {
            // Pan
            _scrollX -= delta.X / _zoom;
            _scrollY -= delta.Y / _zoom;
            ClampScroll();
            Invalidate();
        }

        _lastMousePos = pt.Position;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selectionStart, _selectionEnd));
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(_canvas);
        var delta = pt.Properties.MouseWheelDelta;

        if (e.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control))
        {
            // Zoom
            var oldZoom = _zoom;
            _zoom = Math.Clamp(_zoom * (1 + delta * 0.001), 0.5, 3.0);
            var zoomFactor = _zoom / oldZoom;

            // Zoom toward mouse position
            _scrollX = (_scrollX + pt.Position.X / oldZoom) * zoomFactor - pt.Position.X / _zoom;
            _scrollY = (_scrollY + pt.Position.Y / oldZoom) * zoomFactor - pt.Position.Y / _zoom;
            ClampScroll();
        }
        else
        {
            // Scroll
            _scrollY -= delta * 0.5;
            ClampScroll();
        }
        Invalidate();
    }

    private (int row, int col)? HitTest(Point pt)
    {
        var adjustedX = pt.X / _zoom;
        var adjustedY = pt.Y / _zoom;

        // Check column headers
        if (adjustedY < HeaderHeight)
        {
            var col = GetColAtX(adjustedX + _scrollX);
            if (col >= 0) return (0, col); // Header row
        }

        // Check row headers
        if (adjustedX < RowHeaderWidth)
        {
            var row = GetRowAtY(adjustedY + _scrollY);
            if (row >= 0) return (row, 0); // Header column
        }

        // Check cells
        var row = GetRowAtY(adjustedY + _scrollY);
        var col = GetColAtX(adjustedX + _scrollX);
        if (row >= 0 && col >= 0) return (row, col);

        return null;
    }

    private void ClampScroll()
    {
        // Calculate max scroll
        double maxScrollX = 0;
        for (int c = 0; c < MaxCols; c++)
        {
            if (!_hiddenCols.Contains(c))
                maxScrollX += _colWidths.GetValueOrDefault(c, DefaultColWidth);
        }
        maxScrollX -= _viewportWidth / _zoom;
        maxScrollX = Math.Max(0, maxScrollX);

        double maxScrollY = 0;
        for (int r = 0; r < MaxRows; r++)
        {
            if (!_hiddenRows.Contains(r))
                maxScrollY += _rowHeights.GetValueOrDefault(r, DefaultRowHeight);
        }
        maxScrollY += HeaderHeight;
        maxScrollY -= _viewportHeight / _zoom;
        maxScrollY = Math.Max(0, maxScrollY);

        _scrollX = Math.Clamp(_scrollX, 0, maxScrollX);
        _scrollY = Math.Clamp(_scrollY, 0, maxScrollY);
    }

    public void Invalidate() => _canvas?.Invalidate();

    public void SetCellValue(int row, int col, string value)
    {
        _cells[(row, col)] = new CellData { Value = value, DisplayText = value };
        Invalidate();
    }

    public string? GetCellValue(int row, int col)
    {
        return _cells.TryGetValue((row, col), out var cell) ? cell.Value : null;
    }

    public void SetRowHeight(int row, double height)
    {
        _rowHeights[row] = height;
        Invalidate();
    }

    public void SetColWidth(int col, double width)
    {
        _colWidths[col] = width;
        Invalidate();
    }

    public void HideRow(int row) { _hiddenRows.Add(row); Invalidate(); }
    public void ShowRow(int row) { _hiddenRows.Remove(row); Invalidate(); }
    public void HideCol(int col) { _hiddenCols.Add(col); Invalidate(); }
    public void ShowCol(int col) { _hiddenCols.Remove(col); Invalidate(); }

    public void FreezeRows(int count)
    {
        _frozenRows.Clear();
        for (int i = 0; i < count; i++) _frozenRows.Add(i);
        Invalidate();
    }

    public void FreezeCols(int count)
    {
        _frozenCols.Clear();
        for (int i = 0; i < count; i++) _frozenCols.Add(i);
        Invalidate();
    }

    public void MergeCells(int startRow, int startCol, int endRow, int endCol)
    {
        _mergedCells.Add(new MergeRange(startRow, startCol, endRow, endCol));
        Invalidate();
    }

    public void UnmergeCells(int startRow, int startCol)
    {
        _mergedCells.RemoveAll(m => m.StartRow == startRow && m.StartCol == startCol);
        Invalidate();
    }
}

/// <summary>
/// Cell data storage.
/// </summary>
public sealed class CellData
{
    public string Value { get; set; } = "";
    public string DisplayText { get; set; } = "";
    public CellFormat? Format { get; set; }
}

/// <summary>
/// Cell formatting.
/// </summary>
public sealed class CellFormat
{
    public string NumberFormat { get; set; } = "General";
    public CanvasSolidColorBrush? Background { get; set; }
    public CanvasSolidColorBrush? Foreground { get; set; }
    public CanvasTextFormat? Font { get; set; }
    public CanvasHorizontalAlignment HorizontalAlignment { get; set; } = CanvasHorizontalAlignment.Left;
    public CanvasVerticalAlignment VerticalAlignment { get; set; } = CanvasVerticalAlignment.Center;
    public bool WrapText { get; set; }
    public BorderStyle Borders { get; set; } = new();
}

/// <summary>
/// Border style.
/// </summary>
public sealed class BorderStyle
{
    public bool Top { get; set; }
    public bool Bottom { get; set; }
    public bool Left { get; set; }
    public bool Right { get; set; }
    public Color Color { get; set; } = Colors.Black;
    public float Width { get; set; } = 1;
}

/// <summary>
/// Merged cell range.
/// </summary>
public sealed class MergeRange
{
    public int StartRow { get; }
    public int StartCol { get; }
    public int EndRow { get; }
    public int EndCol { get; }

    public MergeRange(int startRow, int startCol, int endRow, int endCol)
    {
        StartRow = startRow;
        StartCol = startCol;
        EndRow = endRow;
        EndCol = endCol;
    }

    public bool Contains(int row, int col)
    {
        return row >= StartRow && row <= EndRow && col >= StartCol && col <= EndCol;
    }
}

/// <summary>
/// Event arguments for cell activation.
/// </summary>
public sealed class CellActivatedEventArgs : EventArgs
{
    public int Row { get; }
    public int Col { get; }

    public CellActivatedEventArgs(int row, int col)
    {
        Row = row;
        Col = col;
    }
}

/// <summary>
/// Event arguments for selection change.
/// </summary>
public sealed class SelectionChangedEventArgs : EventArgs
{
    public (int row, int col)? Start { get; }
    public (int row, int col)? End { get; }

    public SelectionChangedEventArgs((int row, int col)? start, (int row, int col)? end)
    {
        Start = start;
        End = end;
    }
}

/// <summary>
/// Event arguments for cell edit request.
/// </summary>
public sealed class CellEditEventArgs : EventArgs
{
    public int Row { get; }
    public int Col { get; }
    public string? InitialText { get; }

    public CellEditEventArgs(int row, int col, string? initialText)
    {
        Row = row;
        Col = col;
        InitialText = initialText;
    }
}