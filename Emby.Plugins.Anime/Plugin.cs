using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Emby.Plugins.Anime.Configuration;
using Emby.Plugins.Anime.Providers.AniDB.Identity;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Drawing;
using System.IO;

namespace Emby.Plugins.Anime
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger logger) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            AniDbTitleMatcher.DefaultInstance = new AniDbTitleMatcher(logger, new AniDbTitleDownloader(logger, applicationPaths));
        }

        public override string Name
        {
            get { return "AnimeX"; }
        }

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "animex",
                    EmbeddedResourcePath = "Emby.Plugins.Anime.Configuration.configPage.html"
                }
            };
        }

        private Guid _id = new Guid("33657da1-93d0-4b2a-83b8-62ec40671513");

        public override Guid Id
        {
            get { return _id; }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }
    }
}