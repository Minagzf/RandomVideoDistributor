
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using RandomVideoDistributor.Models;

namespace RandomVideoDistributor.Services;

public partial class VideoSimilarityService
{
    private const double NameWeight = 0.70;
    private const double TimeWeight = 0.30;
    private const double SameFolderBonus = 0.04;

    // الاحتفاظ بالبيانات المحسوبة لكل فيديو طوال فترة استخدامه.
    private static readonly ConditionalWeakTable<
        VideoFileItem,
        SimilarityInfo> SimilarityCache = new();

    [GeneratedRegex(
      @"(?:[\s._-]*[\(\[\{]*\s*\d+\s*[\)\]\}]*|[\s._-]+\d+)$",
      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClipNumberRegex();

    [GeneratedRegex(
        @"[\s._-]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorsRegex();

    public static double CalculateSimilarity(
        VideoFileItem first,
        VideoFileItem second)
    {
        SimilarityInfo firstInfo =
            SimilarityCache.GetValue(first, CreateSimilarityInfo);

        SimilarityInfo secondInfo =
            SimilarityCache.GetValue(second, CreateSimilarityInfo);

        double nameScore = CalculateNameSimilarity(
            firstInfo.NormalizedName,
            secondInfo.NormalizedName);

        double timeScore = CalculateTimeSimilarity(
            first.CreationTime,
            second.CreationTime);

        double score =
            (nameScore * NameWeight) +
            (timeScore * TimeWeight);

        if (firstInfo.Directory is not null &&
            secondInfo.Directory is not null &&
            string.Equals(
                firstInfo.Directory,
                secondInfo.Directory,
                StringComparison.OrdinalIgnoreCase) &&
            (nameScore >= 0.45 || timeScore >= 0.75))
        {
            score += SameFolderBonus;
        }

        return Math.Min(score, 1.0);
    }

    private static SimilarityInfo CreateSimilarityInfo(
        VideoFileItem video)
    {
        string fileName = Path.GetFileNameWithoutExtension(
            video.FullPath);

        return new SimilarityInfo(
            NormalizeFileName(fileName),
            Path.GetDirectoryName(video.FullPath));
    }

    private static string NormalizeFileName(string fileName)
    {
        string normalized = ClipNumberRegex().Replace(
            fileName.Trim(),
            "");

        normalized = SeparatorsRegex().Replace(
            normalized,
            " ");

        return normalized.Trim().ToLowerInvariant();
    }

    private static double CalculateNameSimilarity(
        string first,
        string second)
    {
        if (first.Length == 0 || second.Length == 0)
            return 0;

        if (first.Equals(
            second,
            StringComparison.Ordinal))
        {
            return 1.0;
        }

        int distance = LevenshteinDistance(first, second);

        int longestLength = Math.Max(
            first.Length,
            second.Length);

        return 1.0 - (double)distance / longestLength;
    }

    private static int LevenshteinDistance(
        string first,
        string second)
    {
        // استخدام الصف الأقصر لتقليل استهلاك الذاكرة.
        if (second.Length > first.Length)
        {
            (first, second) = (second, first);
        }

        int[] previous = new int[second.Length + 1];
        int[] current = new int[second.Length + 1];

        for (int j = 0; j <= second.Length; j++)
        {
            previous[j] = j;
        }

        for (int i = 1; i <= first.Length; i++)
        {
            current[0] = i;

            for (int j = 1; j <= second.Length; j++)
            {
                int cost = first[i - 1] == second[j - 1]
                    ? 0
                    : 1;

                current[j] = Math.Min(
                    Math.Min(
                        current[j - 1] + 1,
                        previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[second.Length];
    }

    private static double CalculateTimeSimilarity(
        DateTime first,
        DateTime second)
    {
        double differenceMinutes = Math.Abs(
            (first - second).TotalMinutes);

        return Math.Exp(-differenceMinutes / 30.0);
    }

    private sealed record SimilarityInfo(
        string NormalizedName,
        string? Directory);
}
