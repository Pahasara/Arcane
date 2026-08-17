using System;
using Avalonia;
using Avalonia.Controls;

namespace Arcane.App.Controls;

/// <summary>
/// Markdown diary editor: Edit / Split / Preview modes.
///
/// Exposes Text as a two-way StyledProperty — bind from EntryEditorViewModel.
/// WordCount is read-only for display in the toolbar.
///
/// Auto-save is NOT done here. EntryEditorViewModel watches OnContentChanged
/// and resets a DispatcherTimer that fires SaveAsync after 2 seconds idle.
/// </summary>
public partial class MarkdownEditor : UserControl
{
    // Styled properties
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MarkdownEditor, string>(
            nameof(Text),
            defaultValue: string.Empty,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay,
            coerce: (_, v) => v ?? string.Empty);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<int> WordCountProperty =
        AvaloniaProperty.Register<MarkdownEditor, int>(nameof(WordCount));

    public int WordCount
    {
        get => GetValue(WordCountProperty);
        private set => SetValue(WordCountProperty, value);
    }

    // State
    private enum EditorMode { Edit, Split, Preview }
    private EditorMode _mode = EditorMode.Edit;

    // Guards against infinite loop: property change → editor update → property change → ...
    private bool _syncingFromProperty;

    // Constructor
    public MarkdownEditor()
    {
        InitializeComponent();

        BtnEdit.Click    += (_, _) => SetMode(EditorMode.Edit);
        BtnSplit.Click   += (_, _) => SetMode(EditorMode.Split);
        BtnPreview.Click += (_, _) => SetMode(EditorMode.Preview);

        Editor.TextChanged += OnEditorTextChanged;
    }

    // Property change (binding → editor)
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty && !_syncingFromProperty)
        {
            var newText = change.GetNewValue<string>();
            if (Editor.Document.Text != newText)
            {
                _syncingFromProperty = true;
                Editor.Document.Text  = newText;
                _syncingFromProperty  = false;
            }
        }
    }

    // Editor change (editor → binding)
    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_syncingFromProperty) return;

        var text = Editor.Document.Text;

        _syncingFromProperty = true;
        Text                 = text;
        _syncingFromProperty = false;

        UpdateCounts(text);

        if (_mode is EditorMode.Split or EditorMode.Preview)
            Preview.Markdown = text;
    }

    // Mode switching
    private void SetMode(EditorMode mode)
    {
        _mode = mode;

        Editor.IsVisible   = mode is EditorMode.Edit or EditorMode.Split;
        Splitter.IsVisible = mode == EditorMode.Split;
        PreviewContainer.IsVisible  = mode is EditorMode.Preview or EditorMode.Split;

        if (PreviewContainer.IsVisible)
            Preview.Markdown = Editor.Document.Text;

        ContentGrid.ColumnDefinitions[0].Width = mode == EditorMode.Preview
            ? new GridLength(0)
            : new GridLength(1, GridUnitType.Star);

        ContentGrid.ColumnDefinitions[2].Width = mode == EditorMode.Edit
            ? new GridLength(0)
            : new GridLength(1, GridUnitType.Star);

        SetActive(BtnEdit,    mode == EditorMode.Edit);
        SetActive(BtnSplit,   mode == EditorMode.Split);
        SetActive(BtnPreview, mode == EditorMode.Preview);
    }

    private static void SetActive(Button btn, bool active)
    {
        if (active) btn.Classes.Add("active");
        else        btn.Classes.Remove("active");
    }

    // Counts
    private void UpdateCounts(string text)
    {
        var words      = string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split([' ', '\n', '\r', '\t'],
                  StringSplitOptions.RemoveEmptyEntries).Length;

        WordCount          = words;
        TbWordCount.Text   = $"{words} words";
        TbCharCount.Text   = $"{text.Length} chars";
    }
}
