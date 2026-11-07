
using System.Collections.Generic;

namespace RandomVideoDistributor.Models;

public class VideoDistributionResult
{
    public int TotalVideos { get; set; }

    public int SuccessfulCopies { get; set; }

    public int FailedCopies { get; set; }

    public int CreatedFolderCount { get; set; }

    public string RunFolderPath { get; set; } = string.Empty;

    public List<VideoCopyRecord> Records { get; set; } = [];
}
