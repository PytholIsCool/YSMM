using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using YSMM.Json;

namespace YSMM.Objects; 
public partial class ContentPage : UserControl {
    internal bool canOpen = true;

    public ContentPage() {
        AttachedToVisualTree += (_, _) => {
            ApplyTheme();
            Config.OnThemeApplied += ApplyTheme;
        };
    }

    private void ApplyTheme() {
        Background = this.TryFindResource("BrushBackground", out var bg) ? (IBrush?)bg : new SolidColorBrush(Color.Parse("#1e1e2f"));
        InvalidateVisual();
    }
}
