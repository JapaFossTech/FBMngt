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
        var resolver = new ImportFileResolver();

        Assert.Throws<ArgumentException>(() =>
            resolver.ResolveNewestFilePath(null!));
    }

    [Test]
    public void 
    ResolveNewestFilePath_WhenDirectoryDoesNotExist_Throws()
    {
        var resolver = new ImportFileResolver();

        string path = Path.Combine(
            _tempDir,
            "missing",
            "FanPros.csv");

        Assert.Throws<DirectoryNotFoundException>(() =>
            resolver.ResolveNewestFilePath(path));
    }

    [Test]
    public void 
    ResolveNewestFilePath_WhenNoFilesExist_Throws()
    {
        var resolver = new ImportFileResolver();

        string path = Path.Combine(
            _tempDir,
            "FanPros.csv");

        Assert.Throws<FileNotFoundException>(() =>
            resolver.ResolveNewestFilePath(path));
    }

    [Test]
    public void 
    ResolveNewestFilePath_WhenOnlyCanonicalFileExists_ReturnsIt()
    {
        var resolver = new ImportFileResolver();

        CreateFile(
            "FanPros.csv",
            DateTime.Today.AddHours(10));

        string path = Path.Combine(_tempDir, "FanPros.csv");

        string resolved = resolver.ResolveNewestFilePath(path);

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
        var resolver = new ImportFileResolver();

        CreateFile(
            "FanPros_20240101.csv",
            new DateTime(2024, 1, 1));

        CreateFile(
            "FanPros_20240110.csv",
            new DateTime(2024, 1, 10));

        CreateFile(
            "FanPros.csv",
            new DateTime(2024, 1, 15));

        string path = Path.Combine(_tempDir, "FanPros.csv");

        string resolved = resolver.ResolveNewestFilePath(path);

        Assert.That(
            Path.GetFileName(resolved),
            Is.EqualTo("FanPros_20240115.csv"));
    }

    [Test]
    public void 
    ResolveNewestFilePath_WhenCanonicalAlreadyArchived_DoesNotOverwrite()
    {
        //Arrange
        var resolver = new ImportFileResolver();

        CreateFile(
            "FanPros_20240115.csv",
            new DateTime(2024, 1, 15));

        CreateFile(
            "FanPros.csv",
            new DateTime(2024, 1, 15)); // same day

        string path = Path.Combine(_tempDir, "FanPros.csv");

        //Act
        string resolved = resolver.ResolveNewestFilePath(path);

        //Assert
        // Should return the already-existing archived file
        Assert.That(
            Path.GetFileName(resolved),
            Is.EqualTo("FanPros_20240115.csv"));

        Assert.That(
            File.Exists(Path.Combine(
                _tempDir,
                "FanPros.csv")),
            Is.False,
            "Canonical file should not remain after normalization");
    }
}
