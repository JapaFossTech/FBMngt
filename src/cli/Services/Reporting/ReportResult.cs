namespace FBMngt.Services.Reporting;

// Model output
public sealed class ReportResult<TReportRow>
{
    private string? _allTabsReportLine;
    public required List<TReportRow> ReportRows { get; init; }
    public required List<string> StringLines { get; init; }

    public string AllTabsReportLine
    {
        get
        {
            if (_allTabsReportLine is not null)
                return _allTabsReportLine;

            if (StringLines.Count == 0)
                return string.Empty;

            string headers = StringLines[0];

            int tabCount = headers.Count(c => c == '\t');

            _allTabsReportLine = new string('\t', tabCount);

            return _allTabsReportLine;
        }
    }
}
