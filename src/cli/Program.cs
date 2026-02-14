using FBMngt;
using FBMngt.Commands;
using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Services.Importing;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Services.Reporting.PreDraftRanking;
using FBMngt.Services.Yahoo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    public static IConfiguration Configuration { get; private set; } 
        = null!;

    static async Task Main(string[] args)
    {
        #region Dependency Injection

        var services = new ServiceCollection();

        // config
        services.AddSingleton<IAppSettings, AppSettings>();
        services.AddSingleton<ConfigSettings>(sp =>
        {
            var appSettings = sp.GetRequiredService<IAppSettings>();
            return new ConfigSettings(appSettings);
        });

        // Repositories
        services.AddTransient<IPlayerRepository, PlayerRepository>();
        services.AddTransient<
            IPreDraftAdjustRepository, PreDraftAdjustRepository>();

        // commands
        services.AddTransient<ImportCommand>();
        services.AddTransient<DataIntegrityCommand>();
        services.AddTransient<YahooCommand>();
        services.AddTransient<ReportCommand>();
        services.AddTransient<PlayerOffsetCommand>();

        // services
        services.AddTransient<PlayerResolver>();
        services.AddTransient<PlayerImportService>();
        services.AddTransient<FanProsCsvReader>();
        services.AddTransient<ImportService>();
        services.AddTransient<YahooService>();
        services.AddTransient<ReportService>();
        services.AddTransient<PlayerOffsetService>();

        // reports
        services.AddTransient<FanProsCoreFieldsReport>();

        var serviceProvider = services.BuildServiceProvider();

        #endregion

        var environment = Environment.GetEnvironmentVariable(
                            "DOTNET_ENVIRONMENT") ?? "Development";
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json",
                         optional: true)
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
        var nonCommandArgs = args.Skip(1).ToArray();

        switch (command)
        {
            case "import":  //import --match-column PlayerName --show-player
                EnsureFanProsInputIsNormalized(resolver, configSettings);

                var importCommand = serviceProvider
                                .GetRequiredService<ImportCommand>();
                await importCommand.ExecuteAsync(nonCommandArgs);
                break;
            case "report":
                EnsureFanProsInputIsNormalized(resolver, configSettings);

                var reportCommand = serviceProvider
                                .GetRequiredService<ReportCommand>();
                await reportCommand.ExecuteAsync(nonCommandArgs);
                break;
            case "data-integrity":  //data-integrity players [--dry-run]
                var dataIntegrityCommand = serviceProvider
                           .GetRequiredService<DataIntegrityCommand>();
                await dataIntegrityCommand
                           .ExecuteAsync(args.Skip(1).ToArray());
                break;
            case "playeroffset":
                var offsetCommand = serviceProvider
                           .GetRequiredService<PlayerOffsetCommand>();
                await offsetCommand
                           .ExecuteAsync(args.Skip(1).ToArray());
                break;
            case "yahoo":
                var yahooCommand = serviceProvider
                                .GetRequiredService<YahooCommand>();
                await yahooCommand.ExecuteAsync(nonCommandArgs);
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                ShowHelp();
                break;
        }
    }

    static void EnsureFanProsInputIsNormalized(
                                        ImportFileResolver resolver,
                                        ConfigSettings config)
    {
        resolver.ResolveNewestFilePath(
                       config.FanPros_Rankings_InputCsv_Path,
                       ImportNormalizationMode.NormalizeAndResolve);
    }

    static void ShowHelp()
    {
        Console.WriteLine("FBMngt - Fantasy Baseball Management");
        Console.WriteLine("Commands:");
        Console.WriteLine("  import --match-column PlayerName");
        Console.WriteLine("  import --file-Type FanPros " +
            "--rows 2000");
        Console.WriteLine(
            @"  import --match-column ""PLAYER NAME"" --file-Type FanPros [--show-player] [--rows 150]");
        Console.WriteLine("  report --zscores");
        Console.WriteLine("  data-integrity players " +
            "[--dry-run]");
        Console.WriteLine("  report --FanProsCoreFields " +
            "[--rows 250]");
        Console.WriteLine(
            "  report --combine FanProsCoreFields,zscores");
        Console.WriteLine("  import --file-Type FanPros");
        Console.WriteLine(
            "  playerOffset --initialConfiguration "
            +"[--doCreateReport]");
        Console.WriteLine(
            "  playerOffset --adjust playerID,12|playerID2,-12 " +
            "[--doCreateReport]");
        Console.WriteLine("  yahoo [--showLoginUri] " +
            "[--getAccessToken]");
    }
}
