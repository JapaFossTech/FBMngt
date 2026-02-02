using FBMngt.Services.Importing;
using NUnit.Framework;

namespace FBMngt.Tests.Services.Importing;

[TestFixture]
public class ImportFileResolverTests
{
    private string _tempDir = default!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateFile(
        string fileName,
        DateTime lastWriteTime)
    {
        string path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, "test");
        File.SetLastWriteTime(path, lastWriteTime);
        return path;
    }

    [Test]
    public void
    ResolveNewestFilePath_WhenInputIsNull_Throws()
    {
        // Arrange
        var resolver = new ImportFileResolver();

        // Act / Assert
        Assert.Throws<ArgumentException>(() =>
            resolver.ResolveNewestFilePath(
                null!,
                ImportNormalizationMode.NormalizeAndResolve));
    }

    [Test]
    public void
    ResolveNewestFilePath_WhenDirectoryDoesNotExist_Throws()
    {
        // Arrange
        var resolver = new ImportFileResolver();

        string path = Path.Combine(
            _tempDir,
            "missing",
            "FanPros.csv");

        // Act / Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            resolver.ResolveNewestFilePath(
                path,
                ImportNormalizationMode.NormalizeAndResolve));
    }

    [Test]
    public void
    ResolveNewestFilePath_WhenNoFilesExist_Throws()
    {
        // Arrange
        var resolver = new ImportFileResolver();
        string path = Path.Combine(_tempDir, "FanPros.csv");

        // Act / Assert
        Assert.Throws<FileNotFoundException>(() =>
            resolver.ResolveNewestFilePath(
                path,
                ImportNormalizationMode.NormalizeAndResolve));
    }

    [Test]
    public void
    ResolveNewestFilePath_WhenOnlyCanonicalFileExists_ReturnsArchived()
    {
        // Arrange
        var resolver = new ImportFileResolver();

        CreateFile(
            "FanPros.csv",
            DateTime.Today.AddHours(10));

        string path = Path.Combine(_tempDir, "FanPros.csv");

        // Act
        string resolved =
            resolver.ResolveNewestFilePath(
                path,
                ImportNormalizationMode.NormalizeAndResolve);

        // Assert
        Assert.That(
            Path.GetFileName(resolved),
            Is.EqualTo("FanPros_"
                + DateTime.Today.ToString("yyyyMMdd")
                + ".csv"));
    }

    [Test]
    public void
    ResolveNewestFilePath_WhenMultipleFilesExist_ReturnsNewest()
    {
        // Arrange
        var resolver = new ImportFileResolver();

        CreateFile("FanPros_20240101.csv", new DateTime(2024, 1, 1));
        CreateFile("FanPros_20240110.csv", new DateTime(2024, 1, 10));
        CreateFile("FanPros.csv", new DateTime(2024, 1, 15));

        string path = Path.Combine(_tempDir, "FanPros.csv");

        // Act
        string resolved =
            resolver.ResolveNewestFilePath(
                path,
                ImportNormalizationMode.NormalizeAndResolve);

        // Assert
        Assert.That(
            Path.GetFileName(resolved),
            Is.EqualTo("FanPros_20240115.csv"));
    }

    [Test]
    public void
    ResolveNewestFilePath_WhenCanonicalAlreadyArchived_OverwritesSameDay()
    {
        // Arrange
        var resolver = new ImportFileResolver();

        CreateFile("FanPros_20240115.csv", new DateTime(2024, 1, 15));
        CreateFile("FanPros.csv", new DateTime(2024, 1, 15));

        string path = Path.Combine(_tempDir, "FanPros.csv");

        // Act
        string resolved =
            resolver.ResolveNewestFilePath(
                path,
                ImportNormalizationMode.NormalizeAndResolve);

        // Assert
        Assert.That(
            Path.GetFileName(resolved),
            Is.EqualTo("FanPros_20240115.csv"));

        Assert.That(
            File.Exists(Path.Combine(_tempDir, "FanPros.csv")),
            Is.False);
    }
}
