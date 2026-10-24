using Jellyfin.Plugin.YoutubeMetadata.Providers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using Xunit;

namespace Jellyfin.Plugin.YoutubeMetadata.Tests
{
    public class LocalMetadataProviderTests
    {
        [Theory]
        [InlineData(2, true)]
        [InlineData(-2, false)]
        [InlineData(0.5, false)]
        public void HasChanged_UsesInfoFileTimestampRelativeToSavedMetadata(double minutesAfterSave, bool expected)
        {
            var fileSystem = new Mock<IFileSystem>();
            var mediaPath = Path.Combine("synthetic", "video.mkv");
            var directoryPath = Path.GetDirectoryName(mediaPath)!;
            var infoPath = Path.Combine(directoryPath, "video.info.json");
            var mediaFile = new FileSystemMetadata { Exists = true, IsDirectory = false };
            var directory = new FileSystemMetadata { Exists = true, IsDirectory = true, FullName = directoryPath };
            var infoFile = new FileSystemMetadata { Exists = true, IsDirectory = false };
            var savedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var infoWrittenAt = savedAt.AddMinutes(minutesAfterSave);

            fileSystem.Setup(fs => fs.GetFileSystemInfo(mediaPath)).Returns(mediaFile);
            fileSystem.Setup(fs => fs.GetDirectoryInfo(directoryPath)).Returns(directory);
            fileSystem.Setup(fs => fs.GetFileInfo(infoPath)).Returns(infoFile);
            fileSystem.Setup(fs => fs.GetLastWriteTimeUtc(infoFile)).Returns(infoWrittenAt);

            var item = new Movie { Path = mediaPath, DateLastSaved = savedAt };
            var provider = new YoutubeLocalMovieProvider(
                fileSystem.Object,
                Mock.Of<ILogger<YoutubeLocalMovieProvider>>());

            Assert.Equal(expected, provider.HasChanged(item, null));
        }
    }
}
