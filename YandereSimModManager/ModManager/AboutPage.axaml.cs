using Avalonia.Interactivity;
using YSMM.Objects;
using YSMM.Utils;

namespace YSMM.ModManager; 
public partial class AboutPage : ContentPage {
    internal static AboutPage? Instance;
    public AboutPage() {
        Instance = this;
        canOpen = false;
        InitializeComponent();
    }

    private void OnGithubClicked(object? sender, RoutedEventArgs e) => WebUtils.OpenURL(GithubLink.Text);
    private void OnDiscordClicked(object? sender, RoutedEventArgs e) => WebUtils.OpenURL(@"https://discord.gg/bmwJ74rRNV");
    private void OnYoutubeClicked(object? sender, RoutedEventArgs e) => WebUtils.OpenURL("");
}
