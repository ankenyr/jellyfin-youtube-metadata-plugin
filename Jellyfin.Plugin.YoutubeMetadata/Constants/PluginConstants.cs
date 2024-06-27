using System;

namespace Jellyfin.Plugin.YoutubeMetadata
{
    /// <summary>
    /// Contains constants used by the plugin.
    /// </summary>
    public sealed partial class PluginConstants
    {
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        public const string PluginName = "YoutubeMetadata";

        /// <summary>
        /// The unique identifier of the plugin.
        /// </summary>
        public static readonly Guid PluginGuid = Guid.Parse("4c748daa-a7e4-4ed1-817c-5e18c683585e");
    }
}
