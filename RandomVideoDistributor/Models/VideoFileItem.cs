
using System;

namespace RandomVideoDistributor.Models;

public class VideoFileItem
{
    public string FullPath { get; init; } = string.Empty;

    public DateTime CreationTime { get; init; }
}
