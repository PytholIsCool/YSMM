using System.Text.Json;
using System.Collections.Generic;
using YSMM.Objects;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using YSMM.Json;
using Avalonia.Interactivity;
using System.IO;
using YSMM.Utils;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using System.Diagnostics;
using System.Linq;
using System.IO.Compression;

namespace YSMM.ModManager;

public partial class ModsPage : ContentPage {
    internal static ModsPage? Instance;

    internal Dictionary<string, string> Repos = [];
    internal bool isFetching = false;

    private List<List<ListControlItem>> PagedBlocks = [];
    private int CurrentPageIndex = 0;
    private const int PageSize = 6; // 6 mod blocks per page (mod + buttons)

    private readonly ListControl MainListControl;

    public ModsPage() {
        InitializeComponent();
        Instance = this;
        canOpen = false;

        MainListControl = new ListControl {
            AllowNumberedList = false
        };

        ModListContainer.Children.Add(new Border {
            Margin = new Thickness(8),
            Padding = new Thickness(4),
            Background = Brushes.Transparent,
            Child = MainListControl
        });

        _ = LoadModsAsync();
    }

    #region Loading

    internal async Task RefreshRepos() {
        if (ReposPage.Instance == null)
            return;

        Repos.Clear();
        foreach (var item in ReposPage.Instance.ModRepos.Items) {
            if (item.Content[1] == null)
                continue;

            Repos.Add(item.Content[0], item.Content[1]);
            await UpdateLoadingText($"Repo found: {item.Content[0]}, {item.Content[1]}");
        }
    }

    private async Task LoadModsAsync() {
        isFetching = true;
        LoadingIndicator.IsVisible = true;

        await UpdateLoadingText("Loading...");
        await RefreshRepos();

        using var client = new HttpClient();
        var blocks = new List<List<ListControlItem>>();

        foreach (var repo in Repos) {
            string url = Path.GetFileName(repo.Value) == "repos.json"
                ? repo.Value
                : repo.Value.TrimEnd('/') + "/repos.json";

            try {
                await UpdateLoadingText($"Fetching repos.json from {repo.Key}...");
                string json = await client.GetStringAsync(url);
                var categories = JsonSerializer.Deserialize<Dictionary<string, List<ModSelection>>>(json);
                await UpdateLoadingText($"Parsed repos.json for {repo.Key}");

                blocks.Add([new ListControlItem(["Repository:", repo.Key])]);

                if (categories == null)
                    continue;

                await UpdateLoadingText($"Parsing categories...");
                foreach (var category in categories) {
                    blocks.Add([new ListControlItem(["Category:", category.Key])]);

                    foreach (var mod in category.Value) {
                        if (string.IsNullOrWhiteSpace(mod.version) &&
                            mod.url.Contains("github.com") && mod.url.Contains("/releases/latest")) {
                            try {
                                using var handler = new HttpClientHandler {
                                    AllowAutoRedirect = false
                                };
                                using var redirectClient = new HttpClient(handler);
                                var response = await redirectClient.GetAsync(mod.url);

                                if (response.StatusCode == System.Net.HttpStatusCode.Found &&
                                    response.Headers.Location != null) {
                                    string redirectedUrl = response.Headers.Location.ToString();
                                    var tag = Path.GetFileName(redirectedUrl);
                                    mod.version = tag;
                                }
                            } catch (Exception ex) {
                                Trace.WriteLine($"[Mod Version] Failed to fetch version from: {mod.url} - {ex.Message}");
                            }
                        }

                        bool isSelected = Config.GetSelectedMods().Exists(m => m.name == mod.name && m.url == mod.url);

                        var installState = Config.GetInstallState(mod.url);
                        bool isInstalled = installState != null;
                        bool needsUpdate = isInstalled && !string.IsNullOrWhiteSpace(installState?.installedVersion) && mod.version != installState.installedVersion;

                        string versionDisplay = $"Version: {(string.IsNullOrWhiteSpace(mod.version) ? "?" : mod.version)}";
                        string updateIndicator = needsUpdate ? " (Update Available)" : "";
                        string shortDescription = Truncate(mod.description ?? string.Empty, 50);

                        blocks.Add([
                            new ListControlItem([
                                mod.name,
                                $"{versionDisplay}{updateIndicator}",
                                mod.url,
                                shortDescription,
                                mod.author ?? "Unknown"
                            ]),
                            new ListControlItem([])
                                .ChainAddButton("View Link", () => WebUtils.OpenURL(mod.url))
                                .ChainAddToggle("X", "✓", val => {
                                    if (val)
                                        Config.AddSelectedMod(mod);
                                    else
                                        Config.RemoveSelectedMod(mod);
                                }, defaultValue: isSelected)
                        ]);
                    }
                }
            } catch {
                await UpdateLoadingText($"Failed to fetch mods from: {repo.Key}");
            }
        }

        await UpdateLoadingText("Finalizing mod list...");
        PagedBlocks = blocks;
        CurrentPageIndex = 0;
        RenderCurrentPage();

        LoadingIndicator.IsVisible = false;
        isFetching = false;
    }

    string Truncate(string text, int maxLength) {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }

    private async Task UpdateLoadingText(string text) {
        Trace.WriteLine($"[Mods] {text}");
        LoadingIndicator.Text = text;
        LoadingIndicator.InvalidateVisual();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        await Task.Delay(10);
    }

    private void OnRefresh(object? sender, RoutedEventArgs e) {
        if (isFetching)
            return;

        MainListControl.Items.Clear();
        _ = LoadModsAsync();
    }

    #endregion

    #region Pagination

    private void RenderCurrentPage() {
        MainListControl.Items.Clear();

        int start = CurrentPageIndex * PageSize;
        int end = Math.Min(start + PageSize, PagedBlocks.Count);

        for (int i = start; i < end; i++) {
            foreach (var item in PagedBlocks[i]) {
                MainListControl.Items.Add(item);
            }
        }

        UpdatePageIndicator();
    }

    private void OnNextPage(object? sender, RoutedEventArgs e) {
        int maxPage = (PagedBlocks.Count + PageSize - 1) / PageSize;
        if (CurrentPageIndex < maxPage - 1) {
            CurrentPageIndex++;
            RenderCurrentPage();
        }
    }

    private void OnPrevPage(object? sender, RoutedEventArgs e) {
        if (CurrentPageIndex > 0) {
            CurrentPageIndex--;
            RenderCurrentPage();
        }
    }

    private void UpdatePageIndicator() {
        int totalPages = (PagedBlocks.Count + PageSize - 1) / PageSize;
        PageNumberDisplay.Text = $"{CurrentPageIndex + 1} / {totalPages}";
    }

    private void ApplyTheme() {
        if (ModListContainer.Children.Count > 0 &&
            ModListContainer.Children[0] is Border border) {
            border.Background = this.TryFindResource("BrushBackground", out var bg)
                ? (IBrush?)bg : Brushes.Transparent;
        }

        MainListControl.InvalidateVisual();
        LoadingIndicator.Foreground = this.TryFindResource("BrushPrimary", out var brush)
            ? (IBrush?)brush : Brushes.White;
    }

    #endregion

    #region Downloads

    private async void OnDownload(object? sender, RoutedEventArgs e) {
        string? installPath = Config.GetGamePath();
        if (string.IsNullOrWhiteSpace(installPath)) return;

        var selectedMods = Config.GetSelectedMods();
        var installedMods = Config.GetInstalledModStates();

        foreach (var mod in selectedMods) {
            var installState = Config.GetInstallState(mod.url);
            string? currentVersion = installState?.installedVersion;
            string? newVersion = mod.version;

            if (installState == null || currentVersion != newVersion) {
                Trace.WriteLine($"[ModInstall] Installing: {mod.name}");
                await InstallMod(mod, installPath);
            } else {
                Trace.WriteLine($"[ModInstall] Skipping up-to-date mod: {mod.name}");
            }
        }

        foreach (var installed in installedMods.ToList()) {
            if (!selectedMods.Any(m => m.url == installed.url)) {
                Trace.WriteLine($"[ModUninstall] Removing unselected mod: {installed.name}");
                await UninstallMod(installed.url, installPath);
            }
        }
    }

    private async Task InstallMod(ModSelection mod, string installPath) {
        string tempZip = Path.GetTempFileName();
        string downloadUrl = mod.url;

        if (downloadUrl.Contains("github.com") && downloadUrl.Contains("/releases/latest")) {
            try {
                using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using var redirectClient = new HttpClient(handler);
                var response = await redirectClient.GetAsync(downloadUrl);

                if (response.StatusCode == System.Net.HttpStatusCode.Found && response.Headers.Location != null) {
                    string tagUrl = response.Headers.Location.ToString();
                    string baseUrl = tagUrl.Replace("/tag/", "/download/").TrimEnd('/');
                    downloadUrl = baseUrl + "/Install.zip";

                    Trace.WriteLine($"[ModInstall] Redirected download URL: {downloadUrl}");
                } else
                    Trace.WriteLine("[ModInstall] Could not resolve GitHub redirect.");
            } catch (Exception ex) {
                Trace.WriteLine($"[ModInstall] Failed to resolve GitHub redirect for {mod.url}: {ex.Message}");
                return;
            }
        }

        try {
            using var http = new HttpClient();
            byte[] data = await http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(tempZip, data);

            var installedFiles = new List<string>();

            using var zip = ZipFile.OpenRead(tempZip);
            foreach (var entry in zip.Entries) {
                string fullPath = Path.Combine(installPath, entry.FullName);

                if (string.IsNullOrEmpty(entry.Name)) {
                    Directory.CreateDirectory(fullPath);
                } else {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                    entry.ExtractToFile(fullPath, overwrite: true);
                    installedFiles.Add(Path.GetRelativePath(installPath, fullPath));
                }
            }

            Config.SetInstallState(mod.name, mod.url, mod.version ?? string.Empty, installedFiles);
        } catch (Exception ex) {
            Trace.WriteLine($"[ModInstall] Failed to install {mod.name}: {ex.Message}");
            
        } finally {
            if (File.Exists(tempZip))
                File.Delete(tempZip);
        }
    }

    private async Task UninstallMod(string modUrl, string installPath) {
        var installState = Config.GetInstallState(modUrl);
        if (installState == null) return;

        foreach (string relativePath in installState.installedFiles) {
            string fullPath = Path.Combine(installPath, relativePath);
            try {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                else if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, recursive: true);
            } catch (Exception ex) {
                Trace.WriteLine($"[Uninstall] Failed to delete {relativePath}: {ex.Message}");
            }
        }

        Config.RemoveInstallState(modUrl);
        await Task.Delay(100);
    }

    #endregion

}