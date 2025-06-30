using Avalonia.Interactivity;
using System;
using YSMM.Json;
using YSMM.Objects;

namespace YSMM.ModManager;
public partial class HomePage : ContentPage {
    public HomePage() {
        InitializeComponent();

        if (Config.GetAgreedToTerms() == true)
            Agree();
    }

    private void Agree() {
        if (AboutPage.Instance != null)
            AboutPage.Instance.canOpen = true;

        if (ModsPage.Instance != null)
            ModsPage.Instance.canOpen = true;

        if (SettingsPage.Instance != null)
            SettingsPage.Instance.canOpen = true;

        if (ReposPage.Instance != null)
            ReposPage.Instance.canOpen = true;

        ConfirmationText.IsVisible = true;

        Config.SetAgreedToTerms(true);
    }

    private void OnAgree(object? sender, RoutedEventArgs e) => Agree();

    private void OnDisagree(object? sender, RoutedEventArgs e) {
        if (Config.GetAgreedToTerms() == true)
            return;
        Environment.Exit(0);
    }
}
