using FeiniuBus.Restful.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FeiniuBus.Restful
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
