using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FeiniuBus.Restful.Exceptions;
using FeiniuBus.Restful.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace FeiniuBus.Restful.Services
{
    public class DefaultRestHttpClient : IRestHttpClient
    {
        private static readonly string DefaultUserAgent = "FeiniuBus .NET HttpClient" + Environment.Version;
        private int _activeAsyncRequests;

        public DefaultRestHttpClient()
        {
            ContentType = MimeTypes.Json;
            Accept = MimeTypes.Json;
            EnableCompression = true;
        }

        public CancellationTokenSource CancelTokenSource { get; set; }
        internal HttpClient HttpClient { get; set; }

        public string BaseUri { get; set; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public string BearerToken { get; set; }
        public bool EnableCompression { get; set; }

        public async Task<TResponse> SendAsync<TResponse>(HttpMethod httpMethod, string absoluteUrl, object request,
            CancellationToken token = new CancellationToken())
        {
            if (!httpMethod.HasRequestBody() && (request != null))
            {
                var queryString = QueryStringSerializer.SerializeObject(request);
                if (!string.IsNullOrEmpty(queryString))
                    absoluteUrl += "?" + queryString;
            }

            var client = GetHttpClient();
            var httpRequest = new HttpRequestMessage(httpMethod, absoluteUrl);
            httpRequest.Headers.Add(HeaderNames.Accept, Accept);
            httpRequest.Headers.Add(HeaderNames.UserAgent, DefaultUserAgent);
            if (EnableCompression)
                httpRequest.Headers.Add(HeaderNames.AcceptEncoding, "gzip");

            if (httpMethod.HasRequestBody() && (request != null))
            {
                var httpContent = request as HttpContent;
                if (httpContent != null)
                {
                    httpRequest.Content = httpContent;
                }
                else
                {
                    var str = request as string;
                    var bytes = request as byte[];
                    var stream = request as Stream;

                    if (str != null)
                    {
                        httpRequest.Content = new StringContent(str, Encoding.UTF8, ContentType);
                    }
                    else if (bytes != null)
                    {
                        httpRequest.Content = new ByteArrayContent(bytes);
                        httpRequest.Content.Headers.Add(HeaderNames.ContentType, ContentType);
                    }
                    else if (stream != null)
                    {
                        httpRequest.Content = new StreamContent(stream);
                        httpRequest.Content.Headers.Add(HeaderNames.ContentType, ContentType);
                    }
                    else
                    {
                        if (ContentType == MimeTypes.Json)
                        {
                            httpRequest.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                                ContentType);
                        }
                        else if (ContentType == MimeTypes.FormUrlEncoded)
                        {
                            var s = QueryStringSerializer.SerializeObject(request);
                            var contents = new List<KeyValuePair<string, string>>();

                            foreach (var pair in QueryHelpers.ParseQuery(s))
                            {
                                var keyValue = new KeyValuePair<string, string>(pair.Key, pair.Value);
                                contents.Add(keyValue);
                            }
                            httpRequest.Content = new FormUrlEncodedContent(contents);
                        }
                    }
                }
            }

            ApplyWebRequestFilters(httpRequest);

            Interlocked.Increment(ref _activeAsyncRequests);

            if (token == default(CancellationToken))
            {
                if (CancelTokenSource == null)
                    CancelTokenSource = new CancellationTokenSource();
                token = CancelTokenSource.Token;
            }

            var response = await client.SendAsync(httpRequest, token);
            if (typeof(TResponse) == typeof(HttpResponseMessage))
                return (TResponse) (object) response;

            ThrowIfError(response);

            var ms = new MemoryStream();
            var content = await response.Content.ReadAsStreamAsync();

            if (ShouldDecompress(response))
            {
                using (var gzip = new GZipStream(content, CompressionMode.Decompress, true))
                {
                    await gzip.CopyToAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                await content.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
            }

            if (typeof(TResponse) == typeof(Stream))
                return (TResponse) (object) ms;

            if (typeof(TResponse) == typeof(byte[]))
                return (TResponse) (object) ms.ToArray();

            var reader = new StreamReader(ms);
            var result = await reader.ReadToEndAsync();

            if (typeof(TResponse) == typeof(string))
                return (TResponse) (object) result;

            return result.AsJson<TResponse>();
        }

        public void AddHttpRequestHeader(string key, string value)
        {
            HttpClient = GetHttpClient();
            HttpClient?.DefaultRequestHeaders.Add(key, new[] {value});
        }

        public Action<HttpRequestMessage> RequestFilter { get; set; }

        public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request)
        {
            return SendAsync<TResponse>(HttpMethod.Get, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return SendAsync<TResponse>(HttpMethod.Post, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request)
        {
            return SendAsync<TResponse>(HttpMethod.Delete, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return SendAsync<TResponse>(HttpMethod.Put, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(HttpMethod httpMethod, string relativeOrAbsoluteUrl,
            object request)
        {
            return SendAsync<TResponse>(httpMethod, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        private bool ShouldDecompress(HttpResponseMessage response)
        {
            IEnumerable<string> values;
            if (!response.Headers.TryGetValues(HeaderNames.ContentEncoding, out values))
                return false;

            if (!values.Any(x => string.Equals(x, "gzip", StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }

        private string ToAbsoluteUrl(string relativeOrAbsoluteUrl)
        {
            return relativeOrAbsoluteUrl.StartsWith("http:") || relativeOrAbsoluteUrl.StartsWith("https:")
                ? relativeOrAbsoluteUrl
                : BaseUri.CombineWith(relativeOrAbsoluteUrl);
        }

        private void ApplyWebRequestFilters(HttpRequestMessage request)
        {
            RequestFilter?.Invoke(request);
        }

        private void ThrowIfError(HttpResponseMessage httpRes)
        {
            DisposeCancelToken();

            if (!httpRes.IsSuccessStatusCode)
            {
                var exception = new WebServiceException(httpRes.ReasonPhrase)
                {
                    StatusCode = httpRes.StatusCode,
                    ResponseBody = httpRes.Content.ReadAsStringAsync().Result
                };

                throw exception;
            }
        }

        private void DisposeCancelToken()
        {
            if (Interlocked.Decrement(ref _activeAsyncRequests) > 0)
                return;
            if (CancelTokenSource == null)
                return;

            CancelTokenSource.Dispose();
            CancelTokenSource = null;
        }

        private HttpClient GetHttpClient()
        {
            if (HttpClient != null)
                return HttpClient;

            var baseUri = BaseUri != null ? new Uri(BaseUri) : null;
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler) {BaseAddress = baseUri};

            if (BearerToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);

            return HttpClient = client;
        }
    }
}