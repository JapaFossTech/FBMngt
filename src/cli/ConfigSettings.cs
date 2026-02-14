using Microsoft.Extensions.Configuration;

namespace FBMngt;

public interface IConnectionString
{
    string MLB_ConnString { get; }
}
public interface IAppSettings: IConnectionString
{
    string FanPros_RelativePath { get; }
    string ImportedFilesPath { get; }
    string ProjectionPath { get; }
    string ReportPath { get; }
    int SeasonYear { get; }
    string Yahoo_ClientId { get; }
    string Yahoo_ClientSecret { get; }
    string Yahoo_RedirectUri { get; }
    string Yahoo_RefreshToken { get; }
}

public class AppSettings : IAppSettings
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

    public string Yahoo_ClientId =>
        Program.Configuration["YahooOAuth:ClientId"]
        ?? throw new Exception("Missing config YahooOAuth:ClientId");
    public string Yahoo_ClientSecret =>
        Program.Configuration["YahooOAuth:ClientSecret"]
        ?? throw new Exception("Missing config YahooOAuth:ClientSecret");
    public string Yahoo_RefreshToken =>
        Program.Configuration["YahooOAuth:RefreshToken"]
        ?? throw new Exception("Missing config YahooOAuth:RefreshToken");
    public string Yahoo_RedirectUri =>
        Program.Configuration["YahooOAuth:RedirectUri"]
        ?? throw new Exception("Missing config YahooOAuth:RedirectUri");

    public string MLB_ConnString => 
        Program.Configuration.GetConnectionString("MLB")
            ?? throw new Exception("Missing connection string 'MLB'");
}
public static class RepoPath
{
    public static string Root
        => Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory,
                                       "..", "..", "..", "..", ".."));
}

public class ConfigSettings: IConnectionString
{
    public IAppSettings AppSettings;

    public ConfigSettings(IAppSettings appSettings)
    {
        AppSettings = appSettings;
    }
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

    public string MLB_ConnString => AppSettings.MLB_ConnString;
}
