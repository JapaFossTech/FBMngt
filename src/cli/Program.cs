using FBMngt;
using FBMngt.Commands;
using FBMngt.Services.Importing;
using Microsoft.Extensions.Configuration;
using System.Text;

class Program
{
    
    public static IConfiguration Configuration { get; private set; } 
        = null!;

    static async Task Main(string[] args)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            //.AddJsonFile(
            //    $"appsettings.{
            //        Environment.GetEnvironmentVariable(
            //            "DOTNET_ENVIRONMENT")}.json",
            //    optional: true)
            .AddEnvironmentVariables()
            .Build();

        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var configSettings = new ConfigSettings(new AppSettings());
        var resolver = new ImportFileResolver();

        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "import":  //import --match-column PlayerName --show-player
                EnsureFanProsInputIsNormalized(resolver, configSettings);

                await ImportCommand
                        .ExecuteAsync(args.Skip(1).ToArray());
                break;
            case "report":
                EnsureFanProsInputIsNormalized(resolver, configSettings);

                await ReportCommand
                        .ExecuteAsync(args.Skip(1).ToArray());
                break;
            case "data-integrity":  //data-integrity players [--dry-run]
                await DataIntegrityCommand
                        .ExecuteAsync(args.Skip(1).ToArray());
                break;

            default:
                Console.WriteLine($"Unknown command: {command}");
                ShowHelp();
                break;
        }
    }

    static void EnsureFanProsInputIsNormalized(ImportFileResolver resolver,
                                        ConfigSettings config)
    {
        resolver.ResolveNewestFilePath(config.FanPros_Rankings_InputCsv_Path,
                                       ImportNormalizationMode.NormalizeAndResolve);
    }

    static void ShowHelp()
    {
        Console.WriteLine("FBMngt - Fantasy Baseball Management");
        Console.WriteLine("Commands:");
        Console.WriteLine("  import --match-column PlayerName");
        Console.WriteLine(
            @"  import --match-column PLAYER&nbsp;NAME --file-Type FanPros [--show-player] [--rows 150]");
        Console.WriteLine("  report --zscores");
        Console.WriteLine("  data-integrity players [--dry-run]");
        Console.WriteLine("  report --FanProsCoreFields [--rows 250]");
    }
}
