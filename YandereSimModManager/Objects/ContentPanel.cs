using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace YSMM.Objects;
public class ContentPanel : Grid {
    internal static ContentPanel? Instance;
    public ContentPanel() {
        Instance = this;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
    }

    internal void OpenPage(ContentPage? page) {
        if (page == null)
            return;
        if (page.canOpen == false)
            return;

        Children.Clear();
        Children.Add(page);
        InvalidateMeasure();
    }
}