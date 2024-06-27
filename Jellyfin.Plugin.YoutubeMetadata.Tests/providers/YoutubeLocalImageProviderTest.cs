using Moq;
using Jellyfin.Plugin.YoutubeMetadata.Providers;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.YoutubeMetadata.Tests.Providers
{
    [TestClass]
    public class YoutubeLocalImageProviderTest
    {
        [TestMethod]
        public void GetImages_ShouldReturnLocalImages()
        {
            // Arrange
            var provider = new YoutubeLocalImageProvider();
            var item = new Episode { Path = "/path/to/video.mp4" };
            var directoryServiceMock = new Mock<IDirectoryService>();
            directoryServiceMock.Setup(ds => ds.GetFile("/path/to/video.jpg")).Returns(new FileSystemMetadata { FullName = "/path/to/video.jpg" });
            directoryServiceMock.Setup(ds => ds.GetFile("/path/to/video.webp")).Returns(new FileSystemMetadata { FullName = "/path/to/video.webp" });

            // Act
            var result = provider.GetImages(item, directoryServiceMock.Object);

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(image => image.FileInfo.FullName == "/path/to/video.jpg"));
            Assert.IsTrue(result.Any(image => image.FileInfo.FullName == "/path/to/video.webp"));
        }

        [TestMethod]
        public void GetImages_ShouldNotReturnMissingImages()
        {
            // Arrange
            var provider = new YoutubeLocalImageProvider();
            var item = new Episode { Path = "/path/to/video.mp4" };
            var directoryServiceMock = new Mock<IDirectoryService>();
            directoryServiceMock.Setup(ds => ds.GetFile("/path/to/video.jpg")).Returns(value: null);
            directoryServiceMock.Setup(ds => ds.GetFile("/path/to/video.webp")).Returns(new FileSystemMetadata { FullName = "/path/to/video.webp" });

            // Act
            var result = provider.GetImages(item, directoryServiceMock.Object);

            // Assert
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Any(image => image.FileInfo.FullName == "/path/to/video.webp"));
        }

        [TestMethod]
        public void Supports_ShouldReturnTrueForEpisode()
        {
            // Arrange
            var provider = new YoutubeLocalImageProvider();
            var item = new Episode();

            // Act
            var result = provider.Supports(item);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Supports_ShouldReturnFalseForNonEpisode()
        {
            // Arrange
            var provider = new YoutubeLocalImageProvider();
            var item = new Movie();

            // Act
            var result = provider.Supports(item);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
