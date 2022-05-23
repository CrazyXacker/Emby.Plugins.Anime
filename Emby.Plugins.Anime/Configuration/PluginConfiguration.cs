using MediaBrowser.Model.Plugins;

namespace Emby.Plugins.Anime.Configuration
{
    
    public enum TitlePreferenceType
    {
        /// <summary>
        /// Use titles in the local metadata language.
        /// </summary>
        Localized,

        /// <summary>
        /// Use titles in Japanese.
        /// </summary>
        Japanese,

        /// <summary>
        /// Use titles in Japanese romaji.
        /// </summary>
        JapaneseRomaji,
        
        /// <summary>
        /// Use titles in Korean.
        /// </summary>
        Korean,

        /// <summary>
        /// Use titles in German.
        /// </summary>
        German,
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        public TitlePreferenceType TitlePreference { get; set; }
        public bool TidyGenreList { get; set; }
        public int MaxGenres { get; set; }
        public bool AddAnimeGenre { get; set; }
        public string MyAnimeList_API_Name { get; set; }
        public string MyAnimeList_API_Pw { get; set; }
        public int AniDB_wait_time { get; set; }
        public PluginConfiguration()
        {
            TitlePreference = TitlePreferenceType.Localized;
            TidyGenreList = true;
            MaxGenres = 5;
            AddAnimeGenre = true;
            MyAnimeList_API_Name = "";
            MyAnimeList_API_Pw = "";
            AniDB_wait_time = 0;
        }
    }
}