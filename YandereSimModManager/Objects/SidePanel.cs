using Avalonia.Controls;
using Avalonia.Media;
using YSMM.Json;

namespace YSMM.Objects;
public class SidePanel : StackPanel {
    public SidePanel() {
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        Width = 150;

        AttachedToVisualTree += (_, _) => {
            ApplyTheme();
            Config.OnThemeApplied += ApplyTheme;
        };
    }

    private void ApplyTheme() {
        Background = this.TryFindResource("BrushAltBackground", out var bg) ? (IBrush?)bg : new SolidColorBrush(Color.Parse("#2c2c3e"));
        InvalidateVisual();
    }
}
