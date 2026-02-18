namespace FBMngt.Services.Reporting;
public interface IFileSelector
{
    string GetFilePath(string inputPath);
}
public interface IFileSelectorFactory
{
    IFileSelector CreateLatest();
    IFileSelector CreatePrevious();
}

public sealed class IndexedFileSelector: IFileSelector
{
    private int _fileIndex = 0;

    public IndexedFileSelector(int fileIndex)
    {
        _fileIndex = fileIndex;
    }

    public string GetFilePath(string inputPath)
    {
        string descriptorDirectory = 
                            Path.GetDirectoryName(inputPath)!;

        var directoryInfo = new DirectoryInfo(descriptorDirectory);

        FileInfo[] files = directoryInfo
            .GetFiles("*.csv", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.LastWriteTime)
            .ToArray();

        if (_fileIndex >= files.Length)
            throw new InvalidOperationException(
                $"Requested file index {_fileIndex} but " +
                $"only {files.Length} exist.");

        return files[_fileIndex].FullName;
    }
}
public sealed class FileSelectorFactory : IFileSelectorFactory
{
    public IFileSelector CreateLatest() =>
        new IndexedFileSelector(0);

    public IFileSelector CreatePrevious() =>
        new IndexedFileSelector(1);
}


