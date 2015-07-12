using System;
using System.Threading.Tasks;
using RestSharp;

namespace MyAnimeList.Wrapper
{
    public static class RestClientExtensions
    {
        public static Task<IRestResponse> ExecuteTaskAsync(this IRestClient client, IRestRequest request)
        {
            if (client == null)
                throw new NullReferenceException();

            var tcs = new TaskCompletionSource<IRestResponse>();

            client.ExecuteAsync(request, response =>
            {
                if (response.ErrorException != null)
                    tcs.TrySetException(response.ErrorException);
                else
                    tcs.TrySetResult(response);
            });

            return tcs.Task;
        }

        public static Task<IRestResponse<T>> ExecuteTaskAsync<T>(this IRestClient client, IRestRequest request) where T : new()
        {
            if (client == null)
                throw new NullReferenceException();

            var tcs = new TaskCompletionSource<IRestResponse<T>>();

            client.ExecuteAsync<T>(request, response =>
            {
                if (response.ErrorException != null)
                    tcs.TrySetException(response.ErrorException);
                else
                    tcs.TrySetResult(response);
            });

            return tcs.Task;
        }
    }
}