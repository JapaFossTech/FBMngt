using FBMngt.Services.Reporting;

namespace FBMngt.Tests.TestDoubles;

public class FakeReportPathProvider : IReportPathProvider
{
    public string ReportPath { get; }

    public FakeReportPathProvider(string reportPath)
    {
        ReportPath = reportPath;
    }
}
