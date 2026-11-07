
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RandomVideoDistributor.Models;

namespace RandomVideoDistributor.Services;

public class VideoReportService
{
    public string SaveCsvReport(VideoDistributionResult result)
    {
        string reportPath = Path.Combine(
            result.RunFolderPath,
            "Distribution_Report.csv");

        var csv = new StringBuilder();

        // UTF-8 BOM helps Excel recognize Arabic text correctly.
        csv.Append('\uFEFF');

        csv.AppendLine(string.Join(",",
            "Original File Name",
            "Original Path",
            "New File Name",
            "Destination Path",
            "Status",
            "Error Message"));

        foreach (var record in result.Records)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(record.OriginalFileName),
                EscapeCsv(record.OriginalPath),
                EscapeCsv(record.NewFileName),
                EscapeCsv(record.DestinationPath),
                EscapeCsv(record.IsSuccessful ? "Success" : "Failed"),
                EscapeCsv(record.ErrorMessage ?? string.Empty)));
        }

        File.WriteAllText(
            reportPath,
            csv.ToString(),
            new UTF8Encoding(false));

        return reportPath;
    }

    private static string EscapeCsv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
