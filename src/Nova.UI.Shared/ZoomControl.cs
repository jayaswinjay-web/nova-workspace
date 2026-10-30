using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Nova.UI.Shared;

/// <summary>
/// Zoom control with slider, preset buttons, and text input.
/// </summary>
public sealed class ZoomControl : Control
{
    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(ZoomControl), new PropertyMetadata(1.0, OnZoomFactorChanged));

    public static readonly DependencyProperty MinZoomProperty =
        DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(ZoomControl), new PropertyMetadata(0.1));

    public static readonly DependencyProperty MaxZoomProperty =
        DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(ZoomControl), new PropertyMetadata(5.0));

    public static readonly DependencyProperty PresetsProperty =
        DependencyProperty.Register(nameof(Presets), typeof(double[]), typeof(ZoomControl), new PropertyMetadata(new double[] { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0, 4.0 }));

    public static readonly DependencyProperty ShowPresetsProperty =
        DependencyProperty.Register(nameof(ShowPresets), typeof(bool), typeof(ZoomControl), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowFitButtonsProperty =
        DependencyProperty.Register(nameof(ShowFitButtons), typeof(bool), typeof(ZoomControl), new PropertyMetadata(true));

    public double ZoomFactor
    {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public double[] Presets
    {
        get => (double[])GetValue(PresetsProperty);
        set => SetValue(PresetsProperty, value);
    }

    public bool ShowPresets
    {
        get => (bool)GetValue(ShowPresetsProperty);
        set => SetValue(ShowPresetsProperty, value);
    }

    public bool ShowFitButtons
    {
        get => (bool)GetValue(ShowFitButtonsProperty);
        set => SetValue(ShowFitButtonsProperty, value);
    }

    public event EventHandler<ZoomChangedEventArgs>? ZoomChanged;

    private Slider? _slider;
    private TextBox? _textBox;
    private Button? _fitWidthButton;
    private Button? _fitPageButton;

    public ZoomControl()
    {
        DefaultStyleKey = typeof(ZoomControl);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _slider = GetTemplateChild("ZoomSlider") as Slider;
        _textBox = GetTemplateChild("ZoomTextBox") as TextBox;
        _fitWidthButton = GetTemplateChild("FitWidthButton") as Button;
        _fitPageButton = GetTemplateChild("FitPageButton") as Button;

        if (_slider != null)
        {
            _slider.ValueChanged += OnSliderValueChanged;
            _slider.KeyDown += OnSliderKeyDown;
        }

        if (_textBox != null)
        {
            _textBox.TextChanged += OnTextBoxTextChanged;
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.LostFocus += OnTextBoxLostFocus;
        }

        if (_fitWidthButton != null)
        {
            _fitWidthButton.Click += OnFitWidthClick;
        }

        if (_fitPageButton != null)
        {
            _fitPageButton.Click += OnFitPageClick;
        }

        UpdateControls();
    }

    private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ZoomControl control)
        {
            control.UpdateControls();
            control.ZoomChanged?.Invoke(control, new ZoomChangedEventArgs((double)e.OldValue, (double)e.NewValue));
        }
    }

    private void UpdateControls()
    {
        if (_slider != null)
        {
            _slider.Value = ZoomFactor;
            _slider.Minimum = MinZoom;
            _slider.Maximum = MaxZoom;
        }

        if (_textBox != null)
        {
            _textBox.Text = $"{(int)Math.Round(ZoomFactor * 100)}%";
        }
    }

    private void OnSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        ZoomFactor = Math.Round(e.NewValue * 100) / 100.0;
    }

    private void OnSliderKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            _textBox?.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        // Don't update zoom factor while typing
    }

    private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ParseAndSetZoom();
            _slider?.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            _textBox!.Text = $"{(int)Math.Round(ZoomFactor * 100)}%";
            _slider?.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        ParseAndSetZoom();
    }

    private void ParseAndSetZoom()
    {
        if (_textBox == null) return;

        var text = _textBox.Text.Trim().TrimEnd('%');
        if (double.TryParse(text, out var percent))
        {
            var factor = Math.Clamp(percent / 100.0, MinZoom, MaxZoom);
            ZoomFactor = factor;
        }
        else
        {
            _textBox.Text = $"{(int)Math.Round(ZoomFactor * 100)}%";
        }
    }

    private void OnFitWidthClick(object sender, RoutedEventArgs e)
    {
        FitWidthRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnFitPageClick(object sender, RoutedEventArgs e)
    {
        FitPageRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? FitWidthRequested;
    public event EventHandler? FitPageRequested;
}

/// <summary>
/// Event arguments for zoom changes.
/// </summary>
public sealed class ZoomChangedEventArgs : EventArgs
{
    public double OldZoom { get; }
    public double NewZoom { get; }

    public ZoomChangedEventArgs(double oldZoom, double newZoom)
    {
        OldZoom = oldZoom;
        NewZoom = newZoom;
    }
}