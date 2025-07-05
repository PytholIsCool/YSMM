using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using YSMM.Json;
using YSMM.ModManager;
using YSMM.Objects;
using YSMM.Utils;

namespace YSMM;

public partial class MainWindow : Window {
    public static ContentPage? homePage, modsPage, reposPage, settingsPage, aboutPage;

    public MainWindow() {
        InitializeComponent();

        Trace.Listeners.Add(new TextWriterTraceListener("log.txt"));
        Trace.AutoFlush = true;
        Trace.WriteLine("App started");

        Config.Create();

        modsPage = new ModsPage();
        reposPage = new ReposPage();
        settingsPage = new SettingsPage();
        aboutPage = new AboutPage();

        homePage = new HomePage(); // The home page is created last so it can unlock the other pages and they aren't null

        ContentPanel.Instance?.OpenPage(homePage);
        SettingsPage.Instance?.OnUpdateCheck(new(), new());
    }

    private void OnHomeClick(object? sender, RoutedEventArgs e) => ContentPanel.Instance?.OpenPage(homePage);
    private void OnModsClick(object? sender, RoutedEventArgs e) => ContentPanel.Instance?.OpenPage(modsPage);
    private void OnSettingsClick(object? sender, RoutedEventArgs e) => ContentPanel.Instance?.OpenPage(settingsPage);
    private void OnAboutClick(object? sender, RoutedEventArgs e) => ContentPanel.Instance?.OpenPage(aboutPage);
}
