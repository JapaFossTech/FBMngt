namespace FBMngt.Services.Importing;

public class ImportFileDescriptor
{
    public required string Directory { get; init; }
    public required string CanonicalFileName { get; init; }
    public required string SearchPattern { get; init; }
}

