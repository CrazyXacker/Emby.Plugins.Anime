using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Emby.Plugins.Anime.Configuration;
using Emby.Plugins.Anime.Providers.AniDB.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Emby.Plugins.Anime.Providers.AniDB.Metadata
{
    public class AniDbSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
    {
        private const string SeriesDataFile = "series.xml";
        private const string SeriesQueryUrl = "http://api.anidb.net:9001/httpapi?request=anime&client={0}&clientver=1&protover=1&aid={1}";
        private const string ClientName = "mediabrowser";

        // AniDB has very low request rate limits, a minimum of 2 seconds between requests, and an average of 4 seconds between requests
        //public static readonly SemaphoreSlim ResourcePool = new SemaphoreSlim(1, 1);

        public static readonly RateLimiter RequestLimiter = new RateLimiter(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));
        private static readonly int[] IgnoredCategoryIds = { 6, 22, 23, 60, 128, 129, 185, 216, 242, 255, 268, 269, 289 };
        private static readonly Regex AniDbUrlRegex = new Regex(@"http://anidb.net/\w+ \[(?<name>[^\]]*)\]");
        private readonly IApplicationPaths _appPaths;
        private readonly IHttpClient _httpClient;

        private readonly Dictionary<string, PersonType> _typeMappings = new Dictionary<string, PersonType>
        {
            {"Direction", PersonType.Director},
            {"Music", PersonType.Composer},
            {"Chief Animation Direction", PersonType.Director},
            {"Series Composition", PersonType.Writer},
            {"Animation Work", PersonType.Producer},
            {"Original Work", PersonType.Writer},
            {"Character Design", PersonType.Writer},
            {"Work", PersonType.Producer},
            {"Animation Character Design", PersonType.Writer},
            {"Effects Direction", PersonType.Writer},
            {"Original Plan", PersonType.Writer},
            {"Chief Direction", PersonType.Director},
            {"Main Character Design", PersonType.Writer},
            {"Story Composition", PersonType.Writer},
            {"Magical Bushidou Musashi Design", PersonType.Writer}
        };

        private static readonly Dictionary<string, string> TagsToGenre = new Dictionary<string, string>
        {
            {"deflowering", "В первый раз"},
            {"Adventure", "Приключения"},
            {"erotic game", "Эро игра"},
            {"school life", "В школе"},
            {"high school", "Старшая школа"},
            {"rape", "Изнасилование"},
            {"small breasts", "Маленькая грудь"},
            {"medium breasts", "Средняя грудь"},
            {"large breasts", "Большая грудь"},
            {"huge breasts", "Огромная грудь"},
            {"gigantic breasts", "Гигантская грудь"},
            {"visual novel", "Виузальная новелла"},
            {"anal", "Анал"},
            {"nurse", "Медсестра"},
            {"action", "Боевик"},
            {"game", "Игра"},
            {"elf", "Эльфы"},
            {"incest", "Инцест"},
            {"loli", "Лоли"},
            {"BDSM", "БДСМ"},
            {"bondage", "Бондаж"},
            {"comedy", "Комедия"},
            {"manga", "Манга"},
            {"shota", "Шота"},
            {"female student", "Студентки"},
            {"harem", "Гарем"},
            {"demon", "Демоны"},
            {"fantasy", "Фэнтези"},
            {"idol", "Идол"},
            {"horror", "Ужасы"},
            {"tentacle", "Тентакли"},
            {"half-length episodes", "Короткометражка"},
            {"dementia", "Помешательство"},
            {"melodrama", "Драма"},
            {"ecchi", "Эччи"},
            {"18 restricted", "18+"},
            {"sex", "18+"},
            {"historical", "Исторический"},
            {"magic", "Магия"},
            {"martial arts", "Боевые искусства"},
            {"mecha", "Меха"},
            {"military", "Военные"},
            {"motorsport", "Мотоспорт"},
            {"mystery", "Мистика"},
            {"parody", "Комедия"},
            {"cops", "Полиция"},
            {"psychological", "Психология"},
            {"romance", "Романтика"},
            {"samurai", "Самураи"},
            {"school", "Школа"},
            {"science fiction", "Научная фантастика"},
            {"seinen", "Сэйнэн"},
            {"shoujo", "Сёдзё"},
            {"shoujo ai", "Сёдзё Ай"},
            {"shounen", "Сёнэн"},
            {"shounen ai", "Сёнэн Ай"},
            {"daily life", "Повседневность"},
            {"Slice of Life", "Повседневность"},
            {"space", "Космос"},
            {"alien", "Пришельцы"},
            {"space travel", "Космос"},
            {"sports", "Спорт"},
            {"super power", "Супер сила"},
            {"contemporary fantasy", "Сверхъестественное"},
            {"thriller", "Триллер"},
            {"vampire", "Вампиры"},
            {"witch", "Ведьмы"},
            {"yaoi", "Яой"},
            {"yuri", "Юри"},
            {"parasite", "Паразит"},
            {"neo-noir", "Нео-нуар"},
            {"noir", "Нуар"},
            {"mockumentary", "Псевдодокументальный"},
            {"magical girl", "Девочка-волшебница"},
            {"New", "Оригинальная работа"},
            {"juujin", "Зверолюди"},
            {"henshin", "Маскировка"},
            {"gender bender", "Смена пола"},
            {"educational", "Образовательный"},
            {"documentary", "Документальный"},
            {"dinosaur", "Динозавры"},
            {"detective", "Детектив"},
            {"competition", "Соревнования"},
            {"brainwashing", "Гипноз"},
            {"blackmail", "Шантаж"},
            {"dark-skinned girl", "Темнокожие"},
            {"dark skin", "Темнокожие"},
            {"Western", "Вестерн"},
            {"unidentified flying object", "НЛО"},
            {"torture", "Пытки"},
            {"reverse harem", "Обратный гарем"},
            {"maid", "Горничная"},
            {"nun", "Монахиня"},
            {"office lady", "Офисная работница"},
            {"tragedy", "Трагедия"},
            {"waitress", "Официантка"},
            {"bishounen", "Красивый юноша"},
            {"bishoujo", "Красивая женщина"},
            {"piloted robot", "Меха"},
            {"female teacher", "Учитель"},
            {"futanari", "Футанари"},
            {"guro", "Расчленение"},
            {"chikan", "Приставание"},
            {"parallel world", "Параллельный мир"},
            {"post-apocalyptic", "Постапокалиптика"},
            {"virtual world", "Виртуальный мир"},
            {"isekai", "Перерождение"},
            {"RPG", "РПГ"},
            {"performance", "Перформанс"},
            {"dystopia", "Антиутопия"},
            {"violence", "Насилие"},
            {"groping", "Ощупывание"},
            {"housewives", "Домохозяйка"},
            {"kodomo", "Детское"},
            {"josei", "Дзёсэй"},
            {"cosplaying", "Косплей"},
            {"teacher x student", "Учитель и студентка"},
            {"safer sex", "Безопасный секс"},
            {"public sex", "В общественном месте"},
            {"urination", "Мочеиспускание"},
            {"outdoor sex", "На улице"},
            {"impregnation", "Оплодотворение"},
            {"internal shots", "X-ray"},
            {"lactation", "Лактация"},
            {"macrophilia", "Макрофилия"},
            {"masturbation", "Мастурбация"},
            {"microphilia", "Микрофилия"},
            {"necrophilia", "Некрофилия"},
            {"oral", "Оральный секс"},
            {"orgy", "Оргия"},
            {"oyakodon", "Оякодон"},
            {"pantyjob", "Трусики"},
            {"petplay", "Пет-плей"},
            {"plot with porn", "С сюжетом"},
            {"point of view", "От первого лица"},
            {"pregnant sex", "Беременные"},
            {"ahegao", "Ахегао"},
            {"bestiality", "Зоофилия"},
            {"creampie", "Крипмай"},
            {"cybersex", "Киберсекс"},
            {"doggy style", "По собачьи"},
            {"erotic asphyxiation", "Удушье"},
            {"fingering", "Фингеринг"},
            {"fisting", "Фистинг"},
            {"foursome", "Секс вчетвером"},
            {"gang bang", "Групповуха"},
            {"glory hole", "Глорихол"},
            {"spanking", "Порка"},
            {"cunnilingus", "Куннилингус"},
            {"fellatio", "Фелляция"},
            {"foot fetish", "Фут-фетиш"},
            {"footjob", "Фут-джоб"},
            {"brother-sister incest", "Брат с сестрой"},
            {"infidelity", "Измена"},
            {"nudity", "Нагота"},
            {"uncensored version available", "Без цензуры"},
            {"censored uncensored version", "С цензурой"},
            {"sex tape", "Видеосьёмка"},
            {"enjoyable rape", "Без сопротивления"},
            {"zettai ryouiki", "Зеттай Рёики"},
            {"moe", "Moэ"},
            {"under one roof", "Под одной крышей"},
            {"parental abandonment", "Без взрослых"},
            {"TV censoring", "ТВ цензура"},
            {"shower scene", "В душевой"},
            {"school swimsuit", "Школьный купальник"},
            {"swimsuit", "Купальник"},
            {"slapstick", "Комедия"},
            {"forbidden love", "запретная любовь"},
            {"cross-dressing", "Переодевание"},
            {"trap", "Трап"},
            {"tennis", "Спорт"},
            {"sixty-nine", "Поза 69"},
            {"borderline porn", "Пограничное порно"},
            {"cervix penetration", "X-ray"},
            {"elementary school", "Начальная школа"},
            {"rivalry", "Соперничество"},
            {"water sex", "В воде"},
            {"nurse office", "Кабинет медсестры"},
            {"sex toys", "Секс-игрушки"},
            {"netorare", "Принуждение"},
            {"mother-daughter incest", "Мать и дочь"},
            {"mother-son incest", "Мать и сын"},
            {"hidden vibrator", "Секс-игрушки"},
            {"dildos - vibrators", "Секс-игрушки"},
            {"double-sided dildo", "Секс-игрушки"},
            {"father-daughter incest", "С отцом"},
            {"police", "Полицейские"},
            {"transforming craft", "Меха"},
            {"dragon", "Драконы"},
            {"angel", "Ангелы"},
            {"Gainax bounce", "Эччи"},
            {"android", "Андроиды"},
            {"absurdist humour", "Комедия"},
            {"super deformed", "Комедия"},
            {"otaku culture", "Отаку"},
            {"tsundere", "Цундэрэ"},
            {"violent retribution for accidental infringement", "Эччи"},
            {"horny nosebleed", "Эччи"},
            {"magic circles", "Магия"},
            {"science and magic coexist", "Наука и магия"},
            {"skimpy clothing", "Эччи"},
            {"medium awareness", "Комедия"},
            {"stereotypes", "Стереотипы"},
            {"tsukkomi", "Комедия"},
            {"treasure hunting", "Приключения"},
            {"journey", "Приключения"},
            {"adoring fan club", "Боевик"},
            {"black humour", "Черный юмор"},
            {"facial distortion", "Комедия"},
            {"funny expressions", "Комедия"},
            {"magical realism", "Повседневная магия"},
            {"sex change", "Смена пола"},
            {"twincest", "Близнецы"},
            {"aunt-nephew incest", "Тётя и племянник"},
            {"sister-sister incest", "Сестра с сестрой"},
            {"uncle-niece incest", "Дядя и пелмянница"},
            {"sleeping sex", "Спящие"},
        };

        public AniDbSeriesProvider(IApplicationPaths appPaths, IHttpClient httpClient)
        {
            _appPaths = appPaths;
            _httpClient = httpClient;

            TitleMatcher = AniDbTitleMatcher.DefaultInstance;

            Current = this;
        }

        internal static AniDbSeriesProvider Current { get; private set; }
        public IAniDbTitleMatcher TitleMatcher { get; set; }
        public int Order => 9;

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();

            var aid = info.GetProviderId(ProviderNames.AniDb);
            if (string.IsNullOrEmpty(aid) && !string.IsNullOrEmpty(info.Name))
            {
                aid = await Equals_check.Fast_xml_search(info.Name, info.Name, cancellationToken, true);
                if (string.IsNullOrEmpty(aid))
                {
                    aid = await Equals_check.Fast_xml_search(await Equals_check.Clear_name(info.Name, cancellationToken), await Equals_check.Clear_name(info.Name, cancellationToken), cancellationToken, true);
                }
            }

            if (!string.IsNullOrEmpty(aid))
            {
                result.Item = new Series();
                result.HasMetadata = true;

                result.Item.SetProviderId(ProviderNames.AniDb, aid);

                var seriesDataPath = await GetSeriesData(_appPaths, _httpClient, aid, cancellationToken);
                FetchSeriesInfo(result, seriesDataPath, info.MetadataLanguage ?? "en");
            }

            return result;
        }

        public string Name => "AniDB";

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var metadata = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);

            var list = new List<RemoteSearchResult>();

            if (metadata.HasMetadata)
            {
                var res = new RemoteSearchResult
                {
                    Name = metadata.Item.Name,
                    PremiereDate = metadata.Item.PremiereDate,
                    ProductionYear = metadata.Item.ProductionYear,
                    ProviderIds = metadata.Item.ProviderIds,
                    SearchProviderName = Name
                };

                list.Add(res);
            }

            return list;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        public static async Task<string> GetSeriesData(IApplicationPaths appPaths, IHttpClient httpClient, string seriesId, CancellationToken cancellationToken)
        {
            var dataPath = CalculateSeriesDataPath(appPaths, seriesId);
            var seriesDataPath = Path.Combine(dataPath, SeriesDataFile);
            var fileInfo = new FileInfo(seriesDataPath);

            // download series data if not present, or out of date
            if (!fileInfo.Exists || DateTime.UtcNow - fileInfo.LastWriteTimeUtc > TimeSpan.FromDays(7))
            {
                await DownloadSeriesData(seriesId, seriesDataPath, appPaths.CachePath, httpClient, cancellationToken).ConfigureAwait(false);
            }

            return seriesDataPath;
        }

        public static string CalculateSeriesDataPath(IApplicationPaths paths, string seriesId)
        {
            return Path.Combine(paths.CachePath, "anidb", "series", seriesId);
        }

        private void FetchSeriesInfo(MetadataResult<Series> result, string seriesDataPath, string preferredMetadataLangauge)
        {
            var series = result.Item;
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = File.Open(seriesDataPath, FileMode.Open, FileAccess.Read))
            using (var reader = XmlReader.Create(streamReader, settings))
            {
                reader.MoveToContent();

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "startdate":
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                            
                                    if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime date))
                                    {
                                        date = date.ToUniversalTime();
                                        series.PremiereDate = date;
                                        series.ProductionYear = date.Year;
                                    }
                                }

                                break;

                            case "enddate":
                                var endDate = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(endDate))
                                {
                                    if (DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime date))
                                    {
                                        date = date.ToUniversalTime();
                                        series.EndDate = date;
                                        if (DateTime.Now.Date < date.Date)
                                        {
                                            series.Status = SeriesStatus.Continuing;
                                        }
                                        else
                                        {
                                            series.Status = SeriesStatus.Ended;
                                        }
                                    }
                                }

                                break;

                            case "titles":
                                using (var subtree = reader.ReadSubtree())
                                {
                                    var title = ParseTitle(subtree, preferredMetadataLangauge);
                                    if (!string.IsNullOrEmpty(title))
                                    {
                                        series.Name = title;
                                    }
                                }

                                break;

                            case "creators":
                                using (var subtree = reader.ReadSubtree())
                                {
                                    ParseCreators(result, subtree);
                                }

                                break;

                            case "description":
                                series.Overview = ReplaceLineFeedWithNewLine(StripAniDbLinks(reader.ReadElementContentAsString()).Split(new[] { "Source:", "Note:" }, StringSplitOptions.None)[0]);

                                break;

                            case "ratings":
                                using (var subtree = reader.ReadSubtree())
                                {
                                    ParseRatings(series, subtree);
                                }

                                break;

                            case "resources":
                                using (var subtree = reader.ReadSubtree())
                                {
                                    ParseResources(series, subtree);
                                }

                                break;

                            case "characters":
                                using (var subtree = reader.ReadSubtree())
                                {
                                    ParseActors(result, subtree);
                                }

                                break;

                            case "tags":
                                using (var subtree = reader.ReadSubtree())
                                {
                                    ParseTags(series, subtree);
                                }

                                break;

                            case "categories":
                                using (var subtree = reader.ReadSubtree())
                                {
                                }

                                break;

                            case "episodes":
                                using (var subtree = reader.ReadSubtree())
                                {
                                    ParseEpisodes(series, subtree);
                                }

                                break;
                        }
                    }
                }
            }
            if (series.EndDate == null)
            {
                series.Status = SeriesStatus.Continuing;
            }

            GenreHelper.CleanupGenres(series);
        }

        private void ParseEpisodes(Series series, XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "episode")
                {
        
                    if (int.TryParse(reader.GetAttribute("id"), out int id) && IgnoredCategoryIds.Contains(id))
                        continue;

                    using (var episodeSubtree = reader.ReadSubtree())
                    {
                        while (episodeSubtree.Read())
                        {
                            if (episodeSubtree.NodeType == XmlNodeType.Element)
                            {
                                switch (episodeSubtree.Name)
                                {
                                    case "epno":
                                        //var epno = episodeSubtree.ReadElementContentAsString();
                                        //EpisodeInfo info = new EpisodeInfo();
                                        //info.AnimeSeriesIndex = series.AnimeSeriesIndex;
                                        //info.IndexNumberEnd = string(epno);
                                        //info.SeriesProviderIds.GetOrDefault(ProviderNames.AniDb);
                                        //episodes.Add(info);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ParseTags(Series series, XmlReader reader)
        {
            var genres = new List<GenreInfo>();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "tag")
                {
                    if (!int.TryParse(reader.GetAttribute("weight"), out int weight))
                        continue;

                    if (int.TryParse(reader.GetAttribute("id"), out int id) && IgnoredCategoryIds.Contains(id))
                        continue;

                    if (int.TryParse(reader.GetAttribute("parentid"), out int parentId) && IgnoredCategoryIds.Contains(parentId))
                        continue;

                    using (var categorySubtree = reader.ReadSubtree())
                    {
                        PluginConfiguration config = Plugin.Instance.Configuration;
                        while (categorySubtree.Read())
                        {
                            if (categorySubtree.NodeType == XmlNodeType.Element && categorySubtree.Name == "name")
                            {
                                /*
                                 * Since AniDB tagging (and weight) system is really messy additional TagsToGenre conversion was added. This method adds matching genre regardless of its weight.
                                 * 
                                 * If tags are not converted weight limitation works as in previous plugin versions (<=1.3.5)
                                 */

                                var name = categorySubtree.ReadElementContentAsString();
                                if (config.TidyGenreList)
                                {
                                    if (TagsToGenre.TryGetValue(name, out string mapped))
                                    {
                                        genres.Add(new GenreInfo { Name = mapped, Weight = weight }); 
                                    }
                                }
                                else if (weight >= 400)
                                {
                                    genres.Add(new GenreInfo { Name = UpperCase(name), Weight = weight });
                                }
                            }
                        }
                    }
                }
            }

            if (genres.Where(g => g.Name.Equals("18+")).ToArray().Length > 0)
            {
                series.OfficialRating = "18+";
            }
            
            series.Genres = genres.OrderBy(g => g.Weight).Select(g => g.Name).ToArray();
        }

        private void ParseResources(Series series, XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "resource")
                {
                    var type = reader.GetAttribute("type");

                    switch (type)
                    {
                        case "2":
                            var ids = new List<int>();

                            using (var idSubtree = reader.ReadSubtree())
                            {
                                while (idSubtree.Read())
                                {
                                    if (idSubtree.NodeType == XmlNodeType.Element && idSubtree.Name == "identifier")
                                    {
                                        if (int.TryParse(idSubtree.ReadElementContentAsString(), out int id))
                                            ids.Add(id);
                                    }
                                }
                            }

                            if (ids.Count > 0)
                            {
                                var firstId = ids.OrderBy(i => i).First().ToString(CultureInfo.InvariantCulture);
                                series.SetProviderId(ProviderNames.MyAnimeList, firstId);
                                //                                series.ProviderIds.Add(ProviderNames.AniList, firstId);
                            }

                            break;

                        case "4":
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "url")
                                {
                                    reader.ReadElementContentAsString();
                                    break;
                                }
                            }

                            break;
                    }
                }
            }
        }

        private static string UpperCase(string value)
        {
            char[] array = value.ToCharArray();
            if (array.Length >= 1)
                if (char.IsLower(array[0]))
                    array[0] = char.ToUpper(array[0]);

            for (int i = 1; i < array.Length; i++)
                if (array[i - 1] == ' ' || array[i - 1] == '-')
                    if (char.IsLower(array[i]))
                        array[i] = char.ToUpper(array[i]);

            return new string(array);
        }

        public static string StripAniDbLinks(string text)
        {
            return AniDbUrlRegex.Replace(text, "${name}");
        }

        public static string ReplaceLineFeedWithNewLine(string text)
        {
            return text.Replace("\n", "<br>\n");
        }

        private void ParseActors(MetadataResult<Series> series, XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "character")
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            ParseActor(series, subtree);
                        }
                    }
                }
            }
        }

        private void ParseActor(MetadataResult<Series> series, XmlReader reader)
        {
            string name = null;
            string role = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            role = reader.ReadElementContentAsString();
                            break;

                        case "seiyuu":
                            name = reader.ReadElementContentAsString();
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(role)) // && series.People.All(p => p.Name != name))
            {
                series.AddPerson(CreatePerson(name, PersonType.Actor, role));
            }
        }

        private void ParseRatings(Series series, XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "permanent")
                    {
    
                        if (float.TryParse(
                            reader.ReadElementContentAsString(),
                            NumberStyles.AllowDecimalPoint,
                            CultureInfo.InvariantCulture,
                            out float rating))
                        {
                            series.CommunityRating = (float)Math.Round(rating, 1);
                        }
                    }
                }
            }
        }

        private string ParseTitle(XmlReader reader, string preferredMetadataLangauge)
        {
            var titles = new List<Title>();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "title")
                {
                    var language = reader.GetAttribute("xml:lang");
                    var type = reader.GetAttribute("type");
                    var name = reader.ReadElementContentAsString();

                    titles.Add(new Title
                    {
                        Language = language,
                        Type = type,
                        Name = name
                    });
                }
            }

            return titles.Localize(Plugin.Instance.Configuration.TitlePreference, preferredMetadataLangauge).Name;
        }

        private void ParseCreators(MetadataResult<Series> series, XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "name")
                {
                    var type = reader.GetAttribute("type");
                    var name = reader.ReadElementContentAsString();

                    if (type == "Animation Work" || type == "Work")
                    {
                        series.Item.AddStudio(name);
                    }
                    else
                    {
                        series.AddPerson(CreatePerson(name, type));
                    }
                }
            }
        }

        private PersonInfo CreatePerson(string name, string type, string role = null)
        {
            // todo find nationality of person and conditionally reverse name order
            PersonType mappedType;

            if (!_typeMappings.TryGetValue(type, out  mappedType))
            {
                if (!Enum.TryParse(type, true, out mappedType))
                {
                    mappedType = PersonType.Actor;
                }
            }

            return new PersonInfo
            {
                Name = ReverseNameOrder(name),
                Type = mappedType,
                Role = role
            };
        }

        private PersonInfo CreatePerson(string name, PersonType type, string role = null)
        {
            return new PersonInfo
            {
                Name = ReverseNameOrder(name),
                Type = type,
                Role = role
            };
        }

        public static string ReverseNameOrder(string name)
        {
            return name.Split(' ').Reverse().Aggregate(string.Empty, (n, part) => n + " " + part).Trim();
        }

        private static async Task DownloadSeriesData(string aid, string seriesDataPath, string cachePath, IHttpClient httpClient, CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(seriesDataPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            DeleteXmlFiles(directory);

            var requestOptions = new HttpRequestOptions
            {
                Url = string.Format(SeriesQueryUrl, ClientName, aid),
                CancellationToken = cancellationToken,
                EnableHttpCompression = false
            };

            await RequestLimiter.Tick(cancellationToken);
            await Task.Run(() => Thread.Sleep(Plugin.Instance.Configuration.AniDB_wait_time));
            using (var stream = await httpClient.Get(requestOptions).ConfigureAwait(false))
            using (var unzipped = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new StreamReader(unzipped, Encoding.UTF8, true))
            using (var file = File.Open(seriesDataPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(file))
            {
                var text = await reader.ReadToEndAsync().ConfigureAwait(false);
                text = text.Replace("&#x0;", "");

                await writer.WriteAsync(text).ConfigureAwait(false);
            }

            await ExtractEpisodes(directory, seriesDataPath);
            ExtractCast(cachePath, seriesDataPath);
        }

        private static void DeleteXmlFiles(string path)
        {
            try
            {
                foreach (var file in new DirectoryInfo(path)
                    .EnumerateFiles("*.xml", SearchOption.AllDirectories)
                    .ToList())
                {
                    file.Delete();
                }
            }
            catch (DirectoryNotFoundException)
            {
                // No biggie
            }
        }

        private static async Task ExtractEpisodes(string seriesDataDirectory, string seriesDataPath)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = new StreamReader(seriesDataPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "episode")
                            {
                                var outerXml = reader.ReadOuterXml();
                                await SaveEpsiodeXml(seriesDataDirectory, outerXml).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }

        private static void ExtractCast(string cachePath, string seriesDataPath)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            var cast = new List<AniDbPersonInfo>();

            using (var streamReader = new StreamReader(seriesDataPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "characters")
                        {
                            var outerXml = reader.ReadOuterXml();
                            cast.AddRange(ParseCharacterList(outerXml));
                        }

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "creators")
                        {
                            var outerXml = reader.ReadOuterXml();
                            cast.AddRange(ParseCreatorsList(outerXml));
                        }
                    }
                }
            }

            var serializer = new XmlSerializer(typeof(AniDbPersonInfo));
            foreach (var person in cast)
            {
                var path = GetCastPath(person.Name, cachePath);
                var directory = Path.GetDirectoryName(path);
                Directory.CreateDirectory(directory);

                if (!File.Exists(path) || person.Image != null)
                {
                    try
                    {
                        using (var stream = File.Open(path, FileMode.Create))
                            serializer.Serialize(stream, person);
                    }
                    catch (IOException)
                    {
                        // ignore
                    }
                }
            }
        }

        public static AniDbPersonInfo GetPersonInfo(string cachePath, string name)
        {
            var path = GetCastPath(name, cachePath);
            var serializer = new XmlSerializer(typeof(AniDbPersonInfo));

            try
            {
                if (File.Exists(path))
                {
                    using (var stream = File.OpenRead(path))
                        return serializer.Deserialize(stream) as AniDbPersonInfo;
                }
            }
            catch (IOException)
            {
                return null;
            }

            return null;
        }

        private static string GetCastPath(string name, string cachePath)
        {
            name = name.ToLowerInvariant();
            return Path.Combine(cachePath, "anidb-people", name[0].ToString(), name + ".xml");
        }

        private static IEnumerable<AniDbPersonInfo> ParseCharacterList(string xml)
        {
            var doc = XDocument.Parse(xml);
            var people = new List<AniDbPersonInfo>();

            var characters = doc.Element("characters");
            if (characters != null)
            {
                foreach (var character in characters.Descendants("character"))
                {
                    var seiyuu = character.Element("seiyuu");
                    if (seiyuu != null)
                    {
                        var person = new AniDbPersonInfo
                        {
                            Name = ReverseNameOrder(seiyuu.Value)
                        };

                        var picture = seiyuu.Attribute("picture");
                        if (picture != null && !string.IsNullOrEmpty(picture.Value))
                        {
                            person.Image = "http://img7.anidb.net/pics/anime/" + picture.Value;
                        }

                        var id = seiyuu.Attribute("id");
                        if (id != null && !string.IsNullOrEmpty(id.Value))
                        {
                            person.Id = id.Value;
                        }

                        people.Add(person);
                    }
                }
            }

            return people;
        }

        private static IEnumerable<AniDbPersonInfo> ParseCreatorsList(string xml)
        {
            var doc = XDocument.Parse(xml);
            var people = new List<AniDbPersonInfo>();

            var creators = doc.Element("creators");
            if (creators != null)
            {
                foreach (var creator in creators.Descendants("name"))
                {
                    var type = creator.Attribute("type");
                    if (type != null && type.Value == "Animation Work")
                    {
                        continue;
                    }

                    var person = new AniDbPersonInfo
                    {
                        Name = ReverseNameOrder(creator.Value)
                    };

                    var id = creator.Attribute("id");
                    if (id != null && !string.IsNullOrEmpty(id.Value))
                    {
                        person.Id = id.Value;
                    }

                    people.Add(person);
                }
            }

            return people;
        }

        private static async Task SaveXml(string xml, string filename)
        {
            var writerSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Async = true
            };

            using (var writer = XmlWriter.Create(filename, writerSettings))
            {
                await writer.WriteRawAsync(xml).ConfigureAwait(false);
            }
        }

        private static async Task SaveEpsiodeXml(string seriesDataDirectory, string xml)
        {
            var episodeNumber = ParseEpisodeNumber(xml);

            if (episodeNumber != null)
            {
                var file = Path.Combine(seriesDataDirectory, string.Format("episode-{0}.xml", episodeNumber));
                await SaveXml(xml, file);
            }
        }

        private static string ParseEpisodeNumber(string xml)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = new StringReader(xml))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "epno")
                            {
                                var val = reader.ReadElementContentAsString();
                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    return val;
                                }
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="seriesId">The series id.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths, string seriesId)
        {
            var seriesDataPath = Path.Combine(GetSeriesDataPath(appPaths), seriesId);

            return seriesDataPath;
        }

        /// <summary>
        ///     Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "anidb\\series");

            return dataPath;
        }

        private struct GenreInfo
        {
            public string Name;
            public int Weight;
        }
    }

    public class Title
    {
        public string Language { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
    }

    public static class TitleExtensions
    {
        public static Title Localize(this IEnumerable<Title> titles, TitlePreferenceType preference, string metadataLanguage)
        {
            var titlesList = titles as IList<Title> ?? titles.ToList();

            switch (preference)
            {
                case TitlePreferenceType.Localized:
                {
                    // prefer an official title, else look for a synonym
                    var localized = titlesList.FirstOrDefault(t => t.Language == metadataLanguage && t.Type == "main") ??
                                    titlesList.FirstOrDefault(t => t.Language == metadataLanguage && t.Type == "official") ??
                                    titlesList.FirstOrDefault(t => t.Language == metadataLanguage && t.Type == "synonym");

                    if (localized != null)
                    {
                        return localized;
                    }

                    break;
                }
                case TitlePreferenceType.Japanese:
                {
                    // prefer an official title, else look for a synonym
                    var japanese = titlesList.FirstOrDefault(t => t.Language == "ja" && t.Type == "main") ??
                                   titlesList.FirstOrDefault(t => t.Language == "ja" && t.Type == "official") ??
                                   titlesList.FirstOrDefault(t => t.Language == "ja" && t.Type == "synonym");

                    if (japanese != null)
                    {
                        return japanese;
                    }

                    break;
                }
                case TitlePreferenceType.Korean:
                {
                    // prefer an official title, else look for a synonym
                    var korean = titlesList.FirstOrDefault(t => t.Language == "ko" && t.Type == "main") ??
                                   titlesList.FirstOrDefault(t => t.Language == "ko" && t.Type == "official") ??
                                   titlesList.FirstOrDefault(t => t.Language == "ko" && t.Type == "synonym");

                    if (korean != null)
                    {
                        return korean;
                    }

                    break;
                }
            }

            // return the main title (romaji)
            return titlesList.FirstOrDefault(t => t.Language == "x-jat" && t.Type == "main") ??
                   titlesList.FirstOrDefault(t => t.Type == "main") ??
                   titlesList.FirstOrDefault();
        }

        /// <summary>
        ///     Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="seriesId">The series id.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths, string seriesId)
        {
            var seriesDataPath = Path.Combine(GetSeriesDataPath(appPaths), seriesId);

            return seriesDataPath;
        }

        /// <summary>
        ///     Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "tvdb");

            return dataPath;
        }
    }
}