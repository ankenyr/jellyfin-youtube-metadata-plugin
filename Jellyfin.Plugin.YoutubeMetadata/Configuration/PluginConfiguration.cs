using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.YoutubeMetadata.Configuration
{
    public enum SomeOptions
    {
        OneOption,
        AnotherOption
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        // store configurable settings your plugin might need
        public string apikey { get; set; }

        public PluginConfiguration()
        {
            // set default options here
            apikey = "string";
        }
    }
}
