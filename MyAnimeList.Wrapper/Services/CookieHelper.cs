using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using HtmlAgilityPack;
using Kulman.WPA81.BaseRestService.Services.Abstract;
using MyAnimeList.Wrapper.Resources;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace MyAnimeList.Wrapper.Services
{
	public class RestService : BaseRestService
	{
		private string _userAgent;

		public RestService(string userAgent)
		{
			_userAgent = userAgent;
		}

		protected override string GetBaseUrl()
		{
			return "http://myanimelist.net";
		}

		protected override Dictionary<string, string> GetRequestHeaders(string requestUrl)
		{
			var headers = new Dictionary<string, string>
						{
							{"Accept-Encoding", "gzip, deflate"},
							{"Accept", "application/json"},
							{"User-Agent", _userAgent }
						};
			return headers;
		}

		public async Task<HttpResponseMessage> GetResponse()
		{
			return await Get("http://myanimelist.net/login.php?cache=" + Guid.NewGuid());
		}
	}

	public class CookieHelper
	{
		public static List<Cookie> Cookies = new List<Cookie>();

		private static List<string> ConvertCookieHeaderToArrayList(string strCookHeader)
		{
			strCookHeader = strCookHeader.Replace("\r", "");
			strCookHeader = strCookHeader.Replace("\n", "");
			string[] strCookTemp = strCookHeader.Split(',');
			var al = new List<string>();
			int i = 0;
			int n = strCookTemp.Length;
			while (i < n)
			{
				if (strCookTemp[i].IndexOf("expires=", StringComparison.OrdinalIgnoreCase) > 0)
				{
					al.Add(strCookTemp[i] + "," + strCookTemp[i + 1]);
					i = i + 1;
				}
				else
				{
					al.Add(strCookTemp[i]);
				}
				i = i + 1;
			}
			return al;
		}

		private static CookieCollection ConvertCookieArraysToCookieCollection(List<string> al, string strHost)
		{
			CookieCollection cc = new CookieCollection();
			int alcount = al.Count;
			for (int i = 0; i < alcount; i++)
			{
				string strEachCook = al[i].ToString();
				string[] strEachCookParts = strEachCook.Split(';');
				int intEachCookPartsCount = strEachCookParts.Length;

				Cookie cookTemp = new Cookie();
				for (int j = 0; j < intEachCookPartsCount; j++)
				{
					if (j == 0)
					{
						string strCNameAndCValue = strEachCookParts[j];
						if (strCNameAndCValue != string.Empty)
						{
							int firstEqual = strCNameAndCValue.IndexOf("=", StringComparison.InvariantCultureIgnoreCase);
							string firstName = strCNameAndCValue.Substring(0, firstEqual);
							string allValue = strCNameAndCValue.Substring(firstEqual + 1, strCNameAndCValue.Length - (firstEqual + 1));
							cookTemp.Name = firstName.Replace(" ", "");
							Encoding iso = Encoding.GetEncoding("utf-8");//may be utf-8
							allValue = HttpUtility.UrlEncode(allValue);
							cookTemp.Value = allValue;
						}
						continue;
					}
					string strPNameAndPValue;
					string[] nameValuePairTemp;
					if (strEachCookParts[j].IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						strPNameAndPValue = strEachCookParts[j];
						if (strPNameAndPValue != string.Empty)
						{
							nameValuePairTemp = strPNameAndPValue.Split('=');
							cookTemp.Path = nameValuePairTemp[1] != string.Empty ? nameValuePairTemp[1] : "/";
						}
						continue;
					}
					if (strEachCookParts[j].IndexOf("domain", StringComparison.OrdinalIgnoreCase) < 0)
						continue;
					strPNameAndPValue = strEachCookParts[j];

					if (strPNameAndPValue == string.Empty)
						continue;

					nameValuePairTemp = strPNameAndPValue.Split('=');
					//cookTemp.Domain = nameValuePairTemp[1] != string.Empty ? nameValuePairTemp[1] : strHost;
				}
				if (cookTemp.Path == string.Empty)
				{
					cookTemp.Path = "/";
				}
				if (cookTemp.Domain == string.Empty)
				{
					//cookTemp.Domain = strHost;
				}
				cc.Add(cookTemp);
			}
			return cc;
		}

		public static async Task GetCookies(string login, string password, string userAgent)
		{
			if (Cookies.Any())
				return;

			var myRestService = new RestService(userAgent);

			var httpResponseMessage = await myRestService.GetResponse();

			var cookieHeader = httpResponseMessage.Headers["Set-Cookie"];

			var content = await httpResponseMessage.Content.ReadAsStringAsync();

			var document = new HtmlDocument();

			document.LoadHtml(content);

			var csrfTokenNode = document.DocumentNode.SelectSingleNode("//meta[@name='csrf_token']");
			var csrfToken = csrfTokenNode.Attributes["content"].Value;

			HttpWebResponse response;

			response = null;

			try
			{
				//var cookieCollection = new CookieCollection();
				//var httpWebRequest = (HttpWebRequest)WebRequest.Create(@"http://myanimelist.net");
				//httpWebRequest.UserAgent = "api-MyAnimeList-2BDDAF54629E4708EF4694AB0FF6DB75";
				//httpWebRequest.Headers["User-Agent"] = "api-MyAnimeList-2BDDAF54629E4708EF4694AB0FF6DB75";
				////httpWebRequest.CookieContainer = new CookieContainer();
				////httpWebRequest.CookieContainer.Add(new Uri(@"http://myanimelist.net"), cookieCollection);
				//httpWebRequest.AllowAutoRedirect = false;
				//var webResponse = await httpWebRequest.GetResponseAsync();
				//var httpWebResponse = (HttpWebResponse)webResponse;

				//var setCookieHeader = httpWebResponse.Headers["Set-Cookie"];

				var cc = new CookieCollection();
				if (cookieHeader != string.Empty)
				{
					var al = ConvertCookieHeaderToArrayList(cookieHeader);
					cc = ConvertCookieArraysToCookieCollection(al, @"http://myanimelist.net");
				}


				var webRequest = (HttpWebRequest)WebRequest.Create("http://myanimelist.net/login.php");

				webRequest.CookieContainer = new CookieContainer();
				webRequest.CookieContainer.Add(new Uri("http://myanimelist.net/login.php"), cc);
				webRequest.Headers["Connection"] = "Keep-Alive";
				webRequest.Headers["Cache-Control"] = "max-age=0";
				webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
				webRequest.Headers["Origin"] = @"http://myanimelist.net";
				webRequest.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";
				webRequest.ContentType = "application/x-www-form-urlencoded";
				webRequest.Headers["Accept-Encoding"] = "gzip, deflate";
				webRequest.Headers["Accept-Language"] = "en-US,en;q=0.8,fr;q=0.6";
				webRequest.AllowAutoRedirect = false;

				webRequest.Method = "POST";

				string body = string.Format(@"user_name={0}&password={1}&submit=Login&csrf_token={2}", login, password, csrfToken);
				byte[] postBytes = Encoding.UTF8.GetBytes(body);
				webRequest.ContentLength = postBytes.Length;
				Stream stream = await webRequest.GetRequestStreamAsync();
				stream.Write(postBytes, 0, postBytes.Length);
				stream.Close();

				var responseAwait = await webRequest.GetResponseAsync();

				response = (HttpWebResponse)responseAwait;

			}
			catch (WebException e)
			{
				if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
			}
			catch (Exception ex)
			{
				if (response != null) response.Close();

			}

			if (response.StatusCode != HttpStatusCode.Found)
				throw new ServiceException(Resource.NotAuthenticated);

			var reponseCookies = response.Headers["Set-Cookie"];

			var cookies = new List<Cookie>();
			if (reponseCookies != null)
			{

				reponseCookies = reponseCookies.Replace("HttpOnly,", "");
				reponseCookies = reponseCookies.Replace("httponly,", "");
				var parts = reponseCookies.Split(';')
				 .Where(i => i.Contains("=")) // filter out empty values
				 .Select(i => i.Trim().Split('=')) // trim to remove leading blank
				 .Select(i => new { Name = i.First(), Value = i.Last() });

				foreach (var val in parts)
				{
					cookies.Add(new Cookie(val.Name, HttpUtility.UrlEncode(val.Value)));
				}

			}
			else
				throw new ServiceException(Resource.NotAuthenticated);

			
			foreach (var cookie in cookies)
			{
				Cookies.Add(cookie);
			}

		}
	}
}