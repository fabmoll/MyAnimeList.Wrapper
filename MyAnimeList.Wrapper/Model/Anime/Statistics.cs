using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Anime
{
	[ImplementPropertyChanged]
	public class Statistics
	{
		public double Days { get; set; }
	}
}