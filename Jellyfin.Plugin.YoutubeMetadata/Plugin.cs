using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using Jellyfin.Plugin.YoutubeMetadata.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.YoutubeMetadata
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "YouTube Metadata";

        public override Guid Id => Guid.Parse("b4b4353e-dc57-4398-82c1-de9079e7146a");
        IHttpClientFactory _httpClientFactory;
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IHttpClientFactory httpClientFactory) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _httpClientFactory = httpClientFactory;
        }

        public static Plugin Instance { get; private set; }
        public HttpClient GetHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("YTMetadata", Version.ToString()));

            return httpClient;
        }
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = string.Format("{0}.Configuration.configPage.html", GetType().Namespace)
                }
            };
        }
    }

    /// <summary>
    /// Register webhook services.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<System.IO.Abstractions.IFileSystem, FileSystem>();
        }
    }
}
