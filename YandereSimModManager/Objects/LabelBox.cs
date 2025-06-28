using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ReactiveUI;
using System;
using YSMM.Json;
using YSMM.Utils;

namespace YSMM.Objects;

public class LabelBox : UserControl {
    public static readonly StyledProperty<bool> AllowNewLinesProperty =
        AvaloniaProperty.Register<LabelBox, bool>(nameof(AllowNewLines), defaultValue: true);

    public static readonly StyledProperty<string> DisplayTextProperty =
        AvaloniaProperty.Register<LabelBox, string>(nameof(DisplayText), defaultValue: string.Empty);

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<LabelBox, string>(
            nameof(Text),
            defaultValue: string.Empty,
            defaultBindingMode: BindingMode.TwoWay
        );

    public static readonly StyledProperty<int> CharacterLimitProperty =
    AvaloniaProperty.Register<LabelBox, int>(
        nameof(CharacterLimit),
        defaultValue: 0
    );

    private readonly TextBlock labelBlock;
    private readonly Border border;
    private bool editing = false;

    public bool AllowNewLines {
        get => GetValue(AllowNewLinesProperty);
        set => SetValue(AllowNewLinesProperty, value);
    }

    public string Text {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string DisplayText {
        get => GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public int CharacterLimit {
        get => GetValue(CharacterLimitProperty);
        set => SetValue(CharacterLimitProperty, value);
    }

    public LabelBox() {
        labelBlock = new TextBlock {
            FontSize = 14,
            Focusable = true
        };

        labelBlock.KeyDown += OnKeyDown;
        labelBlock.TextInput += OnTextInput;

        border = new Border {
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            MinHeight = 24,
            Focusable = true,
            Child = labelBlock
        };

        Content = border;

        this.WhenAnyValue(x => x.Text, x => x.DisplayText)
            .Subscribe(_ => {
                if (!editing) RefreshLabelBlock();
            });

        border.PointerPressed += (_, _) => {
            editing = true;
            labelBlock.Focus();
            ApplyTheme();
        };

        AttachedToVisualTree += (_, _) => {
            ApplyTheme();
            Config.OnThemeApplied += ApplyTheme;

            var window = this.GetVisualRoot() as Window;
            window?.AddHandler(PointerPressedEvent, (_, e) => {
                if (!IsPointerOver) {
                    editing = false;
                    RefreshLabelBlock();
                }
            }, RoutingStrategies.Tunnel);
        };
    }

    private void ApplyTheme() {
        border.Background = this.TryFindResource("BrushBackground", out var bg) ? (IBrush?)bg : new SolidColorBrush(Color.Parse("#1e1e2f"));
        border.BorderBrush = this.TryFindResource("BrushAltBackground", out var accent) ? (IBrush?)accent : new SolidColorBrush(Color.Parse("#2c2c3e"));
        RefreshLabelBlock();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e) {
        if (!editing) return;

        if (e.Key == Key.Enter) {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && AllowNewLines) {
                if (CharacterLimit == 0 || Text.Length + 1 <= CharacterLimit) {
                    Text += "\n";
                    RefreshLabelBlock();
                }
            } else {
                editing = false;
                RefreshLabelBlock();
            }
            e.Handled = true;
        } else if (e.Key == Key.Back) {
            if (!string.IsNullOrEmpty(Text)) {
                Text = Text[..^1];
                RefreshLabelBlock();
            }
            e.Handled = true;
        } else if (e.Key == Key.Delete) {
            if (!string.IsNullOrEmpty(Text)) {
                Text = Text.Length > 1 ? Text[1..] : "";
                RefreshLabelBlock();
            }
            e.Handled = true;
        } else if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control)) {
            if (CharacterLimit == 0 || Text.Length < CharacterLimit) 
                PasteTextFromKeyboard();
            e.Handled = true;
        }
    }

    private void OnTextInput(object? sender, TextInputEventArgs e) {
        if (!editing || string.IsNullOrEmpty(e.Text))
            return;

        if (!AllowNewLines && (e.Text.Contains('\n') || e.Text.Contains('\r')))
            return;

        int newLength = Text.Length + e.Text.Length;
        if (CharacterLimit > 0 && newLength > CharacterLimit)
            return;

        Text += e.Text;
        RefreshLabelBlock();
        e.Handled = true;
    }


    private async void PasteTextFromKeyboard() {
        var text = await ClipBoard.GetTextAsync(this);
        if (text == null) return;

        if (!AllowNewLines)
            text = text.Replace("\r", "").Replace("\n", "");

        if (CharacterLimit > 0 && Text.Length + text.Length > CharacterLimit)
            text = text[..Math.Max(0, CharacterLimit - Text.Length)];

        Text += text;
        RefreshLabelBlock();
    }

    private void RefreshLabelBlock() {
        labelBlock.Text = string.IsNullOrEmpty(Text) ? DisplayText : Text;
        labelBlock.Foreground = this.TryFindResource(string.IsNullOrEmpty(Text) ? "BrushSecondary" : "BrushPrimary", out var txtBrush) ? (IBrush?)txtBrush : (string.IsNullOrEmpty(Text) ? new SolidColorBrush(Color.Parse("#888888")) : new SolidColorBrush(Color.Parse("#FFFFFF")));
        border.BorderBrush = this.TryFindResource(editing ? "BrushSecondary" : "BrushAltBackground", out var borderBrush) ? (IBrush?)borderBrush : (string.IsNullOrEmpty(Text) ? new SolidColorBrush(Color.Parse("#888888")) : new SolidColorBrush(Color.Parse("#2c2c3e")));
    }
}