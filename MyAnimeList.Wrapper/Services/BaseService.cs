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
        protected List<Cookie> Cookies;

        protected BaseService(string userAgent)
        {
            RestClient = new RestClient { UserAgent = userAgent };

            Cookies = new List<Cookie>();
        }

        protected IRestRequest GetRestRequest(Method method)
        {
            var request = new RestRequest(method);

            return request;
        }

        protected IRestRequest GetRestRequest(Method method, List<Cookie> cookies)
        {
            var request = GetRestRequest(method);

            Cookies.Clear();

            foreach (var cookie in cookies)
            {
                request.AddCookie(cookie.Name, cookie.Value);
                Cookies.Add(cookie);
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