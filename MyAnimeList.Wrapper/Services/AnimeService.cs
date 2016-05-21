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
using HtmlAgilityPack;
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

		#region Service methods

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

			var document = new HtmlDocument();

			document.LoadHtml(result);

			SetId(document, animeDetail);

			SetRank(document, animeDetail);

			SetTitle(document, animeDetail);

			SetImageUrl(document, animeDetail);

			var leftColumnNodeset =
				 document.DocumentNode.SelectSingleNode("//div[@id='content']//table//tr//td[@class='borderClass']");

			if (leftColumnNodeset != null)
			{
				SetAnimeAlternativeTitles(leftColumnNodeset, animeDetail);

				SetType(document, animeDetail);

				SetNumberOfEpisodes(leftColumnNodeset, animeDetail);

				SetStatus(leftColumnNodeset, animeDetail);

				SetAiredDate(leftColumnNodeset, animeDetail);

				SetGenres(leftColumnNodeset, animeDetail);

				SetClassification(leftColumnNodeset, animeDetail);

				SetScore(leftColumnNodeset, animeDetail);

				SetPopularityRank(leftColumnNodeset, animeDetail);

				SetMembers(leftColumnNodeset, animeDetail);

				SetFavorite(leftColumnNodeset, animeDetail);

				SetPopularTags(leftColumnNodeset, animeDetail);
			}

			var rightColumnNodeset = document.DocumentNode.SelectSingleNode("//h2[text()='Synopsis']").ParentNode.ParentNode.ParentNode;

			if (rightColumnNodeset != null)
			{
				SetSynopsis(rightColumnNodeset, animeDetail);

				SetRelatedAnime(rightColumnNodeset, animeDetail);
			}

			SetWatchedStatus(document, animeDetail);

			SetWatchedEpisode(document, animeDetail);

			SetMyScore(document, animeDetail);

			SetListedAnimeId(document, animeDetail);

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

			var document = new HtmlDocument();

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

		#endregion

		#region Helper methods

		private static void SetListedAnimeId(HtmlDocument document, AnimeDetail animeDetail)
		{
			var editDetailNode = document.DocumentNode.SelectSingleNode("//a[text()='Edit Details']");

			if (editDetailNode != null)
			{
				var hrefValue = editDetailNode.Attributes["href"].Value;

				var regex = Regex.Match(hrefValue, @"\d+");

				animeDetail.ListedAnimeId = Convert.ToInt32(regex.ToString());
			}
		}

		private static void SetMyScore(HtmlDocument document, AnimeDetail animeDetail)
		{
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
		}

		private static void SetWatchedEpisode(HtmlDocument document, AnimeDetail animeDetail)
		{
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
		}

		private static void SetWatchedStatus(HtmlDocument document, AnimeDetail animeDetail)
		{
			var watchedStatusNode =
				document.DocumentNode.SelectNodes("//select[@id='myinfo_status']")
					.FirstOrDefault(c => c.InnerHtml.ToUpper().Contains("SELECTED"));

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
		}

		private void SetRelatedAnime(HtmlNode rightColumnNodeset, AnimeDetail animeDetail)
		{
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

				if (!string.IsNullOrEmpty(summary.ToString()) && !summary.ToString().Contains("Summary:<br"))
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

		private static void SetSynopsis(HtmlNode rightColumnNodeset, AnimeDetail animeDetail)
		{
			var synopsis = rightColumnNodeset.SelectSingleNode("//h2[text()='Synopsis']");

			if (synopsis != null)
			{
				animeDetail.Synopsis = Regex.Replace(HttpUtility.HtmlDecode(synopsis.NextSibling.InnerText), "<br>", "");
			}
		}

		private static void SetPopularTags(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
			var popularTags = leftColumnNodeset.SelectSingleNode("//span[preceding-sibling::h2[text()='Popular Tags']]");

			if (popularTags != null)
			{
				animeDetail.Tags = popularTags.ChildNodes.Where(c => c.Name == "a").Select(x => x.InnerText.Trim()).ToList();
			}
		}

		private static void SetFavorite(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
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
		}

		private static void SetMembers(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
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
		}

		private static void SetPopularityRank(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
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
		}

		private static void SetScore(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
			var score = leftColumnNodeset.SelectSingleNode("//span[text()='Score:']");

			if (score != null)
			{
				double memberScore;
				if (double.TryParse(score.NextSibling.NextSibling.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture,
					out memberScore))
					animeDetail.MembersScore = memberScore;
				else
				{
					animeDetail.MembersScore = 0;
				}
			}
		}

		private static void SetClassification(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
			var classification = leftColumnNodeset.SelectSingleNode("//span[text()='Rating:']");

			if (classification != null)
			{
				animeDetail.Classification = Regex.Replace(classification.NextSibling.InnerText, @"\t|\n|\r", "").Trim();
			}
		}

		private static void SetGenres(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
			var genre = leftColumnNodeset.SelectSingleNode("//span[text()='Genres:']");

			if (genre != null)
			{
				animeDetail.Genres = genre.ParentNode.ChildNodes.Where(c => c.Name == "a").Select(x => x.InnerText.Trim()).ToList();
			}
		}

		private static void SetAiredDate(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
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
					var startDateText = airDateText.IndexOf("to") == -1
						? airDateText.Trim()
						: airDateText.Substring(0, airDateText.IndexOf("to")).Trim();

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
		}

		private static void SetStatus(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
			var status = leftColumnNodeset.SelectSingleNode("//span[text()='Status:']");

			if (status != null)
				animeDetail.Status = status.NextSibling.InnerText;
		}

		private static void SetNumberOfEpisodes(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
		{
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
		}

		private static void SetType(HtmlDocument document, AnimeDetail animeDetail)
		{
			var type = document.DocumentNode.SelectSingleNode("//span[contains(.,'Type:')]");

			if (type != null)
				animeDetail.Type = type.NextSibling.NextSibling.InnerText.Trim();
		}

		private static void SetAnimeAlternativeTitles(HtmlNode leftColumnNodeset, AnimeDetail animeDetail)
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
		}

		private static void SetImageUrl(HtmlDocument document, AnimeDetail animeDetail)
		{
			var imageNode = document.DocumentNode.SelectSingleNode("//div[@id='content']//tr//td//div//img");

			if (imageNode != null)
				animeDetail.ImageUrl = imageNode.Attributes["src"].Value;
		}

		private static void SetTitle(HtmlDocument document, AnimeDetail animeDetail)
		{
			var titleNode = document.DocumentNode.SelectSingleNode("//span[@itemprop='name']");


			if (titleNode != null)
				animeDetail.Title = HttpUtility.HtmlDecode(titleNode.InnerText.Trim());
		}

		private static void SetRank(HtmlDocument document, AnimeDetail animeDetail)
		{
			var rankNode = document.DocumentNode.SelectSingleNode("//span[contains(.,'Rank')]");

			if (rankNode != null)
			{
				if (rankNode.NextSibling.InnerText.ToUpper().Contains("N/A"))
					animeDetail.Rank = 0;
				else
				{
					var regex = Regex.Match(rankNode.NextSibling.InnerText, @"\d+");
					animeDetail.Rank = Convert.ToInt32(regex.ToString());
				}
			}
		}

		private static void SetId(HtmlDocument document, AnimeDetail animeDetail)
		{
			var animeIdInput = document.DocumentNode.SelectSingleNode("//input[@name='aid']");

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
			var relatedDocument = new HtmlDocument();

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
			var relatedDocument = new HtmlDocument();

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
			var relatedDocument = new HtmlDocument();

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

		#endregion
	}
}