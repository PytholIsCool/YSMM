using System;

namespace YSMM.Utils; 
internal static class WebUtils {
    internal static void OpenURL(string? url) {
        try {
            if (!IsValidURL(url))
                return;
            using var process = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = url,
                    UseShellExecute = true
                }
            };
            process.Start();
        } catch {}
    }

    internal static bool IsValidURL(string? input) => Uri.TryCreate(input, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
