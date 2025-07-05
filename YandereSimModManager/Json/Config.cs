using System;
using System.IO;
using System.Text.Json;
using YSMM.Utils;
using Avalonia;
using Avalonia.Media;
using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Generic;
using System.Linq;

namespace YSMM.Json;

internal static class Config {

    public static readonly string YSMMVersion = "1.2.0";
    public static readonly string githubLatestUrl = "https://github.com/PytholIsCool/YSMM/releases/latest";

    #region Properties 

    public static readonly string ConfigFolderPath = Path.Combine(FilePicker.GetLocalFolder(), "YSMM");
    public static readonly string ConfigFilePath = Path.Combine(ConfigFolderPath, "config.json");
    public static readonly string ThemesPath = Path.Combine(ConfigFolderPath, "Themes");

    public static event Action? OnThemeApplied;

    #endregion

    #region Boot

    private class ConfigData {
        public bool AgreedToTerms { get; set; } = false;
        public string? LastGamePath { get; set; }
        public string? LastThemePath { get; set; }
        public string? LastExportsPath { get; set; }
        public List<RepoEntry> AddedRepos { get; set; } = [];
        public List<ModSelection> SelectedMods { get; set; } = [];
        public List<ModInstallState> InstalledMods { get; set; } = [];
    }

    private static ConfigData data = new();

    public static void Create() {
        Directory.CreateDirectory(ConfigFolderPath);
        Directory.CreateDirectory(ThemesPath);

        if (!File.Exists(ConfigFilePath))
            Save();
        else
            Load();
    }

    public static void Load() {
        try {
            var json = File.ReadAllText(ConfigFilePath);
            data = JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();

            if (!string.IsNullOrWhiteSpace(data.LastThemePath) && File.Exists(data.LastThemePath))
                ApplyTheme(data.LastThemePath);
            else
                Trace.WriteLine("[Config] No saved theme to load.");
        } catch (Exception ex) {
            Trace.WriteLine($"[Config] Failed to load config: {ex.Message}");
        }
    }

    public static void Save() {
        try {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        } catch (Exception ex) {
            Trace.WriteLine($"[Config] Failed to save config: {ex.Message}");
        }
    }

    public static bool GetAgreedToTerms() => data.AgreedToTerms;
    public static void SetAgreedToTerms(bool value) {
        data.AgreedToTerms = value;
        Save();
    }

    #endregion

    #region Mods

    public static List<ModSelection> GetSelectedMods() => data.SelectedMods;

    public static void AddSelectedMod(ModSelection mod) {
        if (!data.SelectedMods.Any(m => m.url == mod.url)) {
            data.SelectedMods.Add(mod);
            Save();
        }
    }

    public static void RemoveSelectedMod(ModSelection mod) {
        var match = data.SelectedMods.FirstOrDefault(m => m.url == mod.url);
        if (match != null) {
            data.SelectedMods.Remove(match);
            Save();
        }
    }

    public static List<ModInstallState> GetInstalledModStates() => data.InstalledMods;
    public static ModInstallState? GetInstallState(string url) =>
        data.InstalledMods.FirstOrDefault(m => m.url == url);

    public static void SetInstallState(string name, string url, string version, List<string> files) {
        var existing = GetInstallState(url);
        if (existing != null) {
            existing.installedVersion = version;
            existing.installedFiles = files;
        } else {
            data.InstalledMods.Add(new ModInstallState {
                name = name,
                url = url,
                installedVersion = version,
                installedFiles = files
            });
        }
        Save();
    }

    public static void RemoveInstallState(string url) {
        var match = GetInstallState(url);
        if (match != null) {
            data.InstalledMods.Remove(match);
            Save();
        }
    }

    #endregion

    #region Settings

    public static string? GetGamePath() => data.LastGamePath;
    public static void SetGamePath(string? path) {
        data.LastGamePath = path;
        Save();
    }

    public static string GetExportsPath() =>
        string.IsNullOrWhiteSpace(data.LastExportsPath)
            ? ThemesPath
            : data.LastExportsPath;

    public static void SetExportsPath(string? path) {
        data.LastExportsPath = path;
        Save();
    }

    public static void SetLastThemePath(string path) {
        data.LastThemePath = path;
        Save();
    }

    private class ThemeData {
        public string ColorPrimary { get; set; } = "#FFFFFF";
        public string ColorSecondary { get; set; } = "#888888";
        public string ColorBackground { get; set; } = "#1e1e2f";
        public string ColorAltBackground { get; set; } = "#2c2c3e";
        public string ColorHover { get; set; } = "#333333";
        public string ColorPressed { get; set; } = "#222222";
    }

    public static void ApplyTheme(string path) {
        try {
            var json = File.ReadAllText(path);
            var theme = JsonSerializer.Deserialize<ThemeData>(json);
            if (theme == null) return;

            var res = Application.Current!.Resources;

            res["ColorPrimary"] = Color.Parse(theme.ColorPrimary);
            res["ColorSecondary"] = Color.Parse(theme.ColorSecondary);
            res["ColorBackground"] = Color.Parse(theme.ColorBackground);
            res["ColorAltBackground"] = Color.Parse(theme.ColorAltBackground);
            res["ColorHover"] = Color.Parse(theme.ColorHover);
            res["ColorPressed"] = Color.Parse(theme.ColorPressed);

#pragma warning disable CS8605
            res["BrushPrimary"] = new SolidColorBrush((Color)res["ColorPrimary"]);
            res["BrushSecondary"] = new SolidColorBrush((Color)res["ColorSecondary"]);
            res["BrushBackground"] = new SolidColorBrush((Color)res["ColorBackground"]);
            res["BrushAltBackground"] = new SolidColorBrush((Color)res["ColorAltBackground"]);
            res["BrushHover"] = new SolidColorBrush((Color)res["ColorHover"]);
            res["BrushPressed"] = new SolidColorBrush((Color)res["ColorPressed"]);
#pragma warning restore CS8605

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime) {
                foreach (var window in lifetime.Windows)
                    window.InvalidateVisual();
            }

            OnThemeApplied?.Invoke();
            Trace.WriteLine($"[Theming] Applied theme: {Path.GetFileName(path)}");
        } catch (Exception ex) {
            Trace.WriteLine($"[Theming] Failed to apply theme from config: {ex.Message}");
        }
    }

    #endregion

    #region Repos

    public static List<RepoEntry> GetAddedRepos() => data.AddedRepos;

    public static void AddRepo(RepoEntry repo) {
        if (!data.AddedRepos.Exists(r => r.url == repo.url)) {
            data.AddedRepos.Add(repo);
            Save();
        }
    }

    public static void RemoveRepo(RepoEntry repo) {
        var match = data.AddedRepos.FirstOrDefault(r => r.url == repo.url && r.name == repo.name);
        if (match != null) {
            data.AddedRepos.Remove(match);
            Save();
        }
    }

    #endregion
}

#pragma warning disable CS8618
public class ModSelection {
    public string name { get; set; }
    public string url { get; set; }
    public string? version { get; set; }
    public string description { get; set; }
    public string author { get; set; }
}

public class ModInstallState {
    public string name { get; set; }
    public string url { get; set; }
    public string installedVersion { get; set; } = "";
    public List<string> installedFiles { get; set; } = [];
}

public class RepoEntry {
    public string name { get; set; }
    public string url { get; set; }
}
#pragma warning restore CS8618