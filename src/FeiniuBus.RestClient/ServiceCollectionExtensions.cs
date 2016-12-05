using FeiniuBus.RestClient.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FeiniuBus.RestClient
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFeiniuBusRestClient(this IServiceCollection collection)
        {
            collection.AddScoped<IRestHttpClient, DefaultRestHttpClient>();
            return collection;
        }
    }
}
