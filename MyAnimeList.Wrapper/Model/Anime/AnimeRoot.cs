using System.Collections.Generic;
using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Anime
{
	[ImplementPropertyChanged]
	public class AnimeRoot
	{
		public List<Anime> Animes { get; set; }
		public Statistics Statistics { get; set; }
	}
}