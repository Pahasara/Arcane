using System;
using Arcane.Core.Models.Entities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Arcane.App.Controls;

/// <summary>
/// Colored pill badge for a Tag. Three usage modes controlled by properties:
///
///   Display only:      IsRemovable=false, IsToggleActive=false
///   Removable (editor): IsRemovable=true  — shows × button, raises RemoveRequested
///   Toggle filter:      IsToggleActive bindable — shows a ring when active, raises Clicked
///
/// Background color derives from TagData.ColorHex at ~20% opacity;
/// text uses the full-opacity color for contrast against the dark theme.
/// </summary>
public partial class TagChip : UserControl
{
    public static readonly StyledProperty<Tag?> TagDataProperty =
        AvaloniaProperty.Register<TagChip, Tag?>(nameof(TagData));

    public Tag? TagData
    {
        get => GetValue(TagDataProperty);
        set => SetValue(TagDataProperty, value);
    }

    public static readonly StyledProperty<bool> IsRemovableProperty =
        AvaloniaProperty.Register<TagChip, bool>(nameof(IsRemovable));

    public bool IsRemovable
    {
        get => GetValue(IsRemovableProperty);
        set => SetValue(IsRemovableProperty, value);
    }

    public static readonly StyledProperty<bool> IsToggleActiveProperty =
        AvaloniaProperty.Register<TagChip, bool>(
            nameof(IsToggleActive),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public bool IsToggleActive
    {
        get => GetValue(IsToggleActiveProperty);
        set => SetValue(IsToggleActiveProperty, value);
    }

    /// <summary>Raised when the × button is clicked (IsRemovable mode).</summary>
    public event EventHandler? RemoveRequested;

    /// <summary>Raised when the chip body is clicked (used for filter toggling).</summary>
    public event EventHandler? Clicked;

    public TagChip()
    {
        InitializeComponent();

        BtnRemove.Click += (_, e) =>
        {
            RemoveRequested?.Invoke(this, EventArgs.Empty);
        };

        ChipBorder.PointerPressed += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TagDataProperty)       Render();
        if (change.Property == IsRemovableProperty)   BtnRemove.IsVisible = IsRemovable;
        if (change.Property == IsToggleActiveProperty) UpdateActiveClass();
    }

    private void Render()
    {
        if (TagData is null) return;

        TbName.Text = TagData.Name;

        var baseColor = Color.Parse(TagData.ColorHex);

        // Background: same hue at low opacity so it reads well on the dark theme
        ChipBorder.Background = new SolidColorBrush(baseColor, 0.22);
        TbName.Foreground     = new SolidColorBrush(Lighten(baseColor));

        UpdateActiveClass();
    }

    private void UpdateActiveClass()
    {
        if (IsToggleActive) ChipBorder.Classes.Add("active");
        else                ChipBorder.Classes.Remove("active");
    }

    /// <summary>Lightens a color for readable text on a dark background.</summary>
    private static Color Lighten(Color c)
    {
        byte Blend(byte channel) => (byte)Math.Min(255, channel + (255 - channel) * 0.5);
        return Color.FromRgb(Blend(c.R), Blend(c.G), Blend(c.B));
    }
}
