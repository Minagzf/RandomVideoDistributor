using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RandomVideoDistributor.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }
    
    private void CloseButton_Click(
    object? sender,
    RoutedEventArgs e)
        {
            Close();
        }
    }
}