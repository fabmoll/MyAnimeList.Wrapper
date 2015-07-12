using System.Collections.Generic;
using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Anime
{
	[ImplementPropertyChanged]
    public class AnimeDetail
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Synopsis { get; set; }
        public string Type { get; set; }
        public int Rank { get; set; }
        public string ImageUrl { get; set; }
        public int? Episodes { get; set; }
        public string Status { get; set; }
        public double MembersScore { get; set; }
        public int? ListedAnimeId { get; set; }
        public int WatchedEpisodes { get; set; }
        public int Score { get; set; }
        public string WatchedStatus { get; set; }
        public int? PopularityRank { get; set; }
        public OtherTitles OtherTitles { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Classification { get; set; }
        public int? MembersCount { get; set; }
        public int? FavoritedCount { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Tags { get; set; }
        public List<MangaSummary> MangaAdaptations { get; set; }
        public List<AnimeSummary> Prequels { get; set; }
        public List<AnimeSummary> Sequels { get; set; }
        public List<AnimeSummary> SideStories { get; set; }
        public AnimeSummary ParentStory { get; set; }
        public List<AnimeSummary> CharacterAnime { get; set; }
        public List<AnimeSummary> SpinOffs { get; set; }
        public List<AnimeSummary> Summaries { get; set; }
        public List<AnimeSummary> AlternativeVersions { get; set; }
        public string Progress { get { return WatchedEpisodes + "/" + (Episodes ?? 0); } }
    }


}