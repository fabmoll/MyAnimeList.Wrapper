using System.Collections.Generic;
using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Manga
{
	[ImplementPropertyChanged]
	public class MangaRoot
	{
		public List<Manga> Manga { get; set; }
		public Statistics Statistics { get; set; }
	}
}