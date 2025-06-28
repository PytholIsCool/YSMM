using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace YSMM.Objects;
public partial class PopupWindow : Window {
    private TextBlock? messageText => this.FindControl<TextBlock>("MessageText");
    private Button? okButton => this.FindControl<Button>("OkButton");
    public PopupWindow() {
        InitializeComponent();
        if (okButton != null)
            okButton.Click += (_, _) => Close();
    }

    public void SetMessage(string message) {
        if (messageText != null) 
            messageText.Text = message;
    }

    public static async void Show(string message) {
        if (App.Current == null)
            return;
        var popup = new PopupWindow();
        popup.SetMessage(message);

        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is { } mainWindow) {
            await popup.ShowDialog(mainWindow);
        } else {
            popup.Show();
        }
    }
}