using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RestSharp;

namespace MyAnimeList.Wrapper.Services
{
	public class BaseService
	{
		protected RestClient RestClient;
		public string UserAgent { get; set; }

		protected BaseService(string userAgent)
		{
			RestClient = new RestClient { UserAgent = userAgent };

			UserAgent = userAgent;
		}

		protected IRestRequest GetRestRequest(Method method)
		{
			var request = new RestRequest(method);

			return request;
		}

		protected IRestRequest GetRestRequest(Method method, List<Cookie> cookies)
		{
			var request = GetRestRequest(method);
			
			foreach (var cookie in cookies)
			{
				request.AddCookie(cookie.Name, cookie.Value);
			}
			return request;
		}

		protected async Task<string> ExecuteTaskASync(IRestRequest request)
		{
			//Workaround to invalidate WebRequest cache from client
			request.AddParameter("no-cache", Guid.NewGuid());

			var response = await RestClient.ExecuteTaskAsync(request);

			if (response.ErrorException != null)
				throw response.ErrorException;

			HttpRequestHelper.HandleHttpCodes(response.StatusCode);

			return response.Content;
		}


	}
}