
using Avalonia.Controls;
using Avalonia.Interactivity;
using RandomVideoDistributor.ViewModels;
using System;
using System.Diagnostics;
using System.IO;

namespace RandomVideoDistributor.Views;

public partial class DistributionReportWindow : Window
{
    public DistributionReportWindow()
    {
        InitializeComponent();
    }

    public DistributionReportWindow(
        DistributionReportViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private void OpenCsvReport_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not DistributionReportViewModel viewModel)
            return;

        try
        {
            if (!File.Exists(viewModel.ReportPath))
            {
                viewModel.Message = "ملف التقرير غير موجود.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = viewModel.ReportPath,
                UseShellExecute = true
            });

            viewModel.Message = "تم طلب فتح تقرير CSV.";
        }
        catch (Exception ex)
        {
            viewModel.Message =
                $"تعذر فتح التقرير: {ex.Message}";
        }
    }

    private void OpenOutputFolder_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not DistributionReportViewModel viewModel)
            return;

        try
        {
            string folderPath = viewModel.Result.RunFolderPath;

            if (!Directory.Exists(folderPath))
            {
                viewModel.Message = "مجلد النتائج غير موجود.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });

            viewModel.Message = "تم طلب فتح مجلد النتائج.";
        }
        catch (Exception ex)
        {
            viewModel.Message =
                $"تعذر فتح مجلد النتائج: {ex.Message}";
        }
    }

    private void CloseReport_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
