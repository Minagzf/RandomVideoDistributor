using CommunityToolkit.Mvvm.ComponentModel;

namespace RandomVideoDistributor.ViewModels;

public partial class SplashViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }
        = "جارٍ تجهيز البرنامج...";
}