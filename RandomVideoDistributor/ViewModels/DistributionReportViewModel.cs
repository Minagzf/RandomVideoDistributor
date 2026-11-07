
using CommunityToolkit.Mvvm.ComponentModel;
using RandomVideoDistributor.Models;

namespace RandomVideoDistributor.ViewModels;

public partial class DistributionReportViewModel : ViewModelBase
{
    public VideoDistributionResult Result { get; }

    public string ReportPath { get; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    public DistributionReportViewModel(
        VideoDistributionResult result,
        string reportPath)
    {
        Result = result;
        ReportPath = reportPath;
    }
}
