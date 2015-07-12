using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Manga
{
	[ImplementPropertyChanged]
	public class Statistics
	{
		public double Days { get; set; }
	}
}