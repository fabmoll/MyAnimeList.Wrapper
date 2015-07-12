using PropertyChanged;

namespace MyAnimeList.Wrapper.Model
{
	[ImplementPropertyChanged]
	public class InfoSummary
	{
		public string Title { get; set; }
		public string Url { get; set; }
	}

	[ImplementPropertyChanged]
	public class MangaSummary : InfoSummary
	{
		public int MangaId { get; set; }
	}

	[ImplementPropertyChanged]
	public class AnimeSummary : InfoSummary
	{
		public int AnimeId { get; set; }
	}
}