namespace FBMngt.Tests.TestDoubles;

public class FakeConfigSettingsProvider : IConfigSettings
{
    public string ReportPath { get; }

    public string FanPros_Rankings_Filepath { get; }

    public FakeConfigSettingsProvider(string reportPath,
        string fanPros_Rankings_Filepath)
    {
        ReportPath = reportPath;
        FanPros_Rankings_Filepath = fanPros_Rankings_Filepath;
    }
}
