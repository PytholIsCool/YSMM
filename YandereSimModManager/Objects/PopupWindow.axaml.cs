using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;

namespace YSMM.Objects;
public partial class PopupWindow : Window {
    private TextBlock? messageText => this.FindControl<TextBlock>("MessageText");
    private Button? cancelButton => this.FindControl<Button>("CancelButton");
    private Button? okButton => this.FindControl<Button>("OkButton");
    private Button? confirmButton => this.FindControl<Button>("ConfirmButton");
    public PopupWindow() {
        InitializeComponent();

        Title = "YSMM Popup";

        if (okButton != null)
            okButton.Click += (_, _) => Close();
        if (cancelButton != null)
            cancelButton.Click += (_, _) => Close();
        if (confirmButton != null)
            confirmButton.Click += (_, _) => Close();
    }

    public void SetMessage(string message) {
        if (messageText != null) 
            messageText.Text = message;
    }

    public static async void Show(string message) {
        if (App.Current == null)
            return;
        var popup = new PopupWindow();
        if (popup.cancelButton == null || popup.okButton == null || popup.confirmButton == null) {
            popup.Close();
            return;
        }

        popup.cancelButton.IsVisible = false;
        popup.okButton.IsVisible = true;
        popup.confirmButton.IsVisible = false;

        popup.SetMessage(message);

        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is { } mainWindow)
            await popup.ShowDialog(mainWindow);
        else
            popup.Show();
    }

    public static async void Show(string message, Action OnYesClicked, Action? OnNoClicked = null) {
        if (App.Current == null)
            return;
        var popup = new PopupWindow();
        if (popup.cancelButton == null || popup.okButton == null || popup.confirmButton == null) {
            popup.Close();
            return;
        }

        popup.cancelButton.IsVisible = true;
        popup.okButton.IsVisible = false;
        popup.confirmButton.IsVisible = true;

        popup.SetMessage(message);
        popup.cancelButton.Click += (_, _) => OnNoClicked?.Invoke();
        
        popup.confirmButton.Click += (_, _) => OnYesClicked?.Invoke();

        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is { } mainWindow)
            await popup.ShowDialog(mainWindow);
        else
            popup.Show();
    }
}