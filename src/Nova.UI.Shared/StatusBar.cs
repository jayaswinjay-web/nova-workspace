using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Nova.UI.Shared;

/// <summary>
/// Status bar for displaying document info, zoom, etc.
/// </summary>
public sealed class StatusBar : Control
{
    public static readonly DependencyProperty LeftContentProperty =
        DependencyProperty.Register(nameof(LeftContent), typeof(UIElement), typeof(StatusBar), new PropertyMetadata(null));

    public static readonly DependencyProperty CenterContentProperty =
        DependencyProperty.Register(nameof(CenterContent), typeof(UIElement), typeof(StatusBar), new PropertyMetadata(null));

    public static readonly DependencyProperty RightContentProperty =
        DependencyProperty.Register(nameof(RightContent), typeof(UIElement), typeof(StatusBar), new PropertyMetadata(null));

    public UIElement? LeftContent
    {
        get => (UIElement?)GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public UIElement? CenterContent
    {
        get => (UIElement?)GetValue(CenterContentProperty);
        set => SetValue(CenterContentProperty, value);
    }

    public UIElement? RightContent
    {
        get => (UIElement?)GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public StatusBar()
    {
        DefaultStyleKey = typeof(StatusBar);
    }
}

/// <summary>
/// Write-specific status bar (word count, page count, etc.)
/// </summary>
public sealed class WriteStatusBar : StatusBar
{
    public WriteStatusBar()
    {
        DefaultStyleKey = typeof(WriteStatusBar);
        BuildContent();
    }

    private void BuildContent()
    {
        // Left: Page info
        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };

        leftStack.Children.Add(new TextBlock
        {
            Text = "Page 1 of 1",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        leftStack.Children.Add(new TextBlock
        {
            Text = "Words: 0",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        leftStack.Children.Add(new TextBlock
        {
            Text = "Characters: 0",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        LeftContent = leftStack;

        // Center: Proofing status
        var centerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };

        var proofingButton = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new FontIcon { Glyph = "&#xE73E;", FontSize = 12 },
                    new TextBlock { Text = "No issues", FontSize = 12 }
                }
            },
            Style = Application.Current.Resources["TransparentButtonStyle"] as Style,
            Padding = new Thickness(8, 4)
        };
        centerStack.Children.Add(proofingButton);

        var languageButton = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = "English (US)", FontSize = 12 },
                    new FontIcon { Glyph = "&#xE70D;", FontSize = 10 }
                }
            },
            Style = Application.Current.Resources["TransparentButtonStyle"] as Style,
            Padding = new Thickness(8, 4)
        };
        centerStack.Children.Add(languageButton);

        CenterContent = centerStack;

        // Right: View/Zoom
        var rightStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

        var viewButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        viewButtons.Children.Add(CreateViewButton("&#xE8A5;", "Print Layout", true));
        viewButtons.Children.Add(CreateViewButton("&#xE714;", "Web Layout", false));
        viewButtons.Children.Add(CreateViewButton("&#xE73B;", "Focus", false));
        viewButtons.Children.Add(CreateViewButton("&#xE712;", "Read Mode", false));
        rightStack.Children.Add(viewButtons);

        var zoomControl = new ZoomControl
        {
            MinZoom = 0.1,
            MaxZoom = 5.0,
            Width = 180
        };
        rightStack.Children.Add(zoomControl);

        RightContent = rightStack;
    }

    private Button CreateViewButton(string glyph, string tooltip, bool isChecked)
    {
        return new Button
        {
            Content = new FontIcon { Glyph = glyph, FontSize = 14 },
            Style = Application.Current.Resources["ToggleButtonStyle"] as Style,
            ToolTip = tooltip,
            IsChecked = isChecked,
            Width = 36,
            Height = 32
        };
    }
}

/// <summary>
/// Grid-specific status bar (SUM, AVG, COUNT, etc.)
/// </summary>
public sealed class GridStatusBar : StatusBar
{
    public GridStatusBar()
    {
        DefaultStyleKey = typeof(GridStatusBar);
        BuildContent();
    }

    private void BuildContent()
    {
        // Left: Cell info
        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };

        leftStack.Children.Add(new TextBlock
        {
            Text = "Ready",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        leftStack.Children.Add(new TextBlock
        {
            Text = "Cell: A1",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        LeftContent = leftStack;

        // Center: Calculations
        var centerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, HorizontalAlignment = HorizontalAlignment.Center };

        centerStack.Children.Add(CreateCalcItem("SUM", "0"));
        centerStack.Children.Add(CreateCalcItem("AVG", "0"));
        centerStack.Children.Add(CreateCalcItem("COUNT", "0"));
        centerStack.Children.Add(CreateCalcItem("MIN", "0"));
        centerStack.Children.Add(CreateCalcItem("MAX", "0"));

        CenterContent = centerStack;

        // Right: Zoom + View
        var rightStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

        var zoomControl = new ZoomControl
        {
            MinZoom = 0.25,
            MaxZoom = 4.0,
            Width = 180,
            ShowFitButtons = false
        };
        rightStack.Children.Add(zoomControl);

        var viewButton = new Button
        {
            Content = new FontIcon { Glyph = "&#xE7B4;", FontSize = 14 },
            Style = Application.Current.Resources["TransparentButtonStyle"] as Style,
            ToolTip = "Page Break Preview",
            Width = 36,
            Height = 32
        };
        rightStack.Children.Add(viewButton);

        RightContent = rightStack;
    }

    private StackPanel CreateCalcItem(string label, string value)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = label, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"] },
                new TextBlock { Text = value, FontSize = 12, Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"], MinWidth = 80, TextAlignment = TextAlignment.Right }
            }
        };
    }
}

/// <summary>
/// Deck-specific status bar (slide x of y)
/// </summary>
public sealed class DeckStatusBar : StatusBar
{
    public DeckStatusBar()
    {
        DefaultStyleKey = typeof(DeckStatusBar);
        BuildContent();
    }

    private void BuildContent()
    {
        // Left: Slide info
        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };

        leftStack.Children.Add(new TextBlock
        {
            Text = "Slide 1 of 1",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        leftStack.Children.Add(new TextBlock
        {
            Text = "Notes: 0",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        LeftContent = leftStack;

        // Center: Theme/Layout info
        var centerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };

        centerStack.Children.Add(new TextBlock
        {
            Text = "Theme: Office",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        centerStack.Children.Add(new TextBlock
        {
            Text = "Layout: 16:9",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        CenterContent = centerStack;

        // Right: View/Zoom
        var rightStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

        var viewButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        viewButtons.Children.Add(CreateViewButton("&#xE7B4;", "Normal", true));
        viewButtons.Children.Add(CreateViewButton("&#xE7B5;", "Slide Sorter", false));
        viewButtons.Children.Add(CreateViewButton("&#xE7B6;", "Reading View", false));
        viewButtons.Children.Add(CreateViewButton("&#xE7B7;", "Slide Show", false));
        rightStack.Children.Add(viewButtons);

        var zoomControl = new ZoomControl
        {
            MinZoom = 0.1,
            MaxZoom = 4.0,
            Width = 180,
            ShowFitButtons = false
        };
        rightStack.Children.Add(zoomControl);

        RightContent = rightStack;
    }

    private Button CreateViewButton(string glyph, string tooltip, bool isChecked)
    {
        return new Button
        {
            Content = new FontIcon { Glyph = glyph, FontSize = 14 },
            Style = Application.Current.Resources["ToggleButtonStyle"] as Style,
            ToolTip = tooltip,
            IsChecked = isChecked,
            Width = 36,
            Height = 32
        };
    }
}

/// <summary>
/// PDF-specific status bar (page x of y)
/// </summary>
public sealed class PdfStatusBar : StatusBar
{
    public PdfStatusBar()
    {
        DefaultStyleKey = typeof(PdfStatusBar);
        BuildContent();
    }

    private void BuildContent()
    {
        // Left: Page info
        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };

        leftStack.Children.Add(new TextBlock
        {
            Text = "Page 1 of 1",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        leftStack.Children.Add(new TextBlock
        {
            Text = "100%",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        LeftContent = leftStack;

        // Center: Document info
        var centerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, HorizontalAlignment = HorizontalAlignment.Center };

        centerStack.Children.Add(new TextBlock
        {
            Text = "PDF 1.7",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        centerStack.Children.Add(new TextBlock
        {
            Text = "No Security",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        CenterContent = centerStack;

        // Right: View modes + Zoom
        var rightStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

        var viewButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        viewButtons.Children.Add(CreateViewButton("&#xE7B4;", "Single Page", true));
        viewButtons.Children.Add(CreateViewButton("&#xE7B5;", "Continuous", false));
        viewButtons.Children.Add(CreateViewButton("&#xE7B6;", "Two Page", false));
        rightStack.Children.Add(viewButtons);

        var zoomControl = new ZoomControl
        {
            MinZoom = 0.05,
            MaxZoom = 10.0,
            Width = 180
        };
        rightStack.Children.Add(zoomControl);

        RightContent = rightStack;
    }

    private Button CreateViewButton(string glyph, string tooltip, bool isChecked)
    {
        return new Button
        {
            Content = new FontIcon { Glyph = glyph, FontSize = 14 },
            Style = Application.Current.Resources["ToggleButtonStyle"] as Style,
            ToolTip = tooltip,
            IsChecked = isChecked,
            Width = 36,
            Height = 32
        };
    }
}