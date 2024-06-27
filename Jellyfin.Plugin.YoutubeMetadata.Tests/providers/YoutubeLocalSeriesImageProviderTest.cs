using Moq;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Jellyfin.Plugin.YoutubeMetadata.Providers;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.YoutubeMetadata.Tests.Providers
{
    [TestClass]
    public class YoutubeLocalSeriesImageProviderTest
    {
        [TestMethod]
        public void GetImages_ReturnsLocalImages()
        {
            // Arrange
            var provider = new YoutubeLocalSeriesImageProvider();
            var series = new Series { Path = "/path/to/series" };
            var directoryServiceMock = new Mock<IDirectoryService>();
            directoryServiceMock.Setup(ds => ds.GetFile("/path/to/series/series.jpg")).Returns(new FileSystemMetadata { FullName = "/path/to/series/series.jpg" });
            directoryServiceMock.Setup(ds => ds.GetFile("/path/to/series/series.webp")).Returns(new FileSystemMetadata { FullName = "/path/to/series/series.webp" });

            // Act
            var result = provider.GetImages(series, directoryServiceMock.Object);

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(image => image.FileInfo.FullName == "/path/to/series/series.jpg"));
            Assert.IsTrue(result.Any(image => image.FileInfo.FullName == "/path/to/series/series.webp"));
        }

        [TestMethod]
        public void Supports_ReturnsTrueForSeries()
        {
            // Arrange
            var provider = new YoutubeLocalSeriesImageProvider();
            var series = new Series();

            // Act
            var result = provider.Supports(series);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Supports_ReturnsFalseForNonSeries()
        {
            // Arrange
            var provider = new YoutubeLocalSeriesImageProvider();
            var movie = new Movie();

            // Act
            var result = provider.Supports(movie);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
