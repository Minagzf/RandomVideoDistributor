
using System.Collections.Generic;

namespace RandomVideoDistributor.Models;

public class VideoExtensionSettings
{
    public List<string> Extensions { get; set; } =
    [
        ".mp4",
        ".mkv",
        ".avi",
        ".mov",
        ".wmv",
        ".flv",
        ".webm",
        ".m4v",
        ".mpg",
        ".mpeg",
        ".3gp",
        ".ts",
        ".mts",
        ".m2ts",
        ".vob",
        ".ogv",
        ".divx",
        ".asf",
        ".rm",
        ".rmvb"
    ];
}
