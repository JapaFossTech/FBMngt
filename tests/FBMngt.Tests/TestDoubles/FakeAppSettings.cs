namespace FBMngt.Tests.TestDoubles;

public class FakeAppSettings : IAppSettings
{
    private readonly ConfigSettings _configSettings;
    public string ReportPath { get; set; }

    public string FanPros_Rankings_InputCsv_Path
        => _configSettings.FanPros_Rankings_InputCsv_Path;

    public string FanPros_RelativePath { get; set; }

    public string ImportedFilesPath { get; set; }

    public string ProjectionPath { get; set; }

    public int SeasonYear => DateTime.Now.Year;

    public FakeAppSettings()
    {
        _configSettings = new ConfigSettings(this);

        ReportPath = "C:\\Users\\Master2022\\Documents\\"
                    +"Javier\\FantasyBaseball\\Logs\\Test"; ;
        FanPros_RelativePath = "FanPros";
        ImportedFilesPath = "C:\\Users\\Master2022\\Documents"
                +"\\Javier\\FantasyBaseball\\ImportedFiles";
        ProjectionPath = "C:\\Users\\Master2022\\Documents"
                +"\\Javier\\FantasyBaseball\\ImportedFiles"
                +"\\Projections";
    }
}
