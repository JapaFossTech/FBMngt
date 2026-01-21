namespace FBMngt;

public static class AppContext
{
    /// <summary>
    /// Fantasy baseball season year.
    /// For now: current calendar year.
    /// Easy to override later.
    /// </summary>
    public static int SeasonYear =>
        DateTime.Now.Year;

    public static string ProjectionPath
    {
        get
        {
            var raw =
                Program.Configuration["Paths:Projections"]
                ?? throw new Exception(
                            "Missing config Paths:Projections");

            return raw.Replace(
                AppConst.YEAR_TOKEN,
                SeasonYear.ToString());
        }
    }

    public static string ReportPath =>
        Program.Configuration["Paths:Reports"]
        ?? throw new Exception("Missing config Paths:Reports");

    public static string ImportedFilesPath =>
        Program.Configuration["Paths:ImportedFiles"]
        ?? throw new Exception("Missing config Paths:ImportedFiles");
}
