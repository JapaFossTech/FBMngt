namespace FBMngt.Services.Importing;

public enum ImportNormalizationMode
{
    ResolveOnly,
    NormalizeAndResolve
}

/// <summary>
/// "Resolver" implies normalization, selection, deterministic
/// </summary>
public sealed class ImportFileResolver
{
    /// <summary>
    /// NormalizeAndResolve must ONLY be used by CLI entry points.
    /// Libraries and readers must use ResolveOnly.
    /// </summary>
    public string ResolveNewestFilePath(string inputPath,
                                        ImportNormalizationMode mode)
    {
        // Validate input
        if (inputPath.IsNullOrEmpty())
            throw new ArgumentException("Input path must be provided",
                                        nameof(inputPath));

        bool isResolved = TryResolveDescriptor(
                            inputPath, out ImportFileDescriptor descriptor);
        if (!isResolved)
            throw new ArgumentException(
                $"Could not resolve directory and filename from '{inputPath}'");

        var directoryInfo = new DirectoryInfo(descriptor.Directory);

        if (!directoryInfo.Exists)
            throw new DirectoryNotFoundException(descriptor.Directory);

        EnsureNotBinDirectory(directoryInfo.FullName);

        //Get all files (canonical + non-canonical)
        FileInfo[] files = directoryInfo.GetFiles(
                                            descriptor.SearchPattern,
                                            SearchOption.TopDirectoryOnly);

        //Nothing to work with, halt!
        if (files.Length == 0)
            throw new FileNotFoundException(
                $@"No files found matching '{descriptor.SearchPattern}' 
                    in '{descriptor.Directory}'");

        if (mode == ImportNormalizationMode.NormalizeAndResolve)
        {
            // Step 1: archive canonical file if present
            ArchiveCanonicalFiles(files, descriptor);

            // Step 2: re-evaluate files after archival
            files =
                directoryInfo.GetFiles(
                    descriptor.SearchPattern,
                    SearchOption.TopDirectoryOnly);
        }

        // Step 3: select and return newest
        return files
            .OrderByDescending(f => f.LastWriteTime)
            .First()
            .FullName;
    }

    private static bool TryResolveDescriptor(string inputPath,
                                        out ImportFileDescriptor descriptor)
    {
        descriptor = default!;

        string? directory = Path.GetDirectoryName(inputPath);
        string? fileName = Path.GetFileName(inputPath);

        if (directory.IsNullOrEmpty()
            || fileName.IsNullOrEmpty())
            return false;

        descriptor = new ImportFileDescriptor
                    {
                        Directory = directory!,
                        CanonicalFileName = fileName,
                        SearchPattern = "*.csv"
                    };

        return true;
    }
    private static void ArchiveCanonicalFiles(FileInfo[] files,
                                              ImportFileDescriptor descriptor)
    {
        // Find file to normalize
        FileInfo? canonical =
            files.FirstOrDefault(f =>
                f.Name.Equals(
                    descriptor.CanonicalFileName,
                    AppConst.IGNORE_CASE));

        if (canonical is null)
            return;             //nothing to normalize

        //Append _yyyyMMdd at the end of the filename
        string dateSuffix =
            canonical.LastWriteTime.ToString("yyyyMMdd");

        string archivedName =
            Path.GetFileNameWithoutExtension(canonical.Name) +
            "_" + dateSuffix +
            canonical.Extension;

        string archivedPath =
            Path.Combine(
                canonical.Directory!.FullName,
                archivedName);

        // Overwrite same-day archive if it exists
        if (File.Exists(archivedPath))
            File.Delete(archivedPath);

        canonical.MoveTo(archivedPath);
    }
    private static void EnsureNotBinDirectory(string fullPath)
    {
        if (fullPath.Contains(
                Path.DirectorySeparatorChar + "bin" 
                                            + Path.DirectorySeparatorChar,
                AppConst.IGNORE_CASE))
        {
            throw new InvalidOperationException(
                @"File normalization must not run against build output 
                directories.");
        }
    }

}
