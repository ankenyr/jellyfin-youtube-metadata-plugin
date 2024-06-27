using System.Text;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.YoutubeMetadata.Providers;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.YoutubeMetadata.Tests.Providers;

[TestClass]
public class YoutubeLocalSeriesProviderTest
{
    private ILogger<YoutubeLocalSeriesProvider>? _logger;
    private IDirectoryService? _directoryService;

    [TestInitialize]
    public void Initialize()
    {
        _logger = Mock.Of<ILogger<YoutubeLocalSeriesProvider>>();
        _directoryService = Mock.Of<IDirectoryService>();
    }

    [TestMethod]
    public async Task GetMetadata_ReturnsMetadataResultWithSeries()
    {
        // Arrange
        var provider = new YoutubeLocalSeriesProvider(_logger!);
        var info = new ItemInfo(new Series());
        var cancellationToken = CancellationToken.None;
        var json = "{\"id\":\"123\",\"uploader\":\"Uploader\",\"upload_date\":\"20210101\",\"title\":\"Title\",\"description\":\"Description\",\"channel_id\":\"456\"}";

        Mock.Get(_directoryService!)
            .Setup(d => d.GetFile(It.IsAny<string>()))
            .Returns(new FileSystemMetadata { FullName = "info.json" });

        // Act
        var result = await provider.GetMetadata(info, _directoryService!, (_) => new MemoryStream(Encoding.UTF8.GetBytes(json)), cancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasMetadata);
        Assert.IsNotNull(result.Item);
        Assert.IsNotNull(result.People);
        Assert.IsTrue(result.People.Count == 1);
        Assert.AreEqual(result.People[0].Name, "Uploader");
        Assert.AreEqual(result.People[0].Type, PersonKind.Creator);
        Assert.AreEqual("123", result.Item.ProviderIds[PluginConstants.PluginName]);
        Assert.AreEqual("Uploader", result.Item.Name);
        Assert.AreEqual("Description", result.Item.Overview);
    }

    [TestMethod]
    public async Task GetMetadata_ThrowsExceptionWhenInfoJsonFileNotFound()
    {
        // Arrange
        var provider = new YoutubeLocalSeriesProvider(_logger!);
        var info = new ItemInfo(new Series());
        var cancellationToken = CancellationToken.None;

        Mock.Get(_directoryService!)
            .Setup(d => d.GetFile(It.IsAny<string>()))
            .Returns(value: null);

        // Act
        var exception = await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => provider.GetMetadata(info, _directoryService!, (_) => throw new FileNotFoundException(), cancellationToken));

        // Assert
        Assert.AreEqual("info.json file not found", exception.Message);
    }
}
