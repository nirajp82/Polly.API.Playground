using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polly.Retry;
using Polly;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Collections.Generic;
using Polly.Timeout;
using System.Net.Http.Formatting;
using Polly.Fallback;
using System.Threading;
using Polly.API.Playground.Infrastructure;
using Polly.Registry;
using Polly.Wrap;
using Polly.API.Playground;

namespace PollyBefore.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CatalogController : Controller
    {
        #region Members
        const string _BASE_URI = @"http://localhost:57664/api/";
        const string _REQUEST_END_POINT = "inventory/";
        readonly ILogger<CatalogController> _logger;
        readonly PolicyRegistry _policyRegistry;
        readonly IPolicyHolder _policyHolder;
        #endregion


        #region Constructor
        public CatalogController(ILogger<CatalogController> logger, PolicyRegistry policyRegistry, IPolicyHolder PolicyHolder)
        {
            _logger = logger;
            _policyRegistry = policyRegistry;
            _policyHolder = PolicyHolder;
        }
        #endregion


        #region Action Methods
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            //HttpResponseMessage response = await RetryPolicy(id);
            //HttpResponseMessage response = await ManuallyCombinedPolicy(id);
            HttpResponseMessage response = await _policyHolder.TimeoutRetryAndFallbackWrap.ExecuteAsync(async token =>
            {
                var httpClient = GetHttpClient();
                return await httpClient.GetAsync($"{_REQUEST_END_POINT}{id}", token);
            }, CancellationToken.None);

            if (response.IsSuccessStatusCode)
            {
                var itemsInStock = JsonSerializer.Deserialize<int>(await response.Content.ReadAsStringAsync());
                return Ok(itemsInStock);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }
        #endregion


        #region Private Method
        async Task<HttpResponseMessage> RetryPolicy(int id)
        {
            var asyncRetryPolicy = GetAsyncPolicy(PollyPolicyRegistry.BasicRetryPolicy);
            return await asyncRetryPolicy.ExecuteAsync(context =>
            {
                string tokenValue = context["TokenValue"].ToString();
                _logger.LogInformation($"Calling API: {tokenValue}");

                var httpClient = GetHttpClient(tokenValue);
                return httpClient.GetAsync($"{_REQUEST_END_POINT}{id}");
            }, new Dictionary<string, object>
            {
                {"TokenValue", "BadAuthToken"}
            });
        }

        IAsyncPolicy<HttpResponseMessage> GetAsyncHttpPolicy(string key) => _policyRegistry.Get<AsyncPolicy<HttpResponseMessage>>(key);
        IAsyncPolicy GetAsyncPolicy(string key) => _policyRegistry.Get<AsyncPolicy>(key);


        async Task<HttpResponseMessage> ManuallyCombinedPolicy(int id)
        {
            return await GetAsyncHttpPolicy(PollyPolicyRegistry.FallbackWithTimedOutExceptionPolicy).ExecuteAsync(() =>
                            GetAsyncHttpPolicy(PollyPolicyRegistry.BasicRetryPolicy).ExecuteAsync(() =>
                            GetAsyncPolicy(PollyPolicyRegistry.TimeoutPolicy).ExecuteAsync(
                                        async token =>
                                        {
                                            var httpClient = GetHttpClient();
                                            return await httpClient.GetAsync($"{_REQUEST_END_POINT}{id}", token);
                                        }, CancellationToken.None)));
        }

        HttpClient GetHttpClient(string authCookieValue)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            cookieContainer.Add(new Uri("http://localhost"), new Cookie("Auth", authCookieValue));

            var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri(_BASE_URI);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_BASE_URI);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }
        #endregion
    }
}
