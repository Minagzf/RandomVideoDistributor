
using CommunityToolkit.Mvvm.ComponentModel;
using RandomVideoDistributor.Models;
using System;

namespace RandomVideoDistributor.ViewModels;

public partial class ProgressViewModel : ViewModelBase
{
    // Progress percentages
    [ObservableProperty]
    public partial double CurrentFileProgress { get; set; }

    [ObservableProperty]
    public partial double OverallProgress { get; set; }

    // Video counters
    [ObservableProperty]
    public partial int TotalVideos { get; set; }

    [ObservableProperty]
    public partial int ProcessedVideos { get; set; }

    [ObservableProperty]
    public partial int SuccessfulCopies { get; set; }

    [ObservableProperty]
    public partial int FailedCopies { get; set; }

    [ObservableProperty]
    public partial string LastFailedFileName { get; set; }
    = "لا توجد ملفات فاشلة حتى الآن";

    [ObservableProperty]
    public partial string LastFailureMessage { get; set; }
        = "—";

    // Current video information
    [ObservableProperty]
    public partial string CurrentFileName { get; set; }
        = "لم يبدأ النسخ بعد";

    [ObservableProperty]
    public partial string OriginalPath { get; set; } = "—";

    [ObservableProperty]
    public partial string NewFileName { get; set; } = "—";

    [ObservableProperty]
    public partial string DestinationPath { get; set; } = "—";

    // Overall data size
    [ObservableProperty]
    public partial long TotalBytes { get; set; }

    
    [ObservableProperty]
    public partial long ProcessedBytes { get; set; }
    // Internal tracking
    private string? _lastReportedFilePath;
    private long _lastReportedFileBytes;
    private bool _lastFileCompleted;

    // Initialize progress before copying starts

    public void InitializeProgress(long totalBytes)
    {
        TotalBytes = totalBytes;
        ProcessedBytes = 0;

        CurrentFileProgress = 0;
        OverallProgress = 0;

        ProcessedVideos = 0;
        SuccessfulCopies = 0;
        FailedCopies = 0;
        LastFailedFileName = "لا توجد ملفات فاشلة حتى الآن";
        LastFailureMessage = "—";
        _lastFileCompleted = false;

        _lastReportedFilePath = null;
        _lastReportedFileBytes = 0;
    }

    // Update current video and overall progress


    public void UpdateCurrentFile(VideoCopyRecord record)
    {
        CurrentFileName = record.OriginalFileName;
        OriginalPath = record.OriginalPath;
        NewFileName = record.NewFileName;
        DestinationPath = record.DestinationPath;

        CurrentFileProgress = record.TotalBytes > 0
            ? record.BytesCopied * 100.0 / record.TotalBytes
            : 0;

        if (_lastReportedFilePath != record.OriginalPath)
        {
            _lastReportedFilePath = record.OriginalPath;
            _lastReportedFileBytes = 0;
            _lastFileCompleted = false;
        }

        long newBytes = record.BytesCopied - _lastReportedFileBytes;

        if (newBytes > 0)
        {
            ProcessedBytes += newBytes;
        }

        _lastReportedFileBytes = record.BytesCopied;

        if (record.IsCompleted && !_lastFileCompleted)
        {
            if (!record.IsSuccessful)
            {
                long remainingBytes = record.TotalBytes - record.BytesCopied;

                if (remainingBytes > 0)
                {
                    ProcessedBytes += remainingBytes;
                }

                LastFailedFileName = record.OriginalFileName;
                LastFailureMessage = record.ErrorMessage
                    ?? "سبب الفشل غير محدد.";
            }

            _lastFileCompleted = true;
        }

        OverallProgress = TotalBytes > 0
            ? Math.Min(100, ProcessedBytes * 100.0 / TotalBytes)
            : 0;
    }


}
