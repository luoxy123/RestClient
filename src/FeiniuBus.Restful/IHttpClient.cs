using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FeiniuBus.Restful
{
    public interface IHttpClient
    {
        string BaseUri { get; set; }
        string ContentType { get; set; }
        string Accept { get; set; }
        string BearerToken { get; set; }
        bool EnableCompression { get; set; }

        Task<TResponse> SendAsync<TResponse>(HttpMethod httpMethod, string absoluteUrl, object request,
            CancellationToken token = default(CancellationToken));

        void AddHttpRequestHeader(string key, string value);

        Action<HttpRequestMessage> RequestFilter { get; set; }
    }
}
