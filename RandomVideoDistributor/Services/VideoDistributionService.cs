
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RandomVideoDistributor.Models;

namespace RandomVideoDistributor.Services;

public class VideoDistributionService
{
    public static VideoDistributionResult Distribute(
        IReadOnlyList<VideoFileItem> videos,
        int videosPerFolder,
        string outputFolder,
        IProgress<VideoCopyRecord>? progress = null)
    {
        if (videosPerFolder < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(videosPerFolder),
                "عدد الفيديوهات في كل مجلد يجب أن يكون أكبر من صفر.");
        }

        if (videos.Count == 0)
        {
            throw new InvalidOperationException(
                "لا توجد فيديوهات لتوزيعها.");
        }

        Directory.CreateDirectory(outputFolder);

        string runFolder = CreateUniqueRunFolder(outputFolder);

        var result = new VideoDistributionResult
        {
            TotalVideos = videos.Count,
            RunFolderPath = runFolder
        };

        var random = Random.Shared;

        // إنشاء مجموعات التوزيع وفق خوارزمية التشابه الحالية.
        var groups = BuildDistributionGroups(
            videos,
            videosPerFolder,
            random);

        // مجلد مؤقت لمحاولات النسخ قبل نقل الملفات إلى أماكنها النهائية.
        string stagingFolder = Path.Combine(
            runFolder,
            "_Staging");

        Directory.CreateDirectory(stagingFolder);

        int attemptIndex = 0;

        try
        {
            // المرور على الفيديوهات بترتيب المجموعات الناتج عن الخوارزمية.
            foreach (var group in groups)
            {
                foreach (var video in group)
                {
                    attemptIndex++;

                    // تحديد المكان التالي بناءً على عدد النسخ الناجحة فقط.
                    int groupIndex =
                        result.SuccessfulCopies / videosPerFolder;

                    int indexInGroup =
                        result.SuccessfulCopies % videosPerFolder;

                    string extension = Path.GetExtension(video.FullPath);

                    string newFileName =
                        $"group_{groupIndex + 1:000}_video_{indexInGroup + 1:000}{extension}";

                    string groupFolder = Path.Combine(
                        runFolder,
                        $"Group_{groupIndex + 1:000}");

                    string destinationPath = Path.Combine(
                        groupFolder,
                        newFileName);

                    // اسم مؤقت فريد لكل محاولة، حتى لا تتداخل الملفات الفاشلة.
                    string temporaryFileName =
                        $"attempt_{attemptIndex:000000}{extension}";

                    string temporaryPath = Path.Combine(
                        stagingFolder,
                        temporaryFileName);

                    var record = new VideoCopyRecord
                    {
                        OriginalFileName = Path.GetFileName(video.FullPath),
                        OriginalPath = video.FullPath,
                        NewFileName = newFileName,
                        DestinationPath = destinationPath
                    };

                    try
                    {
                        record.TotalBytes =
                            new FileInfo(video.FullPath).Length;

                        // النسخ أولًا إلى المجلد المؤقت.
                        CopyFileWithProgress(
                            video.FullPath,
                            temporaryPath,
                            record,
                            progress);

                        // إنشاء مجلد الإخراج عند الحاجة.
                        Directory.CreateDirectory(groupFolder);

                        // نقل الملف المكتمل إلى مكانه النهائي.
                        File.Move(
                            temporaryPath,
                            destinationPath);

                        record.IsSuccessful = true;
                        result.SuccessfulCopies++;
                    }
                    catch (Exception ex) when (
                        ex is IOException ||
                        ex is UnauthorizedAccessException ||
                        ex is System.Security.SecurityException ||
                        ex is NotSupportedException)
                    {
                        record.IsSuccessful = false;
                        record.ErrorMessage = ex.Message;

                        result.FailedCopies++;

                        // حذف أي ملف مؤقت متبقٍ بعد فشل المحاولة.
                        try
                        {
                            if (File.Exists(temporaryPath))
                            {
                                File.Delete(temporaryPath);
                            }
                        }
                        catch
                        {
                            // ستتم محاولة تنظيف المجلد المؤقت في النهاية.
                        }
                    }

                    record.IsCompleted = true;

                    result.Records.Add(record);

                    progress?.Report(record);
                }
            }
        }
        finally
        {
            // تنظيف المجلد المؤقت بعد انتهاء جميع المحاولات.
            try
            {
                if (Directory.Exists(stagingFolder))
                {
                    Directory.Delete(stagingFolder, true);
                }
            }
            catch
            {
                // لا نوقف العملية بسبب تعذر تنظيف الملفات المؤقتة.
            }
        }

        // حذف أي مجلد إخراج فارغ نتج عن فشل عملية النقل النهائي.
        foreach (string directory in Directory.GetDirectories(
            runFolder,
            "Group_*",
            SearchOption.TopDirectoryOnly))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                try
                {
                    Directory.Delete(directory);
                }
                catch
                {
                    // لا نوقف العملية بسبب تعذر حذف مجلد فارغ.
                }
            }
        }

        result.CreatedFolderCount = Directory.GetDirectories(
            runFolder,
            "Group_*",
            SearchOption.TopDirectoryOnly).Length;

        return result;
    }
    private static string CreateUniqueRunFolder(string outputFolder)
    {
        string baseName =
            $"VideoDistribution_{DateTime.Now:yyyyMMdd_HHmmss}";

        string candidate = Path.Combine(outputFolder, baseName);
        int suffix = 1;

        while (Directory.Exists(candidate) || File.Exists(candidate))
        {
            candidate = Path.Combine(
                outputFolder,
                $"{baseName}_{suffix++}");
        }

        Directory.CreateDirectory(candidate);

        return candidate;
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }




    private static List<List<VideoFileItem>> BuildDistributionGroups(
        IReadOnlyList<VideoFileItem> videos,
        int videosPerFolder,
        Random random)
    {
        int videoCount = videos.Count;

        int groupCount = (int)Math.Ceiling(
            (double)videoCount / videosPerFolder);

        var groups = Enumerable.Range(0, groupCount)
            .Select(_ => new List<VideoFileItem>())
            .ToList();

        // حساب التشابه الشامل بين جميع أزواج الفيديوهات.
        double[,] similarityMatrix =
            BuildSimilarityMatrix(videos);

        // ترتيب الفيديوهات حسب مجموع مربعات التشابه
        // مع بقية الفيديوهات.
        var videoPriority = new List<(int Index, double Score)>(
            videoCount);

        for (int i = 0; i < videoCount; i++)
        {
            double score = 0;

            for (int j = 0; j < videoCount; j++)
            {
                if (i == j)
                    continue;

                double similarity = similarityMatrix[i, j];

                score += similarity * similarity;
            }

            videoPriority.Add((i, score));
        }

        // خلط ترتيب التعادل قبل الترتيب حسب الأولوية.
        var orderedIndices = Enumerable.Range(0, videoCount).ToList();

        Shuffle(orderedIndices, random);

        var priorityScores = videoPriority.ToDictionary(
            item => item.Index,
            item => item.Score);

        orderedIndices = orderedIndices
            .OrderByDescending(index => priorityScores[index])
            .ToList();

        // تحديد سعة كل مجموعة.
        var groupCapacity = Enumerable.Repeat(
            videosPerFolder,
            groupCount).ToArray();

        groupCapacity[groupCount - 1] =
            videoCount - (videosPerFolder * (groupCount - 1));

        // مجموع مربعات التشابه بين كل مجموعة
        // وكل فيديو يمكن إضافته إليها.
        var groupAffinity = new double[groupCount, videoCount];

        // التوزيع الأولي.
        foreach (int videoIndex in orderedIndices)
        {
            int minimumGroupCount = int.MaxValue;

            for (int groupIndex = 0;
                 groupIndex < groupCount;
                 groupIndex++)
            {
                if (groups[groupIndex].Count < groupCapacity[groupIndex])
                {
                    minimumGroupCount = Math.Min(
                        minimumGroupCount,
                        groups[groupIndex].Count);
                }
            }

            int bestGroupIndex = -1;
            double bestScore = double.MaxValue;

            for (int groupIndex = 0;
                 groupIndex < groupCount;
                 groupIndex++)
            {
                var group = groups[groupIndex];

                if (group.Count != minimumGroupCount ||
                    group.Count >= groupCapacity[groupIndex])
                {
                    continue;
                }

                double score = groupAffinity[groupIndex, videoIndex];

                // كسر التعادل عشوائيًا.
                score += random.NextDouble() * 0.000001;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestGroupIndex = groupIndex;
                }
            }

            if (bestGroupIndex < 0)
            {
                throw new InvalidOperationException(
                    "تعذر العثور على مجموعة متاحة لإضافة الفيديو.");
            }

            groups[bestGroupIndex].Add(videos[videoIndex]);

            // تحديث درجات التشابه للمجموعة بعد إضافة الفيديو.
            for (int i = 0; i < videoCount; i++)
            {
                double similarity = similarityMatrix[videoIndex, i];

                groupAffinity[bestGroupIndex, i] +=
                    similarity * similarity;
            }
        }

        // تحسين التوزيع بالتبديل بين المجموعات.
        OptimizeDistribution(
            groups,
            similarityMatrix,
            groupAffinity,
            videos);

        return groups;
    }

    private static double[,] BuildSimilarityMatrix(
        IReadOnlyList<VideoFileItem> videos)
    {
        const double SourceAdjacencySimilarity = 0.65;

        int count = videos.Count;

        var matrix = new double[count, count];

        // أولًا: حساب التشابه الطبيعي بين جميع أزواج الفيديوهات.
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                double similarity =
                    VideoSimilarityService.CalculateSimilarity(
                        videos[i],
                        videos[j]);

                matrix[i, j] = similarity;
                matrix[j, i] = similarity;
            }
        }

        // ثانيًا: تجميع الفيديوهات حسب مجلد المصدر.
        var sourceGroups = Enumerable.Range(0, count)
            .GroupBy(
                index => Path.GetDirectoryName(
                    videos[index].FullPath) ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        foreach (var sourceGroup in sourceGroups)
        {
            // ترتيب ملفات كل مجلد أبجديًا حسب اسم الملف.
            var orderedIndices = sourceGroup
                .OrderBy(
                    index => Path.GetFileName(videos[index].FullPath),
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    index => videos[index].FullPath,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // اعتبار كل ملفين متجاورين في الترتيب
            // زوجًا يستحق عقوبة عند اجتماعهما في مجموعة واحدة.
            for (int i = 0; i < orderedIndices.Length - 1; i++)
            {
                int firstIndex = orderedIndices[i];
                int secondIndex = orderedIndices[i + 1];

                double adjustedSimilarity = Math.Max(
                    matrix[firstIndex, secondIndex],
                    SourceAdjacencySimilarity);

                matrix[firstIndex, secondIndex] = adjustedSimilarity;
                matrix[secondIndex, firstIndex] = adjustedSimilarity;
            }
        }

        return matrix;
    }


    private static void OptimizeDistribution(
        List<List<VideoFileItem>> groups,
        double[,] similarityMatrix,
        double[,] groupAffinity,
        IReadOnlyList<VideoFileItem> videos)
    {
        int videoCount = videos.Count;

        // تحديد المجموعة والموقع الحالي لكل فيديو.
        // إنشاء خريطة تربط كل فيديو بفهرسه الأصلي.
        var videoIndices = new Dictionary<VideoFileItem, int>(
            ReferenceEqualityComparer.Instance);

        for (int i = 0; i < videoCount; i++)
        {
            videoIndices.Add(videos[i], i);
        }

        // تحديد المجموعة والموقع الحالي لكل فيديو.
        var groupOfVideo = new int[videoCount];
        var positionInGroup = new int[videoCount];

        for (int groupIndex = 0;
             groupIndex < groups.Count;
             groupIndex++)
        {
            for (int position = 0;
                 position < groups[groupIndex].Count;
                 position++)
            {
                VideoFileItem video = groups[groupIndex][position];

                if (!videoIndices.TryGetValue(video, out int videoIndex))
                {
                    throw new InvalidOperationException(
                        "تعذر تحديد موقع أحد الفيديوهات.");
                }

                groupOfVideo[videoIndex] = groupIndex;
                positionInGroup[videoIndex] = position;
            }
        }

        // البحث التكراري عن أفضل تبديل يحسن النتيجة.
        while (true)
        {
            double bestDelta = -1e-12;

            int bestFirst = -1;
            int bestSecond = -1;

            for (int i = 0; i < videoCount; i++)
            {
                int candidateFirstGroupIndex = groupOfVideo[i];

                for (int j = i + 1; j < videoCount; j++)
                {
                    int candidateSecondGroupIndex = groupOfVideo[j];

                    if (candidateFirstGroupIndex == candidateSecondGroupIndex)
                        continue;

                    double pairSimilarity =
                        similarityMatrix[i, j];

                    double pairScore =
                        pairSimilarity * pairSimilarity;

                    double delta =
     groupAffinity[candidateFirstGroupIndex, j]
     - groupAffinity[candidateFirstGroupIndex, i]
     + groupAffinity[candidateSecondGroupIndex, i]
     - groupAffinity[candidateSecondGroupIndex, j]
     - (2.0 * pairScore);

                    if (delta < bestDelta)
                    {
                        bestDelta = delta;
                        bestFirst = i;
                        bestSecond = j;
                    }
                }
            }

            // لا يوجد تبديل إضافي يحسن النتيجة.
            if (bestFirst < 0)
                break;

            int first = bestFirst;
            int second = bestSecond;

            int firstGroupIndex = groupOfVideo[first];
            int secondGroupIndex = groupOfVideo[second];

            int firstPosition = positionInGroup[first];
            int secondPosition = positionInGroup[second];

            var firstGroupList = groups[firstGroupIndex];
            var secondGroupList = groups[secondGroupIndex];

            // تنفيذ التبديل.
            firstGroupList[firstPosition] = videos[second];
            secondGroupList[secondPosition] = videos[first];

            groupOfVideo[first] = secondGroupIndex;
            groupOfVideo[second] = firstGroupIndex;

            positionInGroup[first] = secondPosition;
            positionInGroup[second] = firstPosition;

            // تحديث درجات التشابه بعد التبديل.
            for (int i = 0; i < videoCount; i++)
            {
                double firstSimilarity =
                    similarityMatrix[i, first];

                double secondSimilarity =
                    similarityMatrix[i, second];

                double difference =
                    (secondSimilarity * secondSimilarity) -
                    (firstSimilarity * firstSimilarity);

                groupAffinity[firstGroupIndex, i] += difference;
                groupAffinity[secondGroupIndex, i] -= difference;
            }
        }
    }

    private static void CopyFileWithProgress(
        string sourcePath,
        string destinationPath,
        VideoCopyRecord record,
        IProgress<VideoCopyRecord>? progress)
    {
        const int bufferSize = 1024 * 1024;

        byte[] buffer = new byte[bufferSize];
        bool destinationCreated = false;

        try
        {
            using var source = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                FileOptions.SequentialScan);

            using var destination = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                FileOptions.SequentialScan);

            destinationCreated = true;

            int bytesRead;

            while ((bytesRead = source.Read(
                buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);

                record.BytesCopied += bytesRead;

                progress?.Report(new VideoCopyRecord
                {
                    OriginalFileName = record.OriginalFileName,
                    OriginalPath = record.OriginalPath,
                    NewFileName = record.NewFileName,
                    DestinationPath = record.DestinationPath,
                    BytesCopied = record.BytesCopied,
                    TotalBytes = record.TotalBytes,
                    IsCompleted = false
                });
            }

            destination.Flush();
        }
        catch
        {
            if (destinationCreated)
            {
                try
                {
                    File.Delete(destinationPath);
                }
                catch
                {
                    // تعذر حذف الملف الجزئي.
                }
            }

            throw;
        }
    }
}
