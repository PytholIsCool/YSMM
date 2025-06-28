using Avalonia.Interactivity;
using System.Collections.Generic;
using YSMM.Json;
using YSMM.Objects;
using YSMM.Utils;
using Avalonia.Controls;
using Avalonia.Media;
using System.Diagnostics;

namespace YSMM.ModManager;

public partial class ReposPage : ContentPage {
    internal static ReposPage? Instance;

    private const int PageSize = 10;
    private int CurrentPageIndex = 0;
    private readonly List<ListControlItem> AllRepoItems = [];

    public ReposPage() {
        Instance = this;
        canOpen = false;
        InitializeComponent();

        AllRepoItems.Add(AddDefaultRepo());

        var saved = Config.GetAddedRepos();
        foreach (var repo in saved) {
            Trace.WriteLine($"[Repos] Found custom repo: {repo.name}");
            AllRepoItems.Add(CreateRepoItem(repo));
        }

        RenderCurrentPage();
    }

    #region Repos

    private ListControlItem AddDefaultRepo() => new(["YSMG Officially Supported Mods", "https://raw.githubusercontent.com/PytholIsCool/YSMG-Officially-Supported-Mods/main/repos.json"]);

    private ListControlItem CreateRepoItem(RepoEntry repo) {
        var item = new ListControlItem([repo.name, repo.url]);
        item.ChainAddButton("Delete", () => {
            AllRepoItems.Remove(item);
            Config.RemoveRepo(repo);
            RenderCurrentPage();
        });
        return item;
    }

    private void OnClearRepoList(object? sender, RoutedEventArgs e) {
        Config.GetAddedRepos().Clear();
        Config.Save();

        AllRepoItems.Clear();
        AllRepoItems.Add(AddDefaultRepo());
        CurrentPageIndex = 0;
        RenderCurrentPage();
    }

    private void OnAddRepo(object? sender, RoutedEventArgs e) {
        var repo = new RepoEntry() {
            name = RepoNameBox.Text,
            url = RepoUrlBox.Text
        };

        Config.AddRepo(repo);
        AllRepoItems.Add(CreateRepoItem(repo));
        RenderCurrentPage();
    }

    private void OnClearRepo(object? sender, RoutedEventArgs e) {
        RepoNameBox.Text = string.Empty;
        RepoUrlBox.Text = string.Empty;
    }

    #endregion

    #region Pagination

    private void RenderCurrentPage() {
        ModRepos.Items.Clear();

        int start = CurrentPageIndex * PageSize;
        int end = System.Math.Min(start + PageSize, AllRepoItems.Count);

        for (int i = start; i < end; i++) {
            ModRepos.Items.Add(AllRepoItems[i]);
        }

        UpdatePageIndicator();
    }

    private void OnNextPage(object? sender, RoutedEventArgs e) {
        int maxPage = (AllRepoItems.Count + PageSize - 1) / PageSize;
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
        int totalPages = (AllRepoItems.Count + PageSize - 1) / PageSize;
        PageNumberDisplay.Text = $"{CurrentPageIndex + 1} / {totalPages}";
    }

    #endregion

    private void OnOpenSettings(object? sender, RoutedEventArgs e) {
        ContentPanel.Instance?.OpenPage(MainWindow.settingsPage);
    }
}