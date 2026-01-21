namespace FBMngt.Services.Reporting;

public interface IReportPathProvider
{
    string ReportPath { get; }
}

public class AppContextReportPathProvider : 
                                IReportPathProvider
{
    public string ReportPath => AppContext.ReportPath;
}