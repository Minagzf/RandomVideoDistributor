using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace RandomVideoDistributor.Views;

public partial class CloseConfirmationWindow : Window
{
    public CloseConfirmationWindow()
    {
        InitializeComponent();
    }

    private void Confirm_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    private void Window_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Close(true);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close(false);
            e.Handled = true;
        }
    }
}