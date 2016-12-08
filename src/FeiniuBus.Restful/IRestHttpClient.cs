using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FeiniuBus.Restful
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
