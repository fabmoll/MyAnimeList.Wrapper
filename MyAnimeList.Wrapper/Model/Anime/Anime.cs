using System.Windows.Media.Imaging;
using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Anime
{
	[ImplementPropertyChanged]
	public class Anime
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public OtherTitles OtherTitles { get; set; }
		public string Synopsis { get; set; }
		public string Type { get; set; }
		public string ImageUrl { get; set; }
		public int? Episodes { get; set; }
		public string Status { get; set; }
		public string WatchedStatus { get; set; }
		public string Rank { get; set; }
		public int Score { get; set; }
		public object MembersScore { get; set; }
		public int ListedAnimeId { get; set; }
		public int WatchedEpisodes { get; set; }

		public string Progress
		{
			get { return WatchedEpisodes + "/" + (Episodes ?? 0); }
		}

		public string GroupHeader
		{
			get { return Title.Substring(0, 1); }
		}

		public BitmapImage Picture { get; set; }
	}
}