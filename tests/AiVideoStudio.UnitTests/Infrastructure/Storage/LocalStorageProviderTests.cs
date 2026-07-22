using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Infrastructure.Storage;

public class LocalStorageProviderTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly LocalStorageProvider _provider;

    public LocalStorageProviderTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "aivideostudio_test_storage_" + Guid.NewGuid().ToString("N"));
        var options = Options.Create(new StorageOptions
        {
            BasePath = _testBasePath
        });
        _provider = new LocalStorageProvider(options);
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateDirectoryAndWriteFile()
    {
        // Arrange
        var bucket = "project_1";
        var key = "test_file.txt";
        var content = "Hello World Storage Test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var relativePath = await _provider.UploadAsync(bucket, key, stream, "text/plain");

        // Assert
        relativePath.Should().NotBeNullOrEmpty();
        var fullPath = Path.Combine(_testBasePath, bucket, key);
        File.Exists(fullPath).Should().BeTrue();
        var readText = await File.ReadAllTextAsync(fullPath);
        readText.Should().Be(content);
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnFileStream_WhenFileExists()
    {
        // Arrange
        var bucket = "project_2";
        var key = "download_file.txt";
        var content = "Download Content";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _provider.UploadAsync(bucket, key, uploadStream, "text/plain");

        // Act
        using var downloadStream = await _provider.DownloadAsync(bucket, key);
        using var reader = new StreamReader(downloadStream);
        var readText = await reader.ReadToEndAsync();

        // Assert
        readText.Should().Be(content);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFileFromStorage()
    {
        // Arrange
        var bucket = "project_3";
        var key = "delete_file.txt";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("To Delete"));
        await _provider.UploadAsync(bucket, key, uploadStream, "text/plain");

        (await _provider.ExistsAsync(bucket, key)).Should().BeTrue();

        // Act
        await _provider.DeleteAsync(bucket, key);

        // Assert
        (await _provider.ExistsAsync(bucket, key)).Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            try
            {
                Directory.Delete(_testBasePath, true);
            }
            catch
            {
            }
        }
    }
}
