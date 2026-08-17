using Arcane.Core.Models.Enums;
using Avalonia;
using Avalonia.Controls;

namespace Arcane.App.Controls;

/// <summary>
/// 5-emoji mood selector with two-way bindable SelectedMood.
/// Clicking the already-selected mood deselects it (sets null).
/// </summary>
public partial class MoodPicker : UserControl
{
    public static readonly StyledProperty<MoodLevel?> SelectedMoodProperty =
        AvaloniaProperty.Register<MoodPicker, MoodLevel?>(
            nameof(SelectedMood),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public MoodLevel? SelectedMood
    {
        get => GetValue(SelectedMoodProperty);
        set => SetValue(SelectedMoodProperty, value);
    }

    public MoodPicker()
    {
        InitializeComponent();

        BtnAwful.Click   += (_, _) => Toggle(MoodLevel.Awful);
        BtnBad.Click     += (_, _) => Toggle(MoodLevel.Bad);
        BtnNeutral.Click += (_, _) => Toggle(MoodLevel.Neutral);
        BtnGood.Click    += (_, _) => Toggle(MoodLevel.Good);
        BtnGreat.Click   += (_, _) => Toggle(MoodLevel.Great);
    }

    private void Toggle(MoodLevel mood)
    {
        SelectedMood = SelectedMood == mood ? null : mood;
        UpdateSelectedClass();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedMoodProperty)
            UpdateSelectedClass();
    }

    private void UpdateSelectedClass()
    {
        SetSelected(BtnAwful,   SelectedMood == MoodLevel.Awful);
        SetSelected(BtnBad,     SelectedMood == MoodLevel.Bad);
        SetSelected(BtnNeutral, SelectedMood == MoodLevel.Neutral);
        SetSelected(BtnGood,    SelectedMood == MoodLevel.Good);
        SetSelected(BtnGreat,   SelectedMood == MoodLevel.Great);
    }

    private static void SetSelected(Button btn, bool selected)
    {
        if (selected) btn.Classes.Add("selected");
        else          btn.Classes.Remove("selected");
    }
}
