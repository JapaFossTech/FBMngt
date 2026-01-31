namespace FBMngt.Services.Reporting;

// Model output
public sealed class ReportResult<TReportRow>
{
    public required List<TReportRow> ReportRows { get; init; }
    public required List<string> StringLines { get; init; }
}
