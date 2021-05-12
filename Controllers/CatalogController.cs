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

namespace PollyBefore.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CatalogController : Controller
    {
        #region Members
        const string _BASE_URI = @"http://localhost:57664/api/";
        const string _REQUEST_END_POINT = "inventory/";

        readonly AsyncRetryPolicy<HttpResponseMessage> _asyncRetryPolicy;
        readonly AsyncFallbackPolicy<HttpResponseMessage> _asyncFallbackPolicy;
        readonly AsyncTimeoutPolicy _asyncTimeoutPolicy;
        readonly ILogger<CatalogController> _logger;
        #endregion


        #region Constructor
        public CatalogController(ILogger<CatalogController> logger)
        {
            this._logger = logger;

            ////Basic Retry Policy
            _asyncRetryPolicy = Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
                            .RetryAsync(3);


            ////Wait and Retry Policy
            //_asyncRetryPolicy = Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
            //                  .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt));


            ////Retry Policy with onRetryAsync Delegate
            //_asyncRetryPolicy = Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
            //                .RetryAsync(3, onRetryAsync: OnRetry);


            ////Retry Policy or HttpRequestException along with onRetryAsync Delegate  
            //_asyncRetryPolicy = Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
            //                .Or<HttpRequestException>()
            //               .RetryAsync(3, onRetryAsync: OnRetry);

            //Fallback Policy
            _asyncFallbackPolicy = Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
                   .Or<TimeoutRejectedException>()
                   .FallbackAsync(new HttpResponseMessage(HttpStatusCode.OK)
                   {
                       Content = new ObjectContent(typeof(int), 0, new JsonMediaTypeFormatter())
                   });

            _asyncTimeoutPolicy = Policy.TimeoutAsync(2);
        }
        #endregion


        #region Action Methods
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            HttpResponseMessage response = await RetryPolicy(id);

            if (response.IsSuccessStatusCode)
            {
                var itemsInStock = JsonSerializer.Deserialize<int>(await response.Content.ReadAsStringAsync());
                return Ok(itemsInStock);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        } 
        #endregion


        #region Private Method
        private async Task<HttpResponseMessage> RetryPolicy(int id)
        {
            return await _asyncRetryPolicy.ExecuteAsync(context =>
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

        private Task OnRetry(DelegateResult<HttpResponseMessage> delegateResponse, int retryCnt, Context context)
        {
            if (delegateResponse.Exception != null)
            {
                _logger.LogError(delegateResponse.Exception.GetBaseException().Message);
            }
            else if (delegateResponse.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                _logger.LogError("NotFound");
            else if (delegateResponse.Result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                _logger.LogError("InternalServerError");
            else if (delegateResponse.Result.StatusCode == HttpStatusCode.Unauthorized)
            {
                context.Remove("TokenValue");
                context.Add("TokenValue", "GoodAuthToken");
                _logger.LogError("Unauthorized");
            }
            return Task.CompletedTask;
        }

        private HttpClient GetHttpClient(string authCookieValue)
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

        private HttpClient GetHttpClient()
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
