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
using MyAnimeList.Wrapper.Model.Manga;
using MyAnimeList.Wrapper.Resources;
using MyAnimeList.Wrapper.ServicesContracts;
using RestSharp;

namespace MyAnimeList.Wrapper.Services
{
	public class MangaService : BaseService, IMangaService
	{
		public MangaService(string userAgent)
			: base(userAgent)
		{
		}

		#region Service methods

		public async Task<MangaRoot> FindMangaListAsync(string login)
		{
			RestClient.BaseUrl = new Uri("http://myanimelist.net/malappinfo.php");

			var request = GetRestRequest(Method.GET);

			request.AddParameter("u", login);
			request.AddParameter("status", "all");
			request.AddParameter("type", "manga");

			var result = await ExecuteTaskASync(request).ConfigureAwait(false);

			try
			{
				var xDocument = XDocument.Parse(result).Root;

				var mangaRoot = new MangaRoot { Manga = new List<Manga>(), Statistics = new Statistics() };

				if (xDocument.Element("myinfo") != null && xDocument.Element("myinfo").Element("user_days_spent_watching") != null)
					mangaRoot.Statistics.Days = xDocument.Element("myinfo").ElementValue("user_days_spent_watching", 0.0d);

				foreach (var mangaElement in xDocument.Elements("manga"))
				{
					var manga = new Manga
					{
						Id = mangaElement.ElementValue("series_mangadb_id", 0),
						Title = mangaElement.Element("series_title").Value,
						Type = GetType(mangaElement.ElementValue("series_type", 0)),
						Status = GetStatus(mangaElement.ElementValue("series_status", 0)),
						Chapters = mangaElement.ElementValue("series_chapters", 0),
						Volumes = mangaElement.ElementValue("series_volumes", 0),
						ImageUrl = mangaElement.Element("series_image").Value,
						ListedMangaId = mangaElement.ElementValue("my_id", 0),
						VolumesRead = mangaElement.ElementValue("my_read_volumes", 0),
						ChaptersRead = mangaElement.ElementValue("my_read_chapters", 0),
						Score = mangaElement.ElementValue("my_score", 0),
						ReadStatus = GetReadStatus(mangaElement.ElementValue("my_status", 0))
					};
					mangaRoot.Manga.Add(manga);
				}

				return mangaRoot;
			}
			catch (XmlException exception)
			{
				throw new ServiceException(Resource.ServiceUnableToPerformActionException, exception.InnerException);
			}
		}

		public async Task<MangaDetail> GetMangaDetailAsync(string login, string password, int mangaId)
		{
			var cookies = await CookieHelper.GetCookies(login, password, UserAgent);

			RestClient.BaseUrl = new Uri(string.Format("http://myanimelist.net/manga/{0}", mangaId));

			var request = GetRestRequest(Method.GET, cookies);

			var result = await ExecuteTaskASync(request).ConfigureAwait(false);

			var mangaDetail = new MangaDetail();

			var document = new HtmlDocument();

			document.LoadHtml(result);

			SetId(document, mangaDetail);

			SetRank(document, mangaDetail);

			SetTitle(document, mangaDetail);

			SetImageUrl(document, mangaDetail);

			var leftColumnNodeset =
				document.DocumentNode.SelectSingleNode("//div[@id='content']//table//tr//td[@class='borderClass']");

			if (leftColumnNodeset != null)
			{
				SetAlternativeTitles(leftColumnNodeset, mangaDetail);

				SetType(document, mangaDetail);

				SetVolume(leftColumnNodeset, mangaDetail);

				SetChapter(leftColumnNodeset, mangaDetail);

				SetStatus(leftColumnNodeset, mangaDetail);

				SetGenre(leftColumnNodeset, mangaDetail);

				SetScore(leftColumnNodeset, mangaDetail);

				SetPopularityRank(leftColumnNodeset, mangaDetail);

				SetMember(leftColumnNodeset, mangaDetail);

				SetFavorite(leftColumnNodeset, mangaDetail);

				SetPopularTags(leftColumnNodeset, mangaDetail);
			}

			var rightColumnNodeset =
			  document.DocumentNode.SelectSingleNode("//div[@id='content']/table/tr/td/div/table");

			if (rightColumnNodeset != null)
			{
				SetSynopsis(rightColumnNodeset, mangaDetail);

				SetRelatedManga(rightColumnNodeset, mangaDetail);
			}

			SetReadStatus(document, mangaDetail);

			SetChaptersRead(document, mangaDetail);

			SetVolumesRead(document, mangaDetail);

			SetMyScore(document, mangaDetail);

			SetListedMangaId(document, mangaDetail);

			return mangaDetail;
		}

		public async Task<bool> AddMangaAsync(string login, string password, int mangaId, string status, int chaptersRead, int score)
		{
			var data = CreateMangaValue(GetReadStatus(status), chaptersRead, 0, score);

			RestClient.BaseUrl = new Uri(string.Format("http://myanimelist.net/api/mangalist/add/{0}.xml", mangaId));

			var request = GetRestRequest(Method.POST);

			request.Credentials = new NetworkCredential(login, password);

			request.AddParameter("data", data);

			var response = await RestClient.ExecuteTaskAsync(request).ConfigureAwait(false);

			if (response.ErrorException != null)
				throw response.ErrorException;

			if (response.Content.ToLowerInvariant() == "already added to your list.")
			{
				return false;
			}

			int insertedId;

			if (response.StatusCode == HttpStatusCode.Created && Int32.TryParse(response.Content, out insertedId))
			{
				return true;
			}

			HttpRequestHelper.HandleHttpCodes(response.StatusCode);

			return true;
		}

		public async Task<bool> UpdateMangaAsync(string login, string password, int mangaId, string status, int chaptersRead, int volume, int score)
		{
			var data = CreateMangaValue(GetReadStatus(status), chaptersRead, volume, score);

			RestClient.BaseUrl = new Uri(string.Format("http://myanimelist.net/api/mangalist/update/{0}.xml", mangaId));

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

		public Task<List<MangaDetailSearchResult>> SearchMangaAsync(string searchCriteria)
		{
			throw new System.NotImplementedException();
		}

		public async Task<List<MangaDetailSearchResult>> SearchMangaAsync(string login, string password, string searchCriteria)
		{
			RestClient.BaseUrl = new Uri("http://myanimelist.net/api/manga/search.xml");

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

				var mangaDetailSearchResults = new List<MangaDetailSearchResult>();


				foreach (var mangaElement in xDocument.Elements("entry"))
				{
					var searchResult = new MangaDetailSearchResult
					{
						Title = mangaElement.Element("title").Value,
						MembersScore = 0,
						Type = mangaElement.Element("type").Value,
						ImageUrl = mangaElement.Element("image").Value,
						Synopsis = HttpUtility.HtmlDecode(mangaElement.Element("synopsis").Value),
						Volumes = 0,
						Id = mangaElement.ElementValue("id", 0),
						Chapters = 0
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

		public async Task<bool> DeleteMangaAsync(string login, string password, int mangaId)
		{
			RestClient.BaseUrl = new Uri(string.Format("http://myanimelist.net/api/mangalist/delete/{0}.xml", mangaId));

			var request = GetRestRequest(Method.DELETE);

			request.Credentials = new NetworkCredential(login, password);

			var result = await ExecuteTaskASync(request).ConfigureAwait(false);

			if (!string.IsNullOrEmpty(result) && result.ToLowerInvariant() == "deleted")
				return true;

			return false;
		}

		#endregion

		#region Helper methods

		private static void SetListedMangaId(HtmlDocument document, MangaDetail mangaDetail)
		{
			var editDetailNode = document.DocumentNode.SelectSingleNode("//a[text()='Edit Details']");

			if (editDetailNode != null)
			{
				var hrefValue = editDetailNode.Attributes["href"].Value;

				var regex = Regex.Match(hrefValue, @"\d+");

				mangaDetail.ListedMangaId = Convert.ToInt32(regex.ToString());
			}
		}

		private static void SetMyScore(HtmlDocument document, MangaDetail mangaDetail)
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

					if (score == null || score.Value == null)
						mangaDetail.Score = 0;
					else
						mangaDetail.Score = Convert.ToInt32(score.Value);
				}
			}
		}

		private static void SetVolumesRead(HtmlDocument document, MangaDetail mangaDetail)
		{
			var volumesReadNode = document.DocumentNode.SelectSingleNode("//input[@id='myinfo_volumes']");

			if (volumesReadNode != null)
			{
				var value =
					volumesReadNode.Attributes.FirstOrDefault(c => c.Name.ToLowerInvariant() == "value");

				if (value != null)
				{
					int volumesRead;

					if (Int32.TryParse(value.Value, out volumesRead))
						mangaDetail.VolumesRead = volumesRead;
					else
					{
						mangaDetail.VolumesRead = 0;
					}
				}
			}
		}

		private static void SetChaptersRead(HtmlDocument document, MangaDetail mangaDetail)
		{
			var chapthersReadNode = document.DocumentNode.SelectSingleNode("//input[@id='myinfo_chapters']");

			if (chapthersReadNode != null)
			{
				var value =
					chapthersReadNode.Attributes.FirstOrDefault(c => c.Name.ToLowerInvariant() == "value");

				if (value != null)
				{
					int chaptersRead;

					if (Int32.TryParse(value.Value, out chaptersRead))
						mangaDetail.ChaptersRead = chaptersRead;
					else
					{
						mangaDetail.ChaptersRead = 0;
					}
				}
			}
		}

		private static void SetReadStatus(HtmlDocument document, MangaDetail mangaDetail)
		{
			var readStatusNode = document.DocumentNode.SelectSingleNode("//select[@id='myinfo_status']");

			if (readStatusNode != null)
			{
				var selectedOption =
					readStatusNode.ChildNodes.Where(c => c.Name.ToLowerInvariant() == "option");

				var selected = from c in selectedOption
							   from x in c.Attributes
							   where x.Name.ToLowerInvariant() == "selected"
							   select c;

				if (selected.FirstOrDefault() != null)
					mangaDetail.ReadStatus = selected.FirstOrDefault().NextSibling.InnerText;
			}
		}

		private void SetRelatedManga(HtmlNode rightColumnNodeset, MangaDetail mangaDetail)
		{
			var relatedManga = rightColumnNodeset.SelectSingleNode("//h2[text()='Related Manga']");

			if (relatedManga != null)
			{
				//Alternative
				var alternativeVersion =
					Regex.Match(
						relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Alternative versions?:+(.+?<br)");

				if (!string.IsNullOrEmpty(alternativeVersion.ToString()))
				{
					mangaDetail.AlternativeVersions = new List<MangaSummary>();

					SetMangaSummaryList(mangaDetail.AlternativeVersions, alternativeVersion.ToString());
				}

				//Adaptation
				var adaptation =
					Regex.Match(
						relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Adaptation:+(.+?<br)");

				if (!string.IsNullOrEmpty(adaptation.ToString()))
				{
					mangaDetail.AnimeAdaptations = new List<AnimeSummary>();

					SetAnimeSummaryList(mangaDetail.AnimeAdaptations, adaptation.ToString());
				}


				//Related manga
				var prequel =
					Regex.Match(
						relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Prequel:+(.+?<br)");

				mangaDetail.RelatedManga = new List<MangaSummary>();

				if (!string.IsNullOrEmpty(prequel.ToString()))
				{
					SetMangaSummaryList(mangaDetail.RelatedManga, prequel.ToString());
				}

				var sequel =
					Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Sequel:+(.+?<br)");

				if (!string.IsNullOrEmpty(sequel.ToString()))
				{
					SetMangaSummaryList(mangaDetail.RelatedManga, sequel.ToString());
				}

				var parentStory =
					Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Parent story:+(.+?<br)");

				if (!string.IsNullOrEmpty(parentStory.ToString()))
				{
					SetMangaSummaryList(mangaDetail.RelatedManga, parentStory.ToString());
				}

				var sideStory =
					Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Side story:+(.+?<br)");


				if (!string.IsNullOrEmpty(sideStory.ToString()))
				{
					SetMangaSummaryList(mangaDetail.RelatedManga, sideStory.ToString());
				}

				var character =
					Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Character:+(.+?<br)");

				if (!string.IsNullOrEmpty(character.ToString()))
				{
					SetMangaSummaryList(mangaDetail.RelatedManga, character.ToString());
				}

				var spinOff =
					Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Spin-off:+(.+?<br)");


				if (!string.IsNullOrEmpty(spinOff.ToString()))
				{
					SetMangaSummaryList(mangaDetail.RelatedManga, spinOff.ToString());
				}

				var summary =
					Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
						"Summary:+(.+?<br)");

				if (!string.IsNullOrEmpty(summary.ToString()))
				{
					SetMangaSummaryList(mangaDetail.RelatedManga, summary.ToString());
				}
			}
		}

		private static void SetSynopsis(HtmlNode rightColumnNodeset, MangaDetail mangaDetail)
		{
			var synopsis = rightColumnNodeset.SelectSingleNode("//h2[text()='Synopsis']");

			if (synopsis != null)
			{
				mangaDetail.Synopsis = Regex.Replace(HttpUtility.HtmlDecode(synopsis.NextSibling.InnerText), "<br>", "");
			}
		}

		private static void SetPopularTags(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var popularTags = leftColumnNodeset.SelectSingleNode("//span[preceding-sibling::h2[text()='Popular Tags']]");

			if (popularTags != null)
			{
				mangaDetail.Tags = popularTags.ChildNodes.Where(c => c.Name == "a").Select(x => x.InnerText.Trim()).ToList();
			}
		}

		private static void SetFavorite(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var favorite = leftColumnNodeset.SelectSingleNode("//span[text()='Favorites:']");

			if (favorite != null)
			{
				int favoritedCount;
				if (Int32.TryParse(favorite.NextSibling.InnerText.Replace(",", ""), out favoritedCount))
					mangaDetail.FavoritedCount = favoritedCount;
				else
				{
					mangaDetail.FavoritedCount = null;
				}
			}
		}

		private static void SetMember(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var member = leftColumnNodeset.SelectSingleNode("//span[text()='Members:']");

			if (member != null)
			{
				int memberCount;
				if (Int32.TryParse(member.NextSibling.InnerText.Replace(",", ""), out memberCount))
					mangaDetail.MembersCount = memberCount;
				else
				{
					mangaDetail.MembersCount = null;
				}
			}
		}

		private static void SetPopularityRank(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var popularity = leftColumnNodeset.SelectSingleNode("//span[text()='Popularity:']");

			if (popularity != null)
			{
				int popularityRank;

				if (Int32.TryParse(popularity.NextSibling.InnerText.Replace("#", "").Replace(",", ""), out popularityRank))
					mangaDetail.PopularityRank = popularityRank;
				else
				{
					mangaDetail.PopularityRank = null;
				}
			}
		}

		private static void SetScore(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var score = leftColumnNodeset.SelectSingleNode("//span[text()='Score:']");

			if (score != null)
			{
				double memberScore;
				if (double.TryParse(score.NextSibling.NextSibling.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture,
					out memberScore))
					mangaDetail.MembersScore = memberScore;
				else
				{
					mangaDetail.MembersScore = 0;
				}
			}
		}

		private static void SetGenre(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var genre = leftColumnNodeset.SelectSingleNode("//span[text()='Genres:']");

			if (genre != null)
			{
				mangaDetail.Genres = genre.ParentNode.ChildNodes.Where(c => c.Name == "a").Select(x => x.InnerText.Trim()).ToList();
			}
		}

		private static void SetStatus(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var status = leftColumnNodeset.SelectSingleNode("//span[text()='Status:']");

			if (status != null)
				mangaDetail.Status = status.NextSibling.InnerText;
		}

		private static void SetChapter(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var chapter = leftColumnNodeset.SelectSingleNode("//span[text()='Chapters:']");

			if (chapter != null)
			{
				int chapters;
				if (Int32.TryParse(chapter.NextSibling.InnerText.Replace(",", ""), out chapters))
					mangaDetail.Chapters = chapters;
				else
				{
					mangaDetail.Chapters = null;
				}
			}
		}

		private static void SetVolume(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var volume = leftColumnNodeset.SelectSingleNode("//span[text()='Volumes:']");

			if (volume != null)
			{
				int volumes;
				if (Int32.TryParse(volume.NextSibling.InnerText.Replace(",", ""), out volumes))
					mangaDetail.Volumes = volumes;
				else
				{
					mangaDetail.Volumes = null;
				}
			}
		}

		private static void SetType(HtmlDocument document, MangaDetail mangaDetail)
		{
			var type = document.DocumentNode.SelectSingleNode("//span[contains(.,'Type:')]");

			if (type != null)
				mangaDetail.Type = type.NextSibling.NextSibling.InnerText.Trim();
		}

		private static void SetAlternativeTitles(HtmlNode leftColumnNodeset, MangaDetail mangaDetail)
		{
			var englishAlternative = leftColumnNodeset.SelectSingleNode("//span[text()='English:']");

			mangaDetail.OtherTitles = new OtherTitles();

			if (englishAlternative != null)
			{
				mangaDetail.OtherTitles.English = englishAlternative.NextSibling.InnerText.Split(',').Select(p => p.Trim()).ToList();
			}

			var japaneseAlternative = leftColumnNodeset.SelectSingleNode("//span[text()='Japanese:']");

			if (japaneseAlternative != null)
			{
				mangaDetail.OtherTitles.Japanese = japaneseAlternative.NextSibling.InnerText.Split(',').Select(p => p.Trim()).ToList();
			}
		}

		private static void SetImageUrl(HtmlDocument document, MangaDetail mangaDetail)
		{
			var imageNode = document.DocumentNode.SelectSingleNode("//div[@id='content']//tr//td//div//img");

			if (imageNode != null)
				mangaDetail.ImageUrl = imageNode.Attributes["src"].Value;
		}

		private static void SetTitle(HtmlDocument document, MangaDetail mangaDetail)
		{
			var titleNode = document.DocumentNode.SelectSingleNode("//span[@itemprop='name']");

			if (titleNode != null)
				mangaDetail.Title = HttpUtility.HtmlDecode(titleNode.InnerText.Trim());
		}

		private static void SetRank(HtmlDocument document, MangaDetail mangaDetail)
		{
			var rankNode = document.DocumentNode.SelectSingleNode("//span[contains(.,'Rank')]");

			if (rankNode != null)
			{
				if (rankNode.NextSibling.InnerText.ToUpper().Contains("N/A"))
					mangaDetail.Rank = 0;
				else
				{
					var regex = Regex.Match(rankNode.NextSibling.InnerText, @"\d+");
					mangaDetail.Rank = Convert.ToInt32(regex.ToString());
				}
			}
		}

		private static void SetId(HtmlDocument document, MangaDetail mangaDetail)
		{
			var mangaIdInput = document.DocumentNode.SelectSingleNode("//input[@name='mid']");

			if (mangaIdInput != null)
			{
				mangaDetail.Id = Convert.ToInt32(mangaIdInput.Attributes["value"].Value);
			}
			else
			{
				var detailLink = document.DocumentNode.SelectSingleNode("//a[text()='Details']");

				if (detailLink != null)
				{
					var regex = Regex.Match(detailLink.Attributes["href"].Value, @"\d+");
					mangaDetail.Id = Convert.ToInt32(regex.ToString());
				}
			}
		}

		private void SetMangaSummaryList(List<MangaSummary> mangaSummaries, string htmlContent)
		{
			var relatedDocument = new HtmlAgilityPack.HtmlDocument();

			relatedDocument.LoadHtml(htmlContent);

			foreach (var alternativeNode in relatedDocument.DocumentNode.SelectNodes("//a[@href]").Select(x => x))
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

		private string CreateMangaValue(int status, int chaptersRead, int volumesRead, int score)
		{
			var xml = new StringBuilder();

			//if values are not set they're reseted on MAL
			xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			xml.AppendLine("<entry>");
			xml.AppendLine("<chapter>" + chaptersRead + "</chapter>");
			xml.AppendLine("<volume>" + volumesRead + "</volume>");
			xml.AppendLine("<status>" + status + "</status>");
			xml.AppendLine("<score>" + score + "</score>");
			//xml.AppendLine("<downloaded_chapters></downloaded_chapters>");
			//xml.AppendLine("<times_reread></times_reread>");
			//xml.AppendLine("<reread_value></reread_value>");
			//xml.AppendLine("<date_start></date_start>");
			//xml.AppendLine("<date_finish></date_finish>");
			//xml.AppendLine("<priority></priority>");
			//xml.AppendLine("<enable_discussion></enable_discussion>");
			//xml.AppendLine("<enable_rereading></enable_rereading>");
			//xml.AppendLine("<comments></comments>");
			//xml.AppendLine("<scan_group></scan_group>");
			//xml.AppendLine("<tags></tags>");
			//xml.AppendLine("<retail_volumes></retail_volumes>");
			xml.AppendLine("</entry>");


			return xml.ToString();
		}

		private string GetStatus(int status)
		{
			switch (status)
			{
				case 1:
					return "publishing";

				case 2:
					return "finished";

				case 3:
					return "not yet published";

				default:
					return "finished";
			}
		}

		private string GetReadStatus(int status)
		{
			switch (status)
			{
				case 1:
					return "reading";

				case 2:
					return "completed";

				case 3:
					return "on-hold";

				case 4:
					return "dropped";
				case 6:
					return "plan to read";

				default:
					return "reading";
			}
		}

		private int GetReadStatus(string status)
		{
			switch (status.ToLowerInvariant())
			{
				case "reading":
					return 1;

				case "completed":
					return 2;

				case "on-hold":
					return 3;

				case "dropped":
					return 4;

				case "plan to read":
					return 6;

				default:
					return 6;
			}
		}

		private string GetType(int type)
		{
			switch (type)
			{
				case 1:
					return "Manga";

				case 2:
					return "Novel";

				case 3:
					return "One Shot";

				case 4:
					return "Doujin";

				case 5:
					return "Manwha";

				case 6:
					return "Manhua";

				case 7:
					// Original English Language
					return "OEL";

				default:
					return "Manga";
			}
		} 

		#endregion
	}
}