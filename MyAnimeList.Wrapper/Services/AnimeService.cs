using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using MyAnimeList.Wrapper.Model;
using MyAnimeList.Wrapper.Model.Anime;
using MyAnimeList.Wrapper.Resources;
using MyAnimeList.Wrapper.ServicesContracts;
using RestSharp;

namespace MyAnimeList.Wrapper.Services
{
	public class AnimeService : BaseService, IAnimeService
	{
		public AnimeService(string userAgent)
			: base(userAgent)
		{
		}

		public async Task<AnimeRoot> FindAnimeListAsync(string login)
		{
			RestClient.BaseUrl = new Uri("http://myanimelist.net/malappinfo.php");

			var request = GetRestRequest(Method.GET);

			request.AddParameter("u", login);
			request.AddParameter("status", "all");
			request.AddParameter("type", "anime");

			var result = await ExecuteTaskASync(request).ConfigureAwait(false);

			try
			{
				var xDocument = XDocument.Parse(result).Root;

				var animeRoot = new AnimeRoot { Animes = new List<Anime>(), Statistics = new Statistics() };

				if (xDocument.Element("myinfo") != null && xDocument.Element("myinfo").Element("user_days_spent_watching") != null)
					animeRoot.Statistics.Days = xDocument.Element("myinfo").ElementValue("user_days_spent_watching", 0.0d);

				foreach (var animeElement in xDocument.Elements("anime"))
				{
					var anime = new Anime
					{
						Id = animeElement.ElementValue("series_animedb_id", 0),
						Title = animeElement.Element("series_title").Value,
						Type = GetType(animeElement.ElementValue("series_type", 0)),
						Status = GetStatus(animeElement.ElementValue("series_status", 0)),
						Episodes = animeElement.ElementValue("series_episodes", 0),
						ImageUrl = animeElement.Element("series_image").Value,
						ListedAnimeId = animeElement.ElementValue("my_id", 0),
						WatchedEpisodes = animeElement.ElementValue("my_watched_episodes", 0),
						Score = animeElement.ElementValue("my_score", 0),
						WatchedStatus = GetWatchStatus(animeElement.ElementValue("my_status", 0))
					};
					animeRoot.Animes.Add(anime);
				}

				return animeRoot;
			}
			catch (XmlException exception)
			{
				throw new ServiceException(Resource.ServiceUnableToPerformActionException, exception.InnerException);
			}
		}

		public async Task<AnimeDetail> GetAnimeDetailAsync(string login, string password, int animeId)
		{
			var cookies = await CookieHelper.GetCookies(login, password, UserAgent);
			
			RestClient.BaseUrl = new Uri(string.Format("http://myanimelist.net/anime/{0}", animeId));

			var request = GetRestRequest(Method.GET, cookies);

			var result = await ExecuteTaskASync(request).ConfigureAwait(false);

			var animeDetail = new AnimeDetail();

			var document = new HtmlAgilityPack.HtmlDocument();

			document.LoadHtml(result);

			var animeIdInput = document.DocumentNode.SelectSingleNode("//input[@name='aid']");

			//Get Anime Id
			//Example: <input type="hidden" value="104" name="aid" />
			if (animeIdInput != null)
			{
				animeDetail.Id = Convert.ToInt32(animeIdInput.Attributes["value"].Value);
			}
			else
			{
				var detailLink = document.DocumentNode.SelectSingleNode("//a[text()='Details']");

				if (detailLink != null)
				{
					var regex = Regex.Match(detailLink.Attributes["href"].Value, @"\d+");
					animeDetail.Id = Convert.ToInt32(regex.ToString());
				}
			}

			//Title and rank.
			//Example:
			//# <h1><div style="float: right; font-size: 13px;">Ranked #96</div>Lucky ☆ Star</h1>
			var rankNode = document.DocumentNode.SelectSingleNode("//div[@id='contentWrapper']//div");

			if (rankNode != null)
			{
				if (rankNode.InnerText.ToUpper().Contains("N/A"))
					animeDetail.Rank = 0;
				else
				{
					var regex = Regex.Match(rankNode.InnerText, @"\d+");
					animeDetail.Rank = Convert.ToInt32(regex.ToString());
				}
			}

			var titleNode = document.DocumentNode.SelectSingleNode("//span[@itemprop='name']");


			if (titleNode != null)
				animeDetail.Title = HttpUtility.HtmlDecode(titleNode.InnerText.Trim());

			//Image Url
			var imageNode = document.DocumentNode.SelectSingleNode("//div[@id='content']//tr//td//div//img");

			if (imageNode != null)
				animeDetail.ImageUrl = imageNode.Attributes["src"].Value;

			//Extract from sections on the left column: Alternative Titles, Information, Statistics, Popular Tags

			var leftColumnNodeset =
				 document.DocumentNode.SelectSingleNode("//div[@id='content']//table//tr//td[@class='borderClass']");

			//  # Alternative Titles section.
			//# Example:
			//# <h2>Alternative Titles</h2>
			//# <div class="spaceit_pad"><span class="dark_text">English:</span> Lucky Star/div>
			//# <div class="spaceit_pad"><span class="dark_text">Synonyms:</span> Lucky Star, Raki ☆ Suta</div>
			//# <div class="spaceit_pad"><span class="dark_text">Japanese:</span> らき すた</div>

			if (leftColumnNodeset != null)
			{
				var englishAlternative = leftColumnNodeset.SelectSingleNode("//span[text()='English:']");

				animeDetail.OtherTitles = new OtherTitles();

				if (englishAlternative != null)
				{
					animeDetail.OtherTitles.English = englishAlternative.NextSibling.InnerText.Split(',').Select(p => p.Trim()).ToList();
				}

				var japaneseAlternative = leftColumnNodeset.SelectSingleNode("//span[text()='Japanese:']");

				if (japaneseAlternative != null)
				{
					animeDetail.OtherTitles.Japanese = japaneseAlternative.NextSibling.InnerText.Split(',').Select(p => p.Trim()).ToList();
				}

				//# Information section.
				//# Example:
				//# <h2>Information</h2>
				//# <div><span class="dark_text">Type:</span> TV</div>
				//# <div class="spaceit"><span class="dark_text">Episodes:</span> 24</div>
				//# <div><span class="dark_text">Status:</span> Finished Airing</div>
				//# <div class="spaceit"><span class="dark_text">Aired:</span> Apr  9, 2007 to Sep  17, 2007</div>
				//# <div>
				//#   <span class="dark_text">Producers:</span>
				//#   <a href="http://myanimelist.net/anime.php?p=2">Kyoto Animation</a>,
				//#   <a href="http://myanimelist.net/anime.php?p=104">Lantis</a>,
				//#   <a href="http://myanimelist.net/anime.php?p=262">Kadokawa Pictures USA</a><sup><small>L</small></sup>,
				//#   <a href="http://myanimelist.net/anime.php?p=286">Bang Zoom! Entertainment</a>
				//# </div>
				//# <div class="spaceit">
				//#   <span class="dark_text">Genres:</span>
				//#   <a href="http://myanimelist.net/anime.php?genre[]=4">Comedy</a>,
				//#   <a href="http://myanimelist.net/anime.php?genre[]=20">Parody</a>,
				//#   <a href="http://myanimelist.net/anime.php?genre[]=23">School</a>,
				//#   <a href="http://myanimelist.net/anime.php?genre[]=36">Slice of Life</a>
				//# </div>
				//# <div><span class="dark_text">Duration:</span> 24 min. per episode</div>
				//# <div class="spaceit"><span class="dark_text">Rating:</span> PG-13 - Teens 13 or older</div>

				var type = leftColumnNodeset.SelectSingleNode("//span[text()='Type:']");

				if (type != null)
					animeDetail.Type = type.NextSibling.InnerText.Trim();

				var episode = leftColumnNodeset.SelectSingleNode("//span[text()='Episodes:']");

				if (episode != null)
				{
					int episodes;
					if (Int32.TryParse(episode.NextSibling.InnerText.Replace(",", ""), out episodes))
						animeDetail.Episodes = episodes;
					else
					{
						animeDetail.Episodes = null;
					}
				}

				var status = leftColumnNodeset.SelectSingleNode("//span[text()='Status:']");

				if (status != null)
					animeDetail.Status = status.NextSibling.InnerText;

				var aired = leftColumnNodeset.SelectSingleNode("//span[text()='Aired:']");

				if (aired != null)
				{
					var airDateText = aired.NextSibling.InnerText;

					if (airDateText.Contains("to"))
					{
						var startDateText = airDateText.Substring(0, airDateText.IndexOf("to")).Trim();

						var options = RegexOptions.None;
						var regex = new Regex(@"[ ]{2,}", options);
						startDateText = regex.Replace(startDateText, @" ");

						DateTime startDate;

						if (DateTime.TryParseExact(startDateText, "MMM d, yyyy", CultureInfo.InvariantCulture,
															DateTimeStyles.None, out startDate))
						{
							animeDetail.StartDate = startDate.ToString("d");
						}
						else
							animeDetail.StartDate = startDateText;

						if (airDateText.Contains("?"))
						{
							animeDetail.EndDate = null;
						}
						else
						{
							var endDateText = airDateText.Substring(airDateText.IndexOf("to") + 2).Trim();

							endDateText = regex.Replace(endDateText, @" ");

							DateTime endDate;

							if (DateTime.TryParseExact(endDateText, "MMM d, yyyy", CultureInfo.InvariantCulture,
								 DateTimeStyles.None, out endDate))
							{
								animeDetail.EndDate = endDate.ToString("d");
							}
							else
							{
								animeDetail.EndDate = endDateText;
							}
						}
					}
					else
					{

						var startDateText = airDateText.IndexOf("to") == -1 ? airDateText.Trim() : airDateText.Substring(0, airDateText.IndexOf("to")).Trim();

						var options = RegexOptions.None;
						var regex = new Regex(@"[ ]{2,}", options);
						startDateText = regex.Replace(startDateText, @" ");

						DateTime startDate;

						if (DateTime.TryParseExact(startDateText, "MMM d, yyyy", CultureInfo.InvariantCulture,
															DateTimeStyles.NoCurrentDateDefault, out startDate))
							animeDetail.StartDate = startDate.ToString("d");
						else if (DateTime.TryParseExact(startDateText, "MMM yyyy", CultureInfo.InvariantCulture,
																  DateTimeStyles.NoCurrentDateDefault, out startDate))
						{
							animeDetail.StartDate = startDate.ToString("d");
						}
						else
							animeDetail.StartDate = startDateText;

						animeDetail.EndDate = null;
					}

				}

				var genre = leftColumnNodeset.SelectSingleNode("//span[text()='Genres:']");

				if (genre != null)
				{
					animeDetail.Genres = genre.ParentNode.ChildNodes.Where(c => c.Name == "a").Select(x => x.InnerText.Trim()).ToList();
				}

				var classification = leftColumnNodeset.SelectSingleNode("//span[text()='Rating:']");

				if (classification != null)
				{
					animeDetail.Classification = Regex.Replace(classification.NextSibling.InnerText, @"\t|\n|\r", "").Trim();
				}

				//# Statistics
				//# Example:
				//# <h2>Statistics</h2>
				//# <div>
				//#   <span class="dark_text">Score:</span> 8.41<sup><small>1</small></sup>
				//#   <small>(scored by 22601 users)</small>
				//# </div>
				//# <div class="spaceit"><span class="dark_text">Ranked:</span> #96<sup><small>2</small></sup></div>
				//# <div><span class="dark_text">Popularity:</span> #15</div>
				//# <div class="spaceit"><span class="dark_text">Members:</span> 36,961</div>
				//# <div><span class="dark_text">Favorites:</span> 2,874</div>

				var score = leftColumnNodeset.SelectSingleNode("//span[text()='Score:']");

				if (score != null)
				{
					double memberScore;
					if (double.TryParse(score.NextSibling.NextSibling.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture, out memberScore))
						animeDetail.MembersScore = memberScore;
					else
					{
						animeDetail.MembersScore = 0;
					}
				}

				var popularity = leftColumnNodeset.SelectSingleNode("//span[text()='Popularity:']");

				if (popularity != null)
				{
					int popularityRank;

					if (Int32.TryParse(popularity.NextSibling.InnerText.Replace("#", "").Replace(",", ""), out popularityRank))
						animeDetail.PopularityRank = popularityRank;
					else
					{
						animeDetail.PopularityRank = null;
					}
				}

				var member = leftColumnNodeset.SelectSingleNode("//span[text()='Members:']");

				if (member != null)
				{
					int memberCount;
					if (Int32.TryParse(member.NextSibling.InnerText.Replace(",", ""), out memberCount))
						animeDetail.MembersCount = memberCount;
					else
					{
						animeDetail.MembersCount = null;
					}
				}

				var favorite = leftColumnNodeset.SelectSingleNode("//span[text()='Favorites:']");

				if (favorite != null)
				{
					int favoritedCount;
					if (Int32.TryParse(favorite.NextSibling.InnerText.Replace(",", ""), out favoritedCount))
						animeDetail.FavoritedCount = favoritedCount;
					else
					{
						animeDetail.FavoritedCount = null;
					}
				}


				//# Popular Tags
				//# Example:
				//# <h2>Popular Tags</h2>
				//# <span style="font-size: 11px;">
				//#   <a href="http://myanimelist.net/anime.php?tag=comedy" style="font-size: 24px" title="1059 people tagged with comedy">comedy</a>
				//#   <a href="http://myanimelist.net/anime.php?tag=parody" style="font-size: 11px" title="493 people tagged with parody">parody</a>
				//#   <a href="http://myanimelist.net/anime.php?tag=school" style="font-size: 12px" title="546 people tagged with school">school</a>
				//#   <a href="http://myanimelist.net/anime.php?tag=slice of life" style="font-size: 18px" title="799 people tagged with slice of life">slice of life</a>
				//# </span>

				//Popular global tags are removed from MAL
				var popularTags = leftColumnNodeset.SelectSingleNode("//span[preceding-sibling::h2[text()='Popular Tags']]");

				if (popularTags != null)
				{
					animeDetail.Tags = popularTags.ChildNodes.Where(c => c.Name == "a").Select(x => x.InnerText.Trim()).ToList();
				}

			}

			//# -
			//# Extract from sections on the right column: Synopsis, Related Anime, Characters & Voice Actors, Reviews
			//# Recommendations.

			//Getting table directly doesn't work, the second td inside the first table isn't found...

			//var rightColumnNodeset =
			//  document.DocumentNode.SelectSingleNode("//div[@id='content']/table/tr/td/div/table");

			//So I get it from the child
			var rightColumnNodeset = document.DocumentNode.SelectSingleNode("//h2[text()='Synopsis']").ParentNode.ParentNode.ParentNode;

			if (rightColumnNodeset != null)
			{
				//# Synopsis
				//# Example:
				//# <h2>Synopsis</h2>
				//# Yotsuba's daily life is full of adventure. She is energetic, curious, and a bit odd &ndash; odd enough to be called strange by her father as well as ignorant of many things that even a five-year-old should know. Because of this, the most ordinary experience can become an adventure for her. As the days progress, she makes new friends and shows those around her that every day can be enjoyable.<br />
				//# <br />
				//# [Written by MAL Rewrite]
				var synopsis = rightColumnNodeset.SelectSingleNode("//h2[text()='Synopsis']");

				if (synopsis != null)
				{
					animeDetail.Synopsis = Regex.Replace(HttpUtility.HtmlDecode(synopsis.NextSibling.InnerText), "<br>", "");
				}

				//  # Related Anime
				//# Example:
				//# <td>
				//#   <br>
				//#   <h2>Related Anime</h2>
				//#   Adaptation: <a href="http://myanimelist.net/manga/9548/Higurashi_no_Naku_Koro_ni_Kai_Minagoroshi-hen">Higurashi no Naku Koro ni Kai Minagoroshi-hen</a>,
				//#   <a href="http://myanimelist.net/manga/9738/Higurashi_no_Naku_Koro_ni_Matsuribayashi-hen">Higurashi no Naku Koro ni Matsuribayashi-hen</a><br>
				//#   Prequel: <a href="http://myanimelist.net/anime/934/Higurashi_no_Naku_Koro_ni">Higurashi no Naku Koro ni</a><br>
				//#   Sequel: <a href="http://myanimelist.net/anime/3652/Higurashi_no_Naku_Koro_ni_Rei">Higurashi no Naku Koro ni Rei</a><br>
				//#   Side story: <a href="http://myanimelist.net/anime/6064/Higurashi_no_Naku_Koro_ni_Kai_DVD_Specials">Higurashi no Naku Koro ni Kai DVD Specials</a><br>

				var relatedAnime = rightColumnNodeset.SelectSingleNode("//h2[text()='Related Anime']");

				if (relatedAnime != null)
				{
					//Alternative
					var adaptation =
						 Regex.Match(
							  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
							  "Adaptation:+(.+?<br)");

					if (!string.IsNullOrEmpty(adaptation.ToString()))
					{

						animeDetail.MangaAdaptations = new List<MangaSummary>();

						SetMangaSummaryList(animeDetail.MangaAdaptations, adaptation.ToString());
					}

					//Prequel
					var prequel =
						 Regex.Match(
							  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
							  "Prequel:+(.+?<br)");

					if (!string.IsNullOrEmpty(prequel.ToString()))
					{

						animeDetail.Prequels = new List<AnimeSummary>();

						SetAnimeSummaryList(animeDetail.Prequels, prequel.ToString());
					}

					//Sequel
					var sequel =
						 Regex.Match(
							  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
							  "Sequel:+(.+?<br)");

					if (!string.IsNullOrEmpty(sequel.ToString()))
					{

						animeDetail.Sequels = new List<AnimeSummary>();

						SetAnimeSummaryList(animeDetail.Sequels, sequel.ToString());
					}

					//Side story
					var sideStory =
						 Regex.Match(
							  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
							  "Side story:+(.+?<br)");

					if (!string.IsNullOrEmpty(sideStory.ToString()))
					{
						animeDetail.SideStories = new List<AnimeSummary>();

						SetAnimeSummaryList(animeDetail.SideStories, sideStory.ToString());
					}

					//Parent story
					var parentStory =
						 Regex.Match(
							  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
							  "Parent story:+(.+?<br)");

					if (!string.IsNullOrEmpty(parentStory.ToString()))
					{

						animeDetail.ParentStory = SetAnimeSummaryList(parentStory.ToString());
					}

					//Character
					//var character =
					//	 Regex.Match(
					//		  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
					//		  "Character:?(.+?<br)");

					//if (!string.IsNullOrEmpty(character.ToString()))
					//{
					//	animeDetail.CharacterAnime = new List<AnimeSummary>();

					//	SetAnimeSummaryList(animeDetail.CharacterAnime, character.ToString());
					//}

					//Spin off
					var spinOff =
						 Regex.Match(
							  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
							  "Spin-off:+(.+?<br)");

					if (!string.IsNullOrEmpty(spinOff.ToString()))
					{
						animeDetail.SpinOffs = new List<AnimeSummary>();

						SetAnimeSummaryList(animeDetail.SpinOffs, spinOff.ToString());
					}

					//Summary
					var summary =
						 Regex.Match(
							  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
							  "Summary:+(.+?<br)");

					if (!string.IsNullOrEmpty(summary.ToString()) && ! summary.ToString().Contains("Summary:<br"))
					{
						animeDetail.Summaries = new List<AnimeSummary>();

						SetAnimeSummaryList(animeDetail.Summaries, summary.ToString());
					}

					//Alternative version
					var alternativeVersion =
						 Regex.Match(
							  relatedAnime.ParentNode.InnerHtml.Substring(relatedAnime.ParentNode.InnerHtml.IndexOf("<h2>")),
							  "Alternative versions?:+(.+?<br)");

					if (!string.IsNullOrEmpty(alternativeVersion.ToString()))
					{
						animeDetail.AlternativeVersions = new List<AnimeSummary>();

						SetAnimeSummaryList(animeDetail.AlternativeVersions, alternativeVersion.ToString());
					}
				}
			}

			var watchedStatusNode = document.DocumentNode.SelectSingleNode("//select[@id='myinfo_status']");

			if (watchedStatusNode != null)
			{
				var selectedOption =
					 watchedStatusNode.ChildNodes.Where(c => c.Name.ToLowerInvariant() == "option");

				var selected = from c in selectedOption
							   from x in c.Attributes
							   where x.Name.ToLowerInvariant() == "selected"
							   select c;

				if (selected.FirstOrDefault() != null)
					animeDetail.WatchedStatus = selected.FirstOrDefault().NextSibling.InnerText;
			}

			var watchedEpisodeNode = document.DocumentNode.SelectSingleNode("//input[@id='myinfo_watchedeps']");

			if (watchedEpisodeNode != null)
			{
				var value =
					 watchedEpisodeNode.Attributes.FirstOrDefault(c => c.Name.ToLowerInvariant() == "value");

				if (value != null)
				{
					int watched;

					if (Int32.TryParse(value.Value, out watched))
						animeDetail.WatchedEpisodes = watched;
					else
					{
						animeDetail.WatchedEpisodes = 0;
					}
				}
			}

			var myScoreNode = document.DocumentNode.SelectSingleNode("//select[@id='myinfo_score']");

			if (myScoreNode != null)
			{
				var selectedOption =
					 myScoreNode.ChildNodes.Where(c => c.Name.ToLowerInvariant() == "option");

				var selected = from c in selectedOption
							   from x in c.Attributes
							   where x.Name.ToLowerInvariant() == "selected"
							   select c;

				if (selected.FirstOrDefault() != null)
				{
					var scoreNode = from c in selected.FirstOrDefault().Attributes
									where c.Name.ToLowerInvariant() == "value"
									select c;

					var score = scoreNode.FirstOrDefault();

					if (score != null)
						animeDetail.Score = score.Value == null ? 0 : Convert.ToInt32(score.Value);
				}
			}

			var editDetailNode = document.DocumentNode.SelectSingleNode("//a[text()='Edit Details']");

			if (editDetailNode != null)
			{
				var hrefValue = editDetailNode.Attributes["href"].Value;

				var regex = Regex.Match(hrefValue, @"\d+");

				animeDetail.ListedAnimeId = Convert.ToInt32(regex.ToString());
			}

			return animeDetail;
		}

		public async Task<bool> AddAnimeAsync(string login, string password, int animeId, string status, int watchedEpisodes, int score)
		{
			var data = CreateAnimeValue(GetWatchStatus(status), watchedEpisodes, score);

			RestClient.BaseUrl = new Uri(string.Format("http://myanimelist.net/api/animelist/add/{0}.xml", animeId));

			var request = GetRestRequest(Method.POST);

			request.Credentials = new NetworkCredential(login, password);

			request.AddParameter("data", data);

			var response = await RestClient.ExecuteTaskAsync(request).ConfigureAwait(false);

			if (response.ErrorException != null)
				throw response.ErrorException;

			if (response.Content.ToLowerInvariant() == "this anime is already on your list.")
			{
				return false;
			}

			if (response.StatusCode == HttpStatusCode.Created)
			{
				return true;
			}

			HttpRequestHelper.HandleHttpCodes(response.StatusCode);

			return true;
		}

		private string CreateAnimeValue(int status, int watchedEpisodes, int score)
		{
			var xml = new StringBuilder();
			//if values are not set they're reseted on MAL
			xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			xml.AppendLine("<entry>");
			xml.AppendLine("<episode>" + watchedEpisodes + "</episode>");
			xml.AppendLine("<status>" + status + "</status>");
			xml.AppendLine("<score>" + score + "</score>");
			//xml.AppendLine("<download_episodes></download_episodes>");
			//xml.AppendLine("<storage_type></storage_type>");
			//xml.AppendLine("<storage_value></storage_value>");
			//xml.AppendLine("<times_rewatched></times_rewatched>");
			//xml.AppendLine("<rewatch_value></rewatch_value>");
			//xml.AppendLine("<date_start></date_start>");
			//xml.AppendLine("<date_finish></date_finish>");
			//xml.AppendLine("<priority></priority>");
			//xml.AppendLine("<enable_discussion></enable_discussion>");
			//xml.AppendLine("<enable_rewatching></enable_rewatching>");
			//xml.AppendLine("<comments></comments>");
			//xml.AppendLine("<fansub_group></fansub_group>");
			//xml.AppendLine("<tags></tags>");
			xml.AppendLine("</entry>");


			return xml.ToString();
		}



		public async Task<bool> UpdateAnimeAsync(string login, string password, int animeId, string status, int watchedEpisodes, int score)
		{
			var data = CreateAnimeValue(GetWatchStatus(status), watchedEpisodes, score);

			RestClient.BaseUrl = new Uri(string.Format("http://myanimelist.net/api/animelist/update/{0}.xml", animeId));

			var request = GetRestRequest(Method.POST);

			request.Credentials = new NetworkCredential(login, password);

			request.AddParameter("data", data);

			var response = await RestClient.ExecuteTaskAsync(request).ConfigureAwait(false);

			if (response.ErrorException != null)
				throw response.ErrorException;

			if (response.Content.ToLowerInvariant() == "updated")
			{
				return true;
			}

			HttpRequestHelper.HandleHttpCodes(response.StatusCode);

			return true;
		}

		public Task<List<AnimeDetailSearchResult>> SearchAnimeAsync(string searchCriteria)
		{
			throw new NotImplementedException();
		}

		public async Task<List<AnimeDetailSearchResult>> SearchAnimeAsync(string login, string password, string searchCriteria)
		{
			RestClient.BaseUrl = new Uri("http://myanimelist.net/api/anime/search.xml");

			var request = GetRestRequest(Method.GET);

			request.Credentials = new NetworkCredential(login, password);

			request.AddParameter("q", searchCriteria);

			var result = await ExecuteTaskASync(request).ConfigureAwait(false);

			result = result.Replace("~", "");

			try
			{
				//Weird method to remove special html char and then re-add some of them with the for loop to parse the xml... (for '&' value)
				result = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(result));

				string resultEncoded = "";
				for (int i = 0; i < result.Length; i++)
				{
					if (result[i] == '<' || result[i] == '>' || result[i] == '"')
					{
						resultEncoded += result[i];
					}
					else
					{
						resultEncoded += HttpUtility.HtmlEncode(result[i].ToString());
					}
				}

				string regExp = @"</(\w+)>";
				MatchCollection mc = Regex.Matches(resultEncoded, regExp);
				foreach (Match m in mc)
				{
					string val = m.Groups[1].Value;
					string regExp2 = "<" + val + "( |>)";
					Match m2 = Regex.Match(resultEncoded, regExp2);
					if (m2.Success)
					{
						char[] chars = resultEncoded.ToCharArray();
						chars[m2.Index] = '~';
						resultEncoded = new string(chars);
						resultEncoded = Regex.Replace(resultEncoded, @"</" + val + ">", "~/" + val + ">");
					}

					resultEncoded = Regex.Replace(resultEncoded, @"<\?", @"~?"); // declarations
					resultEncoded = Regex.Replace(resultEncoded, @"<!", @"~!");   // comments
				}

				string regExp3 = @"<\w+\s?/>";
				Match m3 = Regex.Match(resultEncoded, regExp3);
				if (m3.Success)
				{
					char[] chars = resultEncoded.ToCharArray();
					chars[m3.Index] = '~';
					resultEncoded = new string(chars);
				}
				resultEncoded = Regex.Replace(resultEncoded, "<", "&lt;");
				resultEncoded = Regex.Replace(resultEncoded, "~", "<");
				resultEncoded = Regex.Replace(resultEncoded, " & ", " and ");


				var xDocument = XDocument.Parse(resultEncoded).Root;

				var mangaDetailSearchResults = new List<AnimeDetailSearchResult>();


				foreach (var mangaElement in xDocument.Elements("entry"))
				{
					var searchResult = new AnimeDetailSearchResult
					{
						Title = mangaElement.Element("title").Value,
						MembersScore = 0,
						Type = mangaElement.Element("type").Value,
						ImageUrl = mangaElement.Element("image").Value,
						Synopsis = HttpUtility.HtmlDecode(mangaElement.Element("synopsis").Value),
						Episodes = mangaElement.ElementValue("episodes", 0),
						Id = mangaElement.ElementValue("id", 0)
					};

					mangaDetailSearchResults.Add(searchResult);
				}

				return mangaDetailSearchResults;
			}
			catch (XmlException exception)
			{
				throw new ServiceException(Resource.ServiceUnableToPerformActionException, exception.InnerException);
			}
		}

		public async Task<List<AnimeDetail>> FindTopAnimeAsync(int pageNumber, TopAnimeType topAnimeType)
		{
			//Need to be reviewed !!!


			RestClient.BaseUrl = new Uri("http://myanimelist.net/topanime.php");

			var request = GetRestRequest(Method.GET);

			var animeType = GetTopAnimeType(topAnimeType);

			if (!string.IsNullOrEmpty(animeType))
				request.AddParameter("type", animeType);

			if (pageNumber > 0)
			{
				var limit = pageNumber * 30;

				request.AddParameter("limit", limit);
			}

			var animeDetails = new List<AnimeDetail>();

			var result = await ExecuteTaskASync(request).ConfigureAwait(false);

			var document = new HtmlAgilityPack.HtmlDocument();

			document.LoadHtml(result);

			var pageContent = document.DocumentNode.SelectNodes("//div[@id='content']//table//tr");

			foreach (var row in pageContent)
			{
				var animeTitleNode = row.SelectSingleNode("//td//a//strong");

				if (animeTitleNode == null)
					continue;

				var animeUrl = animeTitleNode.ParentNode.Attributes["href"].Value;

				if (!animeUrl.Contains("myanimelist.net/anime"))
					continue;

				var animeDetail = new AnimeDetail();

				var stringToParse = animeUrl.Replace("http://myanimelist.net/anime/", "");

				var animeIdString = stringToParse.Substring(0, stringToParse.IndexOf("/", StringComparison.Ordinal));

				int animeId;

				if (Int32.TryParse(animeIdString, out animeId))
				{
					animeDetail.Id = animeId;
				}
				else
				{
					animeDetail.Id = 0;
				}


				animeDetail.Title = HttpUtility.HtmlDecode(animeTitleNode.InnerText);

				var contentCell = row.SelectSingleNode("//div[@class='spaceit_pad']");

				if (contentCell != null)
				{
					//Regex.Replace(classification.NextSibling.InnerText, @"\t|\n|\r", "").Trim();
					var memberCell = contentCell.SelectSingleNode("//div[@class='spaceit_pad']//span[@class='lightLink']");


					if (memberCell != null)
					{
						var membersString = memberCell.InnerText.Substring(0, memberCell.InnerText.IndexOf(" ", StringComparison.Ordinal)).Trim();

						int memberCount;

						if (Int32.TryParse(membersString.Replace(",", ""), out memberCount))
						{
							animeDetail.MembersCount = memberCount;
						}
					}

					var stats = Regex.Replace(contentCell.ChildNodes.First().InnerText, @"\t|\n|\r", "").Trim().Split(',');

					animeDetail.Type = stats[0];

					var episodesString = stats[1].Substring(0, stats[1].IndexOf("eps", StringComparison.Ordinal)).Trim();

					int episodesCount;

					if (Int32.TryParse(episodesString, out episodesCount))
					{
						animeDetail.Episodes = episodesCount;
					}

					var scoreString = Regex.Match(stats[2], @"\d+\.\d+");

					double score;

					if (Double.TryParse(scoreString.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out score))
					{
						animeDetail.MembersScore = score;
					}
				}

			}

			return animeDetails;
		}

		public async Task<bool> DeleteAnimeAsync(string login, string password, int animeId)
		{
			RestClient.BaseUrl = new Uri(string.Format("http://myanimelist.net/api/animelist/delete/{0}.xml", animeId));

			var request = GetRestRequest(Method.DELETE);

			request.Credentials = new NetworkCredential(login, password);

			var result = await ExecuteTaskASync(request).ConfigureAwait(false);

			if (!string.IsNullOrEmpty(result) && result.ToLowerInvariant() == "deleted")
				return true;

			return false;
		}

		private string GetTopAnimeType(TopAnimeType topAnimeType)
		{
			switch (topAnimeType)
			{
				case TopAnimeType.ByPopularity:
					return "bypopularity";

				case TopAnimeType.Movie:
					return "movie";
				case TopAnimeType.Ova:
					return "ova";
				case TopAnimeType.Special:
					return "special";
				case TopAnimeType.Tv:
					return "tv";
				case TopAnimeType.All:
					return string.Empty;
				default:
					return string.Empty;
			}
		}

		private string GetType(int type)
		{
			switch (type)
			{
				case 1:
					return "TV";

				case 2:
					return "OVA";

				case 3:
					return "Movie";

				case 4:
					return "Special";

				case 5:
					return "ONA";

				case 6:
					return "Music";

				default:
					return "TV";
			}
		}

		private string GetStatus(int status)
		{
			switch (status)
			{
				case 1:
					return "currently airing";

				case 2:
					return "finished airing";

				case 3:
					return "not yet aired";

				default:
					return "finished airing";
			}
		}

		private string GetWatchStatus(int status)
		{
			switch (status)
			{
				case 1:
					return "watching";

				case 2:
					return "completed";

				case 3:
					return "on-hold";

				case 4:
					return "dropped";
				case 6:
					return "plan to watch";

				default:
					return "watching";
			}
		}

		private int GetWatchStatus(string status)
		{
			switch (status.ToLowerInvariant())
			{
				case "watching":
					return 1;

				case "completed":
					return 2;

				case "on-hold":
					return 3;

				case "dropped":
					return 4;
				case "plan to watch":
					return 6;

				default:
					return 1;
			}
		}

		private void SetMangaSummaryList(List<MangaSummary> mangaSummaries, string htmlContent)
		{
			var relatedDocument = new HtmlAgilityPack.HtmlDocument();

			relatedDocument.LoadHtml(htmlContent);

			foreach (var alternativeNode in relatedDocument.DocumentNode.ChildNodes.Where(c => c.Name == "a").Select(x => x))
			{
				var mangaSummary = new MangaSummary
				{
					Url = alternativeNode.Attributes["href"].Value,
					Title = alternativeNode.InnerText
				};

				var stringToParse = alternativeNode.Attributes["href"].Value.Replace(
					 "/manga/", "");


				var mangaIdString = stringToParse.Substring(0, stringToParse.IndexOf("/", StringComparison.Ordinal));

				int mangaAlternativeId;

				if (Int32.TryParse(mangaIdString, out mangaAlternativeId))
				{
					mangaSummary.MangaId = mangaAlternativeId;
				}
				else
				{
					mangaSummary.MangaId = 0;
				}

				mangaSummaries.Add(mangaSummary);
			}
		}

		private void SetAnimeSummaryList(List<AnimeSummary> animeSummaries, string htmlContent)
		{
			var relatedDocument = new HtmlAgilityPack.HtmlDocument();

			relatedDocument.LoadHtml(htmlContent);

			foreach (var alternativeNode in relatedDocument.DocumentNode.SelectNodes("//a[@href]").Select(x => x))
			{
				var animeSummary = new AnimeSummary
				{
					Url = alternativeNode.Attributes["href"].Value,
					Title = alternativeNode.InnerText
				};

				var stringToParse = alternativeNode.Attributes["href"].Value.Replace(
					 "/anime/", "");


				var mangaIdString = stringToParse.Substring(0, stringToParse.IndexOf("/", StringComparison.Ordinal));

				int mangaAlternativeId;

				if (Int32.TryParse(mangaIdString, out mangaAlternativeId))
				{
					animeSummary.AnimeId = mangaAlternativeId;
				}
				else
				{
					animeSummary.AnimeId = 0;
				}

				animeSummaries.Add(animeSummary);
			}
		}

		private AnimeSummary SetAnimeSummaryList(string htmlContent)
		{
			var relatedDocument = new HtmlAgilityPack.HtmlDocument();

			relatedDocument.LoadHtml(htmlContent);

			var alternativeNode = relatedDocument.DocumentNode.SelectNodes("//a[@href]").FirstOrDefault();

			var animeSummary = new AnimeSummary
			{
				Url = alternativeNode.Attributes["href"].Value,
				Title = alternativeNode.InnerText
			};

			var stringToParse = alternativeNode.Attributes["href"].Value.Replace(
				 "http://myanimelist.net/anime/", "");

			//Sometimes the url does not contain the domain name... 
			stringToParse = stringToParse.Replace("/anime/", "");


			var mangaIdString = stringToParse.Substring(0, stringToParse.IndexOf("/", StringComparison.Ordinal));

			int mangaAlternativeId;

			if (Int32.TryParse(mangaIdString, out mangaAlternativeId))
			{
				animeSummary.AnimeId = mangaAlternativeId;
			}
			else
			{
				animeSummary.AnimeId = 0;
			}

			return animeSummary;
		}
	}
}