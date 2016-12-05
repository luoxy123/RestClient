using System;
using System.Net.Http;

namespace FeiniuBus.RestClient.Services
{
    public static class HttpExtensions
    {
        public static bool HasRequestBody(this HttpMethod method)
        {
            if ((method == HttpMethod.Delete) || (method == HttpMethod.Get) || (method == HttpMethod.Head) ||
                (method == HttpMethod.Options))
                return false;

            return true;
        }

        public static string CombineWith(this string path, string relativeUrl)
        {
            var absoluteUri = new Uri(new Uri(path), relativeUrl);
            return absoluteUri.ToString();
        }
    }
}
