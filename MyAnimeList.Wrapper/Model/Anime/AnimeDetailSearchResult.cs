using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Anime
{
	[ImplementPropertyChanged]
	public class AnimeDetailSearchResult
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public int? Episodes { get; set; }
		public string Type { get; set; }
		public string Synopsis { get; set; }
		public string ImageUrl { get; set; }
		public double MembersScore { get; set; }
	}
}