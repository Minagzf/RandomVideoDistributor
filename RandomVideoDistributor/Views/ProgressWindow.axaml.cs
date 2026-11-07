
using Avalonia.Controls;
using RandomVideoDistributor.ViewModels;

namespace RandomVideoDistributor.Views;

public partial class ProgressWindow : Window
{
    // Constructor بدون Parameters
    public ProgressWindow()
    {
        InitializeComponent();
    }

    // Constructor يستقبل ViewModel
    public ProgressWindow(ProgressViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
