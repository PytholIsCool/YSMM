using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Diagnostics;
using System.Text.Json;
using System.IO;
using System;
using YSMM.Objects;
using YSMM.Utils;
using YSMM.Json;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Path = System.IO.Path;

namespace YSMM.ModManager;

public partial class SettingsPage : ContentPage {
    public static SettingsPage? Instance;

    public SettingsPage() {
        canOpen = false;
        Instance = this;
        InitializeComponent();

        GamePath.Text = Config.GetGamePath() ?? string.Empty;

        ExportsPath.Text = Config.GetExportsPath() ?? string.Empty;

        LoadAppliedThemeOnStartup();
    }

    #region Game Path

    private async void SetPath() {
        var path = await FilePicker.GetPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        GamePath.Text = path;
        Config.SetGamePath(path);
    }

    private void OnSetPath(object? sender, RoutedEventArgs e) => SetPath();
    private void OnClearPath(object? sender, RoutedEventArgs e) => GamePath.Text = string.Empty;
    private void OnOpenPath(object? sender, RoutedEventArgs e) => FilePicker.OpenPath(GamePath.Text);

    #endregion

    #region Theming

    private const string AppliedThemeFile = "applied_theme.json";

    private static readonly JsonSerializerOptions JsonOpts = new() {
        WriteIndented = true
    };

    private class ThemeData {
        public string ColorPrimary { get; set; } = "#FFFFFF";
        public string ColorSecondary { get; set; } = "#888888";
        public string ColorBackground { get; set; } = "#1e1e2f";
        public string ColorAltBackground { get; set; } = "#2c2c3e";
        public string ColorHover { get; set; } = "#333333";
        public string ColorPressed { get; set; } = "#222222";
    }

    private ThemeData GetThemeFromInputs() => new() {
        ColorPrimary = PrimaryHex.Text,
        ColorSecondary = SecondaryHex.Text,
        ColorBackground = BackgroundHex.Text,
        ColorAltBackground = BackgroundAltHex.Text,
        ColorHover = HoverHex.Text,
        ColorPressed = ClickedHex.Text
    };

    private void SetInputsFromTheme(ThemeData data) {
        PrimaryHex.Text = data.ColorPrimary;
        SecondaryHex.Text = data.ColorSecondary;
        BackgroundHex.Text = data.ColorBackground;
        BackgroundAltHex.Text = data.ColorAltBackground;
        HoverHex.Text = data.ColorHover;
        ClickedHex.Text = data.ColorPressed;
    }

    private static void ApplyThemeToApp(ThemeData data) {
        var res = Application.Current!.Resources;

        res["ColorPrimary"] = Color.Parse(data.ColorPrimary);
        res["ColorSecondary"] = Color.Parse(data.ColorSecondary);
        res["ColorBackground"] = Color.Parse(data.ColorBackground);
        res["ColorAltBackground"] = Color.Parse(data.ColorAltBackground);
        res["ColorHover"] = Color.Parse(data.ColorHover);
        res["ColorPressed"] = Color.Parse(data.ColorPressed);

        File.WriteAllText(AppliedThemeFile, JsonSerializer.Serialize(data, JsonOpts));
        Trace.WriteLine("[Theming] Theme applied and saved.");
    }

    public void LoadAppliedThemeOnStartup() {
        try {
            var json = File.ReadAllText(Path.Combine(Config.ThemesPath, AppliedThemeFile));
            var data = JsonSerializer.Deserialize<ThemeData>(json);
            if (data != null) {
                ApplyThemeToApp(data);
                SetInputsFromTheme(data);
#pragma warning disable CS8625
                OnApplyTheme(null, null);
#pragma warning restore CS8625
            }
        } catch (Exception ex) {
            Trace.WriteLine($"[Theming] Failed to load applied theme: {ex.Message}");
        }
    }

    private void OnResetTheme(object? sender, RoutedEventArgs e) {
        SetInputsFromTheme(new ThemeData());
        Trace.WriteLine("[Theming] Reset to default theme values.");
    }

    private void OnApplyTheme(object? sender, RoutedEventArgs e) {
        var theme = GetThemeFromInputs();
        var path = Path.Combine(Config.ThemesPath, "applied_theme.json");

        try {
            File.WriteAllText(path, JsonSerializer.Serialize(theme, JsonOpts));
            Config.SetLastThemePath(path);
            Config.ApplyTheme(path);
            Trace.WriteLine("[Theming] Applied and saved current theme.");
        } catch (Exception ex) {
            Trace.WriteLine($"[Theming] Failed to apply theme: {ex.Message}");
        }
    }

    private async void OnImportTheme(object? sender, RoutedEventArgs e) {
        var path = await FilePicker.GetFile(".json", "Import Theme");
        if (string.IsNullOrWhiteSpace(path)) {
            Trace.WriteLine("[Theming] Import canceled.");
            return;
        }

        try {
            var json = File.ReadAllText(path);
            var theme = JsonSerializer.Deserialize<ThemeData>(json);
            if (theme != null) {
                SetInputsFromTheme(theme);
                Trace.WriteLine($"[Theming] Loaded theme \"{Path.GetFileName(path)}\" into fields (not yet applied).");
            }
        } catch (Exception ex) {
            Trace.WriteLine($"[Theming] Failed to import theme: {ex.Message}");
        }
    }

    private void OnExportTheme(object? sender, RoutedEventArgs e) {
        var exportPath = ExportsPath.Text;
        if (string.IsNullOrEmpty(exportPath)) {
            Trace.WriteLine("[Settings] No export path found for the theme.");
            return;
        }
        if (string.IsNullOrEmpty(ThemeName.Text)) {
            Trace.WriteLine("[Settings] No name for the theme found.");
            return;
        }

        try {
            var theme = GetThemeFromInputs();
            File.WriteAllText(Path.Combine(exportPath, $"{ThemeName.Text}.json"), JsonSerializer.Serialize(theme, JsonOpts));
            PopupWindow.Show($"Exported theme to \"{Path.GetFileName(exportPath)}\".");
            Trace.WriteLine($"[Theming] Exported theme to \"{Path.GetFileName(exportPath)}\".");
        } catch (Exception ex) {
            Trace.WriteLine($"[Theming] Failed to export theme: {ex.Message}");
        }
    }

    private async void SetExportsPath() {
        var path = await FilePicker.GetPath();

        if (!string.IsNullOrWhiteSpace(path)) {
            ExportsPath.Text = path;
            Config.SetExportsPath(path);
        }
    }

    private void OnSetExportsPath(object? sender, RoutedEventArgs e) => SetExportsPath();
    private void OnClearExportsPath(object? sender, RoutedEventArgs e) => ExportsPath.Text = string.Empty;
    private void OnOpenExportsPath(object? sender, RoutedEventArgs e) => FilePicker.OpenPath(ExportsPath.Text);

    #endregion

    #region Diagnostics

    private void OnOpenLocalFolder(object? sender, RoutedEventArgs e) => FilePicker.OpenPath(FilePicker.GetLocalFolder());
    private void OnOpenConfigFolder(object? sender, RoutedEventArgs e) => FilePicker.OpenPath(Config.ConfigFolderPath);
    private void OnOpenInstallFolder(object? sender, RoutedEventArgs e) => FilePicker.OpenPath(AppContext.BaseDirectory);

    #endregion

    private void OnOpenRepos(object? sender, RoutedEventArgs e) =>
        ContentPanel.Instance?.OpenPage(MainWindow.reposPage);
}
