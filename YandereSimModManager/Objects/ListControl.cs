using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using YSMM.Json;

namespace YSMM.Objects;

public class ListControl : UserControl {
    public static readonly StyledProperty<int> ListIndexProperty =
    AvaloniaProperty.Register<ListControl, int>(
        nameof(ListIndex),
        defaultValue: 0
    );

    public static readonly StyledProperty<bool> AllowNumberedListProperty =
    AvaloniaProperty.Register<ListControl, bool>(
        nameof(AllowNumberedList),
        defaultValue: true
    );

    private readonly StackPanel ListPanel;

    public ObservableCollection<ListControlItem> Items { get; } = [];
    public bool AllowNumberedList {
        get => GetValue(AllowNumberedListProperty);
        set => SetValue(AllowNumberedListProperty, value);
    }

    public int ListIndex {
        get => GetValue(ListIndexProperty);
        private set => SetValue(ListIndexProperty, value);
    }

    public ListControl() {
        ListPanel = new StackPanel {
            Spacing = 6,
            Margin = new Thickness(8),
        };

        var wrapper = new StackPanel {
            Children = { ListPanel }
        };

        var border = new Border {
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            MinHeight = 24,
            Focusable = true,
            Child = wrapper 
        };

        Content = border;

        AttachedToVisualTree += (_, _) => {
            ApplyTheme();
            Config.OnThemeApplied += ApplyTheme;
        };

        Items.CollectionChanged += (_, _) => RefreshRepoList();
        RefreshRepoList();
    }

    private void ApplyTheme() {
        if (Content is Border border) {
            border.Background = this.TryFindResource("BrushAltBackground", out var bg) ? (IBrush?)bg : new SolidColorBrush(Color.Parse("#1e1e2f"));
            border.BorderBrush = this.TryFindResource("BrushSecondary", out var bb) ? (IBrush?)bb : new SolidColorBrush(Color.Parse("#888888"));
        }

        foreach (var row in ListPanel.Children) {
            if (row is StackPanel stack) {
                foreach (var element in stack.Children) {
                    if (element is TextBlock tb)
                        tb.Foreground = this.TryFindResource("BrushPrimary", out var brush) ? (IBrush?)brush : Brushes.White;
                }
            }
        }
    }

    private void RefreshRepoList() {
        ListPanel.Children.Clear();

        int itemCount = 1;
        foreach (var item in Items) {
            var row = new StackPanel {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };

            string displayText = "";
            if (AllowNumberedList)
                displayText += $"[{itemCount}]   ";

            List<string> parts = item.Content;
            displayText += string.Join("   -   ", parts);

            var label = new TextBlock {
                Text = displayText,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TryResource<IBrush>("BrushPrimary", Brushes.White)
            };

            row.Children.Add(label);

            foreach (var btn in item.Buttons) {
                var button = new Button {
                    Content = btn.Text,
                    Padding = new Thickness(4, 2),
                    MinWidth = 60,
                    Margin = new Thickness(4, 0),
                    FontSize = 12
                };

                button.Classes.Add("button");

                button.Click += (_, _) => {
                    btn.Listener?.Invoke();
                    button.Content = btn.Text;
                };

                if (btn.InvokeOnInit) {
                    btn.Listener?.Invoke();
                    button.Content = btn.Text;
                }

                row.Children.Add(button);
            }

            foreach (var toggle in item.Toggles) {
                toggle.Value = toggle.DefaultValue;

                var checkBox = new CheckBox {
                    IsChecked = toggle.Value,
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = GetToggleContent(toggle, toggle.Value)
                };

                checkBox.Classes.Add("checkbox");

                checkBox.IsCheckedChanged += (_, _) => {
                    toggle.Value = checkBox.IsChecked ?? false;
                    toggle.Listener?.Invoke(toggle.Value);
                    checkBox.Content = GetToggleContent(toggle, toggle.Value);
                };

                if (toggle.InvokeOnInit) {
                    toggle.Listener?.Invoke(toggle.Value);
                    checkBox.Content = GetToggleContent(toggle, toggle.Value);
                }

                row.Children.Add(checkBox);
            }

            ListPanel.Children.Add(row);
            itemCount++;
            ListIndex++;
        }
    }

    private static T TryResource<T>(string key, T fallback) where T : class =>
    Application.Current?.Resources.TryGetValue(key, out var value) == true && value is T t
        ? t
        : fallback;

    private static object GetToggleContent(ListControlItem.LCIToggle toggle, bool value) {
        if (toggle.OnText == null && toggle.OffText == null)
            return toggle.Text ?? string.Empty;

        return value
            ? (toggle.OnText ?? toggle.Text ?? string.Empty)
            : (toggle.OffText ?? toggle.Text ?? string.Empty);
    }
}

public class ListControlItem(List<string> content) {
    public List<string> Content { get; set; } = content;
    public ObservableCollection<LCIButton> Buttons { get; set; } = [];
    public ObservableCollection<LCIToggle> Toggles { get; set; } = [];

    public class LCIButton(string text, Action? onClick = null, bool invokeOnInit = false) {
        public string Text { get; set; } = text;
        public bool InvokeOnInit { get; set; } = invokeOnInit;
        public Action? Listener { get; set; } = onClick;
    }
    public LCIButton AddButton(string text, Action? onClick = null, bool invokeOnInit = false) {
        var button = new LCIButton(text, onClick, invokeOnInit);
        Buttons.Add(button);
        return button;
    }
    public ListControlItem ChainAddButton(string text, Action? onClick = null, bool invokeOnInit = false) {
        var button = new LCIButton(text, onClick, invokeOnInit);
        Buttons.Add(button);
        return this;
    }

    public class LCIToggle {
        public string? Text { get; set; }
        public string? OnText { get; private set; }
        public string? OffText { get; private set; }
        public bool Value { get; set; } 
        public bool DefaultValue { get; private set; }
        public bool InvokeOnInit { get; private set; }
        public Action<bool>? Listener { get; set; }

        public LCIToggle(string text, Action<bool>? onClick = null, bool defaultValue = false, bool invokeOnInit = false) {
            Text = text;
            Listener = onClick;
            DefaultValue = defaultValue;
            InvokeOnInit = invokeOnInit;
        }

        public LCIToggle(string onText, string offText, Action<bool>? onClick = null, bool defaultValue = false, bool invokeOnInit = false) {
            OnText = onText;
            OffText = offText;
            Listener = onClick;
            DefaultValue = defaultValue;
            InvokeOnInit = invokeOnInit;
        }
    }

    public LCIToggle AddToggle(string text, Action<bool>? onClick = null, bool defaultValue = false, bool invokeOnInit = false) {
        var toggle = new LCIToggle(text, onClick, defaultValue, invokeOnInit);
        Toggles.Add(toggle);
        return toggle;
    }
    public LCIToggle AddToggle(string onText, string offText, Action<bool>? onClick = null, bool defaultValue = false, bool invokeOnInit = false) {
        var toggle = new LCIToggle(onText, offText, onClick, defaultValue, invokeOnInit);
        Toggles.Add(toggle);
        return toggle;
    }
    public ListControlItem ChainAddToggle(string text, Action<bool>? onClick = null, bool defaultValue = false, bool invokeOnInit = false) {
        var toggle = new LCIToggle(text, onClick, defaultValue, invokeOnInit);
        Toggles.Add(toggle);
        return this;
    }
    public ListControlItem ChainAddToggle(string onText, string offText, Action<bool>? onClick = null, bool defaultValue = false, bool invokeOnInit = false) {
        var toggle = new LCIToggle(onText, offText, onClick, defaultValue, invokeOnInit);
        Toggles.Add(toggle);
        return this;
    }
}