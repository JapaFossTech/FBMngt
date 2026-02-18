using FBMngt.Models;
using FBMngt.Services.Players;

namespace FBMngt.Services.Reporting;

// Base pipeline
public abstract class ReportBase<TInput, TReportRow>
    where TInput : IPlayer
{
    private readonly PlayerResolver _playerResolver;

    //Ctor
    protected ReportBase(PlayerResolver playerResolver)
    {
        _playerResolver = playerResolver;
    }
    public async virtual Task<List<TReportRow>> 
                              GenerateAsync(int rows = 0)
    {
        Console.WriteLine($"ReportBase: Rows to read:: {rows}");
        
        // 1 Resolve File Path
        string filePath =  ResolveFilePath();

        List<TInput> input = default!;
        if (filePath.HasString())
        {
            // 1️.1 Read
            input = await ReadAsync(rows, filePath);
        }
        else
            throw new Exception("Full file path is not provided. Is empty");

        // 2️ Resolve PlayerIDs
        await _playerResolver.ResolvePlayerIDAsync(
                                input.Cast<IPlayer>().ToList());

        // 3️ Transform
        return await TransformAsync(input);
    }

    public async virtual Task<ReportResult<TReportRow>> 
        GenerateAndWriteAsync(int rows = 0)
    {
        // 1,2 and 3: Get Data with PalyerID resolved
        // and data transformed
        List<TReportRow> reportRows = await GenerateAsync(rows);

        // 4️ Format
        List<string> lines =
            FormatReport(reportRows);

        // 5️ Persist
        await WriteAsync(lines);

        return new ReportResult<TReportRow>
        {
            ReportRows = reportRows,
            StringLines = lines
        };
    }

    #region Helper Functionality
    protected virtual string ResolveFilePath() => string.Empty;
    protected abstract Task<List<TInput>> ReadAsync(
                                int rows, string filePath);
    protected virtual Task<List<TReportRow>> TransformAsync(List<TInput> input)
    {
        if (typeof(TReportRow) == typeof(TInput))
        {
            return Task.FromResult(
                input.Cast<TReportRow>().ToList());
        }

        throw new NotSupportedException(
            $"{GetType().Name} must override TransformAsync because " +
            $"{typeof(TInput).Name} ≠ {typeof(TReportRow).Name}");
    }
    protected abstract List<string> FormatReport(List<TReportRow> rows);
    protected abstract Task WriteAsync(List<string> lines);

    #endregion
}
