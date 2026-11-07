
namespace RandomVideoDistributor.Models;

public class VideoCopyRecord
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string NewFileName { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;

    public bool IsSuccessful { get; set; }
    public bool IsCompleted { get; set; }

    public string? ErrorMessage { get; set; }

    public long BytesCopied { get; set; }
    public long TotalBytes { get; set; }

    public string StatusText =>
    !IsCompleted ? "جارٍ التنفيذ" :
    IsSuccessful ? "نجح" : "فشل";
    public string StatusColor =>
    IsSuccessful ? "#16803C" : "#DC2626";
}
