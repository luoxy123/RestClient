using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.IO;
using System.Net.Http;
using FeiniuBus.RestClient.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace FeiniuBus.RestClient
{
    public class DefaultRestHttpClient : IRestHttpClient
    {
        private static readonly string DefaultUserAgent = "FeiniuBus .NET HttpClient" + Environment.Version;
        private int _activeAsyncRequests;

        public DefaultRestHttpClient()
        {
            ContentType = MimeTypes.Json;
            Accept = MimeTypes.Json;
        }

        public CancellationTokenSource CancelTokenSource { get; set; }
        internal HttpClient HttpClient { get; set; }

        public string BaseUri { get; set; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public string BearerToken { get; set; }

        public Task<TResponse> SendAsync<TResponse>(HttpMethod httpMethod, string absoluteUrl, object request,
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
            httpRequest.Headers.Add("Accept", Accept);
            httpRequest.Headers.Add("User-Agent", DefaultUserAgent);
            if (!string.IsNullOrEmpty(BearerToken))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);

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
                        httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
                    }
                    else if (stream != null)
                    {
                        httpRequest.Content = new StreamContent(stream);
                        httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
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

            var sendAsyncTask = client.SendAsync(httpRequest, token);
            if (typeof(TResponse) == typeof(HttpResponseMessage))
                return (Task<TResponse>)(object)sendAsyncTask;

            return sendAsyncTask.ContinueWith(responseTask =>
            {
                var httpRes = responseTask.Result;

                if (!httpRes.IsSuccessStatusCode)
                    ThrowIfError(responseTask, httpRes);

                if (typeof(TResponse) == typeof(string))
                    return httpRes.Content.ReadAsStringAsync()
                        .ContinueWith(task => (TResponse)(object)task.Result, token);

                if (typeof(TResponse) == typeof(byte[]))
                    return httpRes.Content.ReadAsByteArrayAsync()
                        .ContinueWith(task => (TResponse)(object)task.Result, token);

                if (typeof(TResponse) == typeof(Stream))
                    return httpRes.Content.ReadAsStreamAsync()
                        .ContinueWith(task => (TResponse)(object)task.Result, token);

                return httpRes.Content.ReadAsStringAsync().ContinueWith(task =>
                {
                    var body = task.Result;
                    var response = body.AsJson<TResponse>();
                    return response;
                }, token);
            }, token).Unwrap();
        }

        public void AddHttpRequestHeader(string key, string value)
        {
            HttpClient = GetHttpClient();
            HttpClient?.DefaultRequestHeaders.Add(key, new[] { value });
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

        private void ThrowIfError(Task task, HttpResponseMessage httpRes)
        {
            DisposeCancelToken();

            if (task.IsFaulted)
                throw new ReuqestTaskFaultedException("请求任务失败", task.Exception);

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
            var handler = new HttpClientHandler { ClientCertificateOptions = ClientCertificateOption.Automatic };
            var client = new HttpClient(handler) { BaseAddress = baseUri };

            return HttpClient = client;
        }
    }
}
