using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

namespace FeiniuBus.RestClient
{
    public interface IRestHttpClient : IHttpClient
    {
        Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request);

        Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request);

        Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request);

        Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request);

        Task<TResponse> CustomMethodAsync<TResponse>(HttpMethod httpMethod, string relativeOrAbsoluteUrl, object request);
    }
}
