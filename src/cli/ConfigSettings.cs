namespace FBMngt;

public interface IAppSettings
{
    string FanPros_RelativePath { get; }
    string ImportedFilesPath { get; }
    string ProjectionPath { get; }
    string ReportPath { get; }
    int SeasonYear { get; }
}

public class AppSettings: IAppSettings
{
    public int SeasonYear => DateTime.Now.Year;
    public string ProjectionPath =>
        Program.Configuration["Paths:Projections"]
        ?? throw new Exception("Missing config Paths:Projections");

    public string ReportPath =>
        Program.Configuration["Paths:Reports"]
        ?? throw new Exception("Missing config Paths:Reports");

    public string ImportedFilesPath =>
        Program.Configuration["Paths:ImportedFiles"]
        ?? throw new Exception("Missing config Paths:ImportedFiles");
    public string FanPros_RelativePath =>
        Program.Configuration["Paths:FanPros"]
        ?? throw new Exception("Missing config Paths:FanPros");

}
public static class RepoPath
{
    public static string Root
        => Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory,
                                       "..", "..", "..", "..", ".."));
}

public class ConfigSettings
{
    public IAppSettings AppSettings;

    public ConfigSettings(IAppSettings appSettings)
    {
        AppSettings = appSettings;
    }
    //public string ReportPath => AppSettings.ReportPath;
    public string FanPros_Rankings_InputCsv_Path
    {
        get
        {
            var fanProsCsvFile = Path.Combine(
                RepoPath.Root,
                "rawData",
                AppSettings.FanPros_RelativePath,
                $"FantasyPros_{AppSettings.SeasonYear}_Draft"
                    +"_ALL_Rankings.csv");

            return fanProsCsvFile;
        }
    }
    public string FanPros_OutReport_Path
    {
        get
        {
            var fanProsCsvFile = Path.Combine(
            AppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{AppSettings.SeasonYear}.tsv");

            return fanProsCsvFile;
        }
    }

}
