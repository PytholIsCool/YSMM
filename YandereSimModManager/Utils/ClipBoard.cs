using Avalonia.Controls;
using System.Threading.Tasks;

namespace YSMM.Utils;

internal static class ClipBoard {
    public static async Task<string?> GetTextAsync(Control context) {
        var clipboard = TopLevel.GetTopLevel(context)?.Clipboard;
        return clipboard != null ? await clipboard.GetTextAsync() : null;
    }
}
