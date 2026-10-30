using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Nova.UI.Shared;

/// <summary>
/// Ribbon tab item.
/// </summary>
public partial class RibbonTab : ObservableObject
{
    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private string _glyph = string.Empty;

    [ObservableProperty]
    private ObservableCollection<RibbonGroup> _groups = new();

    [ObservableProperty]
    private bool _isSelected;

    public string AutomationName => Header;
}

/// <summary>
/// Ribbon group within a tab.
/// </summary>
public partial class RibbonGroup : ObservableObject
{
    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private ObservableCollection<RibbonControl> _controls = new();
}

/// <summary>
/// Base class for ribbon controls.
/// </summary>
public abstract partial class RibbonControl : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _glyph = string.Empty;

    [ObservableProperty]
    private string _toolTip = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private ControlSize _size = ControlSize.Normal;

    public abstract ControlType Type { get; }
}

/// <summary>
/// Ribbon control types.
/// </summary>
public enum ControlType
{
    Button,
    ToggleButton,
    SplitButton,
    DropDownButton,
    ComboBox,
    TextBox,
    CheckBox,
    RadioButton,
    Separator,
    Gallery,
    Menu
}

/// <summary>
/// Control size variants.
/// </summary>
public enum ControlSize
{
    Small,
    Normal,
    Large
}

/// <summary>
/// Ribbon button control.
/// </summary>
public sealed partial class RibbonButton : RibbonControl
{
    public override ControlType Type => ControlType.Button;

    public IRelayCommand? Command { get; set; }
    public object? CommandParameter { get; set; }

    [ObservableProperty]
    private bool _isDefault;

    public RibbonButton()
    {
        Size = ControlSize.Normal;
    }
}

/// <summary>
/// Ribbon toggle button.
/// </summary>
public sealed partial class RibbonToggleButton : RibbonControl
{
    public override ControlType Type => ControlType.ToggleButton;

    public IRelayCommand? Command { get; set; }

    [ObservableProperty]
    private bool _isChecked;

    public RibbonToggleButton()
    {
        Size = ControlSize.Normal;
    }
}

/// <summary>
/// Ribbon split button (button with dropdown).
/// </summary>
public sealed partial class RibbonSplitButton : RibbonControl
{
    public override ControlType Type => ControlType.SplitButton;

    public IRelayCommand? Command { get; set; }
    public ObservableCollection<RibbonMenuItem> Items { get; } = new();

    [ObservableProperty]
    private bool _isDropDownOpen;

    public RibbonSplitButton()
    {
        Size = ControlSize.Normal;
    }
}

/// <summary>
/// Ribbon dropdown button.
/// </summary>
public sealed partial class RibbonDropDownButton : RibbonControl
{
    public override ControlType Type => ControlType.DropDownButton;

    public ObservableCollection<RibbonMenuItem> Items { get; } = new();

    [ObservableProperty]
    private bool _isDropDownOpen;

    public RibbonDropDownButton()
    {
        Size = ControlSize.Normal;
    }
}

/// <summary>
/// Ribbon combo box.
/// </summary>
public sealed partial class RibbonComboBox : RibbonControl
{
    public override ControlType Type => ControlType.ComboBox;

    public ObservableCollection<object> Items { get; } = new();

    [ObservableProperty]
    private object? _selectedItem;

    [ObservableProperty]
    private string _displayMemberPath = string.Empty;

    [ObservableProperty]
    private bool _isEditable;

    [ObservableProperty]
    private string _text = string.Empty;

    public IRelayCommand? SelectionChangedCommand { get; set; }

    public RibbonComboBox()
    {
        Size = ControlSize.Normal;
    }
}

/// <summary>
/// Ribbon text box.
/// </summary>
public sealed partial class RibbonTextBox : RibbonControl
{
    public override ControlType Type => ControlType.TextBox;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _placeholderText = string.Empty;

    [ObservableProperty]
    private int _maxLength;

    public IRelayCommand? TextChangedCommand { get; set; }

    public RibbonTextBox()
    {
        Size = ControlSize.Normal;
        MaxLength = 0;
    }
}

/// <summary>
/// Ribbon check box.
/// </summary>
public sealed partial class RibbonCheckBox : RibbonControl
{
    public override ControlType Type => ControlType.CheckBox;

    public IRelayCommand? Command { get; set; }

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private bool _isThreeState;

    [ObservableProperty]
    private bool? _nullValue;

    public RibbonCheckBox()
    {
        Size = ControlSize.Normal;
    }
}

/// <summary>
/// Ribbon separator.
/// </summary>
public sealed class RibbonSeparator : RibbonControl
{
    public override ControlType Type => ControlType.Separator;

    public RibbonSeparator()
    {
        Label = string.Empty;
        Glyph = string.Empty;
    }
}

/// <summary>
/// Ribbon menu item for dropdowns.
/// </summary>
public sealed partial class RibbonMenuItem : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _glyph = string.Empty;

    [ObservableProperty]
    private string _toolTip = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isCheckable;

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private ObservableCollection<RibbonMenuItem> _subItems = new();

    public IRelayCommand? Command { get; set; }
    public object? CommandParameter { get; set; }
    public bool HasSubItems => SubItems.Count > 0;
}

/// <summary>
/// Ribbon gallery item.
/// </summary>
public sealed partial class RibbonGalleryItem : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private ImageSource? _image;

    [ObservableProperty]
    private object? _value;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Ribbon gallery control.
/// </summary>
public sealed partial class RibbonGallery : RibbonControl
{
    public override ControlType Type => ControlType.Gallery;

    public ObservableCollection<RibbonGalleryItem> Items { get; } = new();

    [ObservableProperty]
    private int _columns = 4;

    [ObservableProperty]
    private double _itemWidth = 64;

    [ObservableProperty]
    private double _itemHeight = 64;

    public IRelayCommand<RibbonGalleryItem>? ItemClickCommand { get; set; }

    public RibbonGallery()
    {
        Size = ControlSize.Large;
    }
}

/// <summary>
/// Main ribbon control.
/// </summary>
public sealed class Ribbon : Control
{
    public static readonly DependencyProperty TabsProperty =
        DependencyProperty.Register(nameof(Tabs), typeof(ObservableCollection<RibbonTab>), typeof(Ribbon), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedTabIndexProperty =
        DependencyProperty.Register(nameof(SelectedTabIndex), typeof(int), typeof(Ribbon), new PropertyMetadata(0, OnSelectedTabIndexChanged));

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(Ribbon), new PropertyMetadata(false, OnIsCompactChanged));

    public static readonly DependencyProperty AccentColorProperty =
        DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(Ribbon), new PropertyMetadata(Colors.Blue));

    public ObservableCollection<RibbonTab> Tabs
    {
        get => (ObservableCollection<RibbonTab>)GetValue(TabsProperty);
        set => SetValue(TabsProperty, value);
    }

    public int SelectedTabIndex
    {
        get => (int)GetValue(SelectedTabIndexProperty);
        set => SetValue(SelectedTabIndexProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public event EventHandler<RibbonTabChangedEventArgs>? TabChanged;

    public Ribbon()
    {
        DefaultStyleKey = typeof(Ribbon);
        Tabs = new ObservableCollection<RibbonTab>();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState();
    }

    private static void OnSelectedTabIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateVisualState();
            ribbon.TabChanged?.Invoke(ribbon, new RibbonTabChangedEventArgs((int)e.OldValue, (int)e.NewValue));
        }
    }

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        VisualStateManager.GoToState(this, IsCompact ? "Compact" : "Expanded", true);
    }
}

/// <summary>
/// Event arguments for ribbon tab changes.
/// </summary>
public sealed class RibbonTabChangedEventArgs : EventArgs
{
    public int OldIndex { get; }
    public int NewIndex { get; }

    public RibbonTabChangedEventArgs(int oldIndex, int newIndex)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }
}

/// <summary>
/// Quick Access Toolbar.
/// </summary>
public sealed class QuickAccessToolbar : Control
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<RibbonControl>), typeof(QuickAccessToolbar), new PropertyMetadata(null));

    public ObservableCollection<RibbonControl> Items
    {
        get => (ObservableCollection<RibbonControl>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public QuickAccessToolbar()
    {
        DefaultStyleKey = typeof(QuickAccessToolbar);
        Items = new ObservableCollection<RibbonControl>();
    }
}