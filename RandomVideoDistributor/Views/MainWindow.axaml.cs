using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RandomVideoDistributor.Views;

public partial class MainWindow : Window
{
    private bool _allowClose;
    private bool _isConfirmationOpen;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void MainWindow_Closing(
        object? sender,
        WindowClosingEventArgs e)
    {
        // السماح بالإغلاق بعد تأكيد المستخدم.
        if (_allowClose)
            return;

        // منع الإغلاق مؤقتًا حتى تظهر رسالة التأكيد.
        e.Cancel = true;

        // منع فتح أكثر من رسالة تأكيد في الوقت نفسه.
        if (_isConfirmationOpen)
            return;

        _isConfirmationOpen = true;

        try
        {
            var dialog = new CloseConfirmationWindow();

            bool confirmed = await dialog.ShowDialog<bool>(this);

            if (confirmed)
            {
                _allowClose = true;
                Close();
            }
        }
        finally
        {
            _isConfirmationOpen = false;
        }
    }

    private void CloseButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private async void OpenExtensionSettings_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var dialog = new ExtensionSettingsWindow();

        await dialog.ShowDialog(this);
    }
}