
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RandomVideoDistributor.Models;
using RandomVideoDistributor.Services;
using RandomVideoDistributor.Views;
using RandomVideoDistributor.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace RandomVideoDistributor.ViewModels;

public partial class MainViewModel : ViewModelBase

{
    private VideoFileItem[] _scannedVideos = [];
    private readonly VideoExtensionSettingsService _settingsService = new();
    private readonly VideoReportService _reportService = new();

    private VideoExtensionSettings _extensionSettings = new();
    [ObservableProperty]
    public partial string? SourceFolder { get; set; }

    [ObservableProperty]
    public partial string? OutputFolder { get; set; }

    [ObservableProperty]
    public partial decimal? VideosPerFolder { get; set; } = 5;

    [ObservableProperty]
    public partial int VideoCount { get; set; }

    [ObservableProperty]
    public partial int OutputFolderCount { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }
        = "لم يتم فحص أي مجلد حتى الآن.";
    public MainViewModel()
       : this(new VideoExtensionSettingsService().Load())
    {
    }

    public MainViewModel(VideoExtensionSettings extensionSettings)
    {
        _extensionSettings = extensionSettings;
    }

    [RelayCommand]
    private async Task ChooseSourceFolder()
    {
        var folder = await PickFolderAsync("اختر مجلد الفيديوهات المصدر");

        if (folder is not null)
        {
            SourceFolder = folder;
            StatusMessage = "تم اختيار مجلد المصدر. اضغط فحص الفيديوهات.";
        }
    }

    [RelayCommand]
    private async Task ChooseOutputFolder()
    {
        var folder = await PickFolderAsync("اختر مكان حفظ النتائج");

        if (folder is not null)
        {
            OutputFolder = folder;
        }
    }

    private static async Task<string?> PickFolderAsync(string title)
    {
        var window =
            (App.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes
                    .IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        var topLevel = TopLevel.GetTopLevel(window);

        if (topLevel?.StorageProvider is not { } storageProvider)
        {
            return null;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

        return folders.Count > 0
            ? folders[0].Path.LocalPath
            : null;
    }

    [RelayCommand]
    private async Task ScanVideos()
    {
        if (string.IsNullOrWhiteSpace(SourceFolder) ||
            !Directory.Exists(SourceFolder))
        {
            StatusMessage = "من فضلك اختر مجلد مصدر صالحًا.";
            return;
        }
        _extensionSettings = _settingsService.Load();

        StatusMessage = "جارٍ فحص الفيديوهات...";
        VideoCount = 0;
        OutputFolderCount = 0;



        try
        {

            var files = await Task.Run(() =>
                Directory.EnumerateFiles(
                    SourceFolder,
                    "*",
                    SearchOption.AllDirectories)
                .Where(file =>
                    _extensionSettings.Extensions.Contains(
                        Path.GetExtension(file).TrimStart('.'),
                        StringComparer.OrdinalIgnoreCase))
                .Select(file => new VideoFileItem
                {
                    FullPath = file,
                    CreationTime = File.GetCreationTime(file)
                })
                .ToArray());

            _scannedVideos = files;
            VideoCount = files.Length;

            OutputFolderCount = VideoCount == 0 ||
     VideosPerFolder is null ||
     VideosPerFolder < 1
     ? 0
     : (int)Math.Ceiling(VideoCount / VideosPerFolder.Value);


            StatusMessage = VideoCount == 0
                ? "اكتمل الفحص، ولم يتم العثور على فيديوهات مدعومة."
                : "اكتمل الفحص بنجاح.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر إكمال الفحص: {ex.Message}";
        }
    }

    [RelayCommand]

    private async Task DistributeVideos()
    {
        if (string.IsNullOrWhiteSpace(SourceFolder) ||
            !Directory.Exists(SourceFolder))
        {
            StatusMessage = "من فضلك اختر مجلد مصدر صالحًا.";
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputFolder) ||
            !Directory.Exists(OutputFolder))
        {
            StatusMessage = "من فضلك اختر مجلد إخراج صالحًا.";
            return;
        }

        if (VideosPerFolder is null ||
            VideosPerFolder < 1 ||
            VideosPerFolder != decimal.Truncate(VideosPerFolder.Value))
        {
            StatusMessage = "أدخل عددًا صحيحًا أكبر من صفر.";
            return;
        }

        if (_scannedVideos.Length == 0)
        {
            StatusMessage = "افحص الفيديوهات أولًا قبل بدء التوزيع.";
            return;
        }

        string sourcePath = Path.GetFullPath(SourceFolder);
        string outputPath = Path.GetFullPath(OutputFolder);

        string relativePath = Path.GetRelativePath(
            sourcePath,
            outputPath);

        if (relativePath == "." ||
            (!Path.IsPathRooted(relativePath) &&
             relativePath != ".." &&
             relativePath.StartsWith(
                 $"..{Path.DirectorySeparatorChar}",
                 StringComparison.Ordinal)))
        {
            StatusMessage = "اختر مجلد إخراج خارج مجلد المصدر.";
            return;
        }

        ProgressWindow? progressWindow = null;

        try
        {
            StatusMessage = "جارٍ تجهيز عملية النسخ...";

            // حساب إجمالي أحجام الفيديوهات بالبايت.
            long totalBytes = _scannedVideos.Sum(
                video => new FileInfo(video.FullPath).Length);

            // تجهيز نموذج بيانات نافذة التقدم.
            var progressViewModel = new ProgressViewModel
            {
                TotalVideos = _scannedVideos.Length
            };

            progressViewModel.InitializeProgress(totalBytes);

            // إنشاء نافذة التقدم وعرضها.
            progressWindow = new ProgressWindow(progressViewModel);
            progressWindow.Show();

            // استقبال تحديثات النسخ على واجهة المستخدم.
            var progress = new Progress<VideoCopyRecord>(record =>
            {
                progressViewModel.UpdateCurrentFile(record);

                if (record.IsCompleted)
                {
                    progressViewModel.ProcessedVideos++;

                    if (record.IsSuccessful)
                    {
                        progressViewModel.SuccessfulCopies++;
                    }
                    else
                    {
                        progressViewModel.FailedCopies++;
                    }
                }
            });

            StatusMessage = "جارٍ توزيع الفيديوهات ونسخها...";

            var result = await Task.Run(() =>
                VideoDistributionService.Distribute(
                    _scannedVideos,
                    (int)VideosPerFolder.Value,
                    outputPath,
                    progress));


            // حفظ التقرير بعد انتهاء النسخ.
            string reportPath = await Task.Run(() =>
                _reportService.SaveCsvReport(result));

            // إغلاق نافذة التقدم بعد اكتمال النسخ وحفظ التقرير.
            progressWindow?.Close();
            progressWindow = null;

            // تحديث حالة العملية.
            StatusMessage =
                $"اكتملت العملية. إجمالي الفيديوهات: {result.TotalVideos}، " +
                $"نجح النسخ: {result.SuccessfulCopies}، " +
                $"فشل النسخ: {result.FailedCopies}، " +
                $"عدد المجلدات: {result.CreatedFolderCount}.\n" +
                $"تم حفظ التقرير في: {reportPath}";

            // تجهيز نافذة التقرير.
            var reportViewModel = new DistributionReportViewModel(result, reportPath);

            var reportWindow = new DistributionReportWindow(reportViewModel);

            // الحصول على النافذة الرئيسية لتكون مالك نافذة التقرير.
            var mainWindow =
                (App.Current?.ApplicationLifetime
                    as Avalonia.Controls.ApplicationLifetimes
                        .IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            // عرض التقرير.
            if (mainWindow is not null)
            {
                await reportWindow.ShowDialog(mainWindow);
            }
            else
            {
                reportWindow.Show();
            }


            StatusMessage =
                $"اكتملت العملية. إجمالي الفيديوهات: {result.TotalVideos}، " +
                $"نجح النسخ: {result.SuccessfulCopies}، " +
                $"فشل النسخ: {result.FailedCopies}، " +
                $"عدد المجلدات: {result.CreatedFolderCount}.\n" +
                $"تم حفظ التقرير في: {reportPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر إكمال التوزيع: {ex.Message}";
        }
        finally
        {
            progressWindow?.Close();
        }
    }


}
