using System;
using System.Windows.Media.Imaging;
using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Manga
{
	[ImplementPropertyChanged]
	public class Manga
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public OtherTitles OtherTitles { get; set; }
		public string Synopsis { get; set; }
		public string Type { get; set; }
		public string ImageUrl { get; set; }
		public int Volumes { get; set; }
		public int VolumesRead { get; set; }
		public string Status { get; set; }
		public string ReadStatus { get; set; }
		public string Rank { get; set; }
		public object MembersScore { get; set; }
		public int Score { get; set; }
		public int ListedMangaId { get; set; }
		public int Chapters { get; set; }

		public string ChaptersVolume
		{
			get { return String.Format(ChaptersRead + "/" + VolumesRead); }
		}
		public string GroupHeader { get { return Title.Substring(0, 1); } }

		public int ChaptersRead { get; set; }
		public BitmapImage Picture { get; set; }
	}
}