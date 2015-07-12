using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Manga
{
	[ImplementPropertyChanged]
	public class MangaDetailSearchResult
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public object Volumes { get; set; }
		public object Chapters { get; set; }
		public string Type { get; set; }
		public string Synopsis { get; set; }
		public string ImageUrl { get; set; }
		public double MembersScore { get; set; }
	}
}