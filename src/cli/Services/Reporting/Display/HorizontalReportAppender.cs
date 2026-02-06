using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMngt.Services.Reporting.Display;

public interface IHorizontalReportAppender
{
    List<string> Append(
                    List<ReportResult<object>> reports);
}

public sealed class HorizontalReportAppender 
                                : IHorizontalReportAppender
{
    public List<string> Append(
                    List<ReportResult<object>> reportList)
    {
        var combinedLines = new List<string>();

        if (reportList.Count == 0)
        {
            return combinedLines;
        }

        int maxLineCount = reportList
            .Max(r => r.StringLines.Count);

        for (int lineIndex = 0; lineIndex < maxLineCount
                              ; lineIndex++)
        {
            var lineParts = new List<string>();

            foreach (var report in reportList)
            {
                if (lineIndex < report.StringLines.Count)
                {
                    lineParts.Add(
                                report.StringLines[lineIndex]);
                }
                else
                {
                    lineParts.Add(string.Empty);
                }
            }

            string combinedLine = string.Join("\t\t", lineParts);
            combinedLines.Add(combinedLine);
        }

        return combinedLines;
    }
}

