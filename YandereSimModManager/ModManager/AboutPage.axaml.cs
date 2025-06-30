using Avalonia.Interactivity;
using YSMM.Objects;
using YSMM.Utils;

namespace YSMM.ModManager; 
public partial class AboutPage : ContentPage {
    internal static AboutPage? Instance;
    public AboutPage() {
        InitializeComponent();
        Instance = this;
        canOpen = false;
    }

    private void OnGithubClicked(object? sender, RoutedEventArgs e) => WebUtils.OpenURL(@"https://github.com/PytholIsCool/YSMM");
    private void OnDiscordClicked(object? sender, RoutedEventArgs e) => WebUtils.OpenURL(@"https://discord.gg/bmwJ74rRNV");
    private void OnYoutubeClicked(object? sender, RoutedEventArgs e) => WebUtils.OpenURL("");
}
