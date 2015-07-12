using System.Collections.Generic;
using PropertyChanged;

namespace MyAnimeList.Wrapper.Model.Anime
{
	[ImplementPropertyChanged]
	public class OtherTitles
	{
		public List<string> English { get; set; }
		public List<string> Japanese { get; set; }
	}
}