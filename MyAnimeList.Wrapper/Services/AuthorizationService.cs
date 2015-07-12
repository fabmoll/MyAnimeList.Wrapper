using System;
using System.Net;
using System.Threading.Tasks;
using MyAnimeList.Wrapper.ServicesContracts;
using RestSharp;

namespace MyAnimeList.Wrapper.Services
{
    public class AuthorizationService : BaseService, IAuthorizationService
    {
        private const string CredentialsUrl = "http://myanimelist.net/api/account/verify_credentials.xml";

        public AuthorizationService(string userAgent)
            : base(userAgent)
        {
        }

        public async Task<bool> VerifyCredentialsAsync(string login, string password)
        {
            RestClient.BaseUrl = new Uri(CredentialsUrl);

            var request = GetRestRequest(Method.GET);

            request.Credentials = new NetworkCredential(login, password);

            await ExecuteTaskASync(request).ConfigureAwait(false);

            return true;
        }

    }
}