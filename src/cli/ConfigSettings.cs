namespace FBMngt;

public static class AppSettings
{
    public static int SeasonYear =>
        DateTime.Now.Year;
    public static string ProjectionPath =>
        Program.Configuration["Paths:Projections"]
        ?? throw new Exception("Missing config Paths:Projections");

    public static string ReportPath =>
        Program.Configuration["Paths:Reports"]
        ?? throw new Exception("Missing config Paths:Reports");

    public static string ImportedFilesPath =>
        Program.Configuration["Paths:ImportedFiles"]
        ?? throw new Exception("Missing config Paths:ImportedFiles");
    public static string FanProsPath =>
        Program.Configuration["Paths:FanPros"]
        ?? throw new Exception("Missing config Paths:FanPros");

}
public interface IConfigSettings
{
    string ReportPath { get; }
    string FanPros_Rankings_Filepath { get; }
}
public class ConfigSettings: IConfigSettings
{
    public string ReportPath => AppSettings.ReportPath;
    public string FanPros_Rankings_Filepath
    {
        get
        {
            var fanProsCsvFile = Path.Combine(
            AppSettings.FanProsPath,
            $"FantasyPros_{AppSettings.SeasonYear}_Draft_ALL_Rankings.csv");

            return fanProsCsvFile;
        }
    }

}
