using System.Collections.Generic;
using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Manga
{
	[ImplementPropertyChanged]
    public class MangaDetail
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public OtherTitles OtherTitles { get; set; }
        public string Synopsis { get; set; }
        public string Type { get; set; }
        public string ImageUrl { get; set; }
        public int? Volumes { get; set; }
        public int? Chapters { get; set; }
        public int VolumesRead { get; set; }
        public int ChaptersRead { get; set; }
        public string ReadStatus { get; set; }
        public double MembersScore { get; set; }
        public int? ListedMangaId { get; set; }
        public int? MembersCount { get; set; }
        public int? PopularityRank { get; set; }
        public int? FavoritedCount { get; set; }
        public List<AnimeSummary> AnimeAdaptations { get; set; }
        public List<MangaSummary> RelatedManga { get; set; }
        public List<MangaSummary> AlternativeVersions { get; set; }
        public string Status { get; set; }
        public int Rank { get; set; }
        public object Score { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Tags { get; set; }
    }
}