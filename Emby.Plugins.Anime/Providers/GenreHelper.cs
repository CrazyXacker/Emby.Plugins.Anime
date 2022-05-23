using MediaBrowser.Controller.Entities.TV;
using Emby.Plugins.Anime.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Plugins.Anime.Providers
{
    public static class GenreHelper
    {
        private static readonly Dictionary<string, string> GenreMappings = new Dictionary<string, string>
        {
            {"Action", "Боевик"},
            {"Advanture", "Приключения"},
            {"Contemporary Fantasy", "Сверхъестественное"},
            {"Comedy", "Комедия"},
            {"Dark Fantasy", "Тёмное фэнтези"},
            {"Dementia", "Психологический триллер"},
            {"Demons", "Демоны"},
            {"Drama", "Драма"},
            {"Ecchi", "Эччи"},
            {"Fantasy", "Фэнтези"},
            {"Harem", "Гарем"},
            {"Hentai", "18+"},
            {"Historical", "Исторический"},
            {"Horror", "Ужасы"},
            {"Josei", "Дзёсэй"},
            {"Kids", "Детское"},
            {"Magic", "Магия"},
            {"Martial Arts", "Боевые искусства"},
            {"Mahou Shoujo", "Махо-сёдзё"},
            {"Mecha", "Меха"},
            {"Music", "Музыкальный"},
            {"Mystery", "Мистика"},
            {"Parody", "Комедия"},
            {"Psychological", "Психологический триллер"},
            {"Romance", "Романтика"},
            {"Sci-Fi", "Научная фантастика"},
            {"Seinen", "Сэйнэн"},
            {"Shoujo", "Сёдзё"},
            {"Shounen", "Сёнэн"},
            {"Slice of Life", "Повседневность"},
            {"Space", "Космос"},
            {"Sports", "Спорт"},
            {"Supernatural", "Сверхъестественное"},
            {"Thriller", "Триллер"},
            {"Tragedy", "Трагедия"},
            {"Witch", "Ведьмы"},
            {"Vampire", "Вампиры"},
            {"Yaoi", "Яой"},
            {"Yuri", "Юри"},
            {"Zombie", "Зомби"},
            //AniSearch Genre
            {"Geister­geschichten", "Привидения"},
            {"Romanze", "Романтика"},
            {"Alltagsdrama", "Драма"},
            {"Alltagsleben", "Повседневность"},
            {"Psychodrama", "Психология"},
            {"Actiondrama", "Боевик"},
            {"Nonsense-Komödie", "Комедия"},
            {"Magie", "Фэнтези"},
            {"Abenteuer", "Приключения"},
            {"Komödie", "Комедия"},
            {"Erotik", "18+"},
            {"Historisch", "Исторический"},
            {"Romantische Komödie", "Ромком"},
			{"Maid", "Горничная"},
			{"Oberschule", "Школа"},
			{"Schule", "Школа"},
			{"BigBoobs", "Большая грудь"},
			{"Entjungferung", "В первый раз"},
			{"Hardcore", "БДСМ"},
			{"Im Freien", "На улице"},
			{"Masturbation", "Мастурбация"},
			{"Schulmädchen", "Школьницы"},
			{"Urinieren", "Мочеиспускание"},
			{"Dämonen", "Демоны"},
			{"Anal", "Анал"},
			{"Chikan", "Приставание"},
			{"Exhibitionismus", "Нагота"},
			{"Inzest", "Инцест"},
			{"Lactation", "Лактация"},
			{"Oral", "Оральный секс"},
			{"Sado Maso", "БДСМ"},
			{"Schwangerschaft", "Беременные"},
			{"Bondage", "Бондаж"},
			{"Bürodame", "Офисная работница"},
			{"Flat Chested", "Маленькая грудь"},
			{"Footjob", "Фут-джоб"},
			{"Kemonomimi", "Зверолюди"},
			{"Lehrerin", "Учитель"},
			{"Tentakel", "Тентакли"},
			{"Magical Girl", "Девочка-волшебница"},
			{"Elfen", "Эльфы"},
			{"Cruel", "БДСМ"},
            //Proxer
            {"Slice_of_Life", "Slice of Life"},
        };

        private static readonly string[] GenresAsTags =
        {
            "Hentai",
            "Space",
            "Weltraum",
            "Yaoi",
            "Yuri",
            "Demons",
            "Witch",
            //AniSearchTags
            "Krieg",
            "Militär",
            "Satire",
            "Übermäßige Gewaltdarstellung",
            "Monster",
            "Zeitgenössische Fantasy",
            "Dialogwitz",
            "Romantische Komödie",
            "Slapstick",
            "Alternative Welt",
            "4-panel",
            "CG-Anime",
            "Episodisch",
            "Moe",
            "Parodie",
            "Splatter",
            "Tragödie",
            "Verworrene Handlung",
            //Themen
            "Erwachsenwerden",
            "Gender Bender",
            "Ältere Frau, jüngerer Mann",
            "Älterer Mann, jüngere Frau",
            //Schule (School)
            "Grundschule",
            "Kindergarten",
            "Klubs",
            "Mittelschule",
            "Oberschule",
            "Schule",
            "Universität",
            //Zeit (Time)
            "Altes Asien",
            "Frühe Neuzeit",
            "Gegenwart",
            "industrialisierung",
            "Meiji-Ära",
            "Mittelalter",
            "Weltkriege",
            //Fantasy
            "Dunkle Fantasy",
            "Epische Fantasy",
            "Zeitgenössische Fantasy",
            //Ort
            "Alternative Welt",
            "In einem Raumschiff",
            "Weltraum",
            //Setting
            "Cyberpunk",
            "Endzeit",
            "Space Opera",
            //Hauptfigur
            "Charakterschache Heldin",
            "Charakterschacher Held",
            "Charakterstarke Heldin",
            "Charakterstarker Held",
            "Gedächtnisverlust",
            "Stoische Heldin",
            "Stoischer Held",
            "Widerwillige Heldin",
            "Widerwilliger Held",
            //Figuren
            "Diva",
            "Genie",
            "Schul-Delinquent",
            "Tomboy",
            "Tsundere",
            "Yandere",
            //Kampf (fight)
            "Bionische Kräfte",
            "Martial Arts",
            "PSI-Kräfte",
            "Real Robots",
            "Super Robots",
            "Schusswaffen",
            "Schwerter & co",
            //Sports (Sport)
            "Baseball",
            "Boxen",
            "Denk- und Glücksspiele",
            "Football",
            "Fußball",
            "Kampfsport",
            "Rennsport",
            "Tennis",
            //Kunst (Art)
            "Anime & Film",
            "Malerei",
            "Manga & Doujinshi",
            "Musik",
            "Theater",
            //Tätigkeit
            "Band",
            "Detektiv",
            "Dieb",
            "Essenszubereitung",
            "Idol",
            "Kopfgeldjäger",
            "Ninja",
            "Polizist",
            "Ritter",
            "Samurai",
            "Solosänger",
            //Wesen
            "Außerirdische",
            "Cyborgs",
            "Dämonen",
            "Elfen",
            "Geister",
            "Hexen",
            "Himmlische Wesen",
            "Kamis",
            "Kemonomimi",
            "Monster",
            "Roboter & Androiden",
            "Tiermenschen",
            "Vampire",
            "Youkai",
            "Zombie",
            //Proxer
            "Virtual Reality",
            "Game",
            "Survival",
            "Fanservice",
            "Schlauer Protagonist",
        };

        private static readonly Dictionary<string, string> IgnoreIfPresent = new Dictionary<string, string>
        {
            {"Psychological Thriller", "Thriller"}
        };

        public static void CleanupGenres(Series series)
        {
            PluginConfiguration config = Plugin.Instance.Configuration;
            
            if (config.TidyGenreList)
            {
                series.Genres = RemoveRedundantGenres(series.Genres)
                                           .Distinct()
                                           .ToArray();
            
                TidyGenres(series);
            }
            
            var max = config.MaxGenres;
            if (config.AddAnimeGenre)
            {
                series.Genres = series.Genres.Except(new[] { "Animation", "Anime" }).ToArray();
            
                max = Math.Max(max - 1, 0);
            }
            
            if (config.MaxGenres > 0)
            {
                series.Genres = series.Genres.Take(max).ToArray();
            }
            
            if (!series.Genres.Contains("Anime") && config.AddAnimeGenre)
            {
                series.Genres = series.Genres.Except(new[] { "Animation" }).ToArray();
            
                series.AddGenre("Аниме");
            }

            series.Genres = series.Genres.OrderBy(i => i).ToArray();
        }

        public static void TidyGenres(Series series)
        {
	        var genres = new HashSet<string>();
            var tags = new HashSet<string>(series.Tags);

            foreach (string genre in series.Genres)
            {
                string mapped;
                if (GenreMappings.TryGetValue(genre, out mapped))
                    genres.Add(mapped);
                else
                {
                    genres.Add(genre);
                }

                if (GenresAsTags.Contains(genre))
                {
                    genres.Add(genre);
                }
            }

            series.Genres = genres.ToArray();
            series.Tags = tags.ToArray();
        }

        public static IEnumerable<string> RemoveRedundantGenres(IEnumerable<string> genres)
        {
            var list = genres as IList<string> ?? genres.ToList();

            var toRemove = list.Where(IgnoreIfPresent.ContainsKey).Select(genre => IgnoreIfPresent[genre]).ToList();
            return list.Where(genre => !toRemove.Contains(genre));
        }
    }
}