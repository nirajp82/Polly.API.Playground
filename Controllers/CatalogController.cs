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

namespace PollyBefore.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CatalogController : Controller
    {
        #region Members
        readonly AsyncRetryPolicy<HttpResponseMessage> _httpAsyncRetryPolicy;
        readonly ILogger<CatalogController> _logger;
        #endregion


        #region Constructor
        public CatalogController(ILogger<CatalogController> logger)
        {
            ////Basic Retry Policy
            //_httpAsyncRetryPolicy = Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
            //                .RetryAsync(3);


            ////Wait and Retry Policy
            //_httpAsyncRetryPolicy = Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
            //                  .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt));


            //Retry Policy with onRetryAsync Delegate
            _httpAsyncRetryPolicy = Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
                            .RetryAsync(3, onRetryAsync: OnRetry);
            this._logger = logger;
        }
        #endregion


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            string requestEndpoint = $"inventory/{id}";

            var response = await _httpAsyncRetryPolicy.ExecuteAsync(context =>
            {
                string tokenValue = context["TokenValue"].ToString();
                _logger.LogInformation($"Calling API: {tokenValue}");
                var httpClient = GetHttpClient(tokenValue);
                return httpClient.GetAsync(requestEndpoint);
            }, new Dictionary<string, object>
            {
                {"TokenValue", "BadAuthToken"}
            });

            if (response.IsSuccessStatusCode)
            {
                var itemsInStock = JsonSerializer.Deserialize<int>(await response.Content.ReadAsStringAsync());
                return Ok(itemsInStock);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }

        private Task OnRetry(DelegateResult<HttpResponseMessage> delegateResponse, int retryCnt, Context context)
        {
            if (delegateResponse.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
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
            httpClient.BaseAddress = new Uri(@"http://localhost:57664/api/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        //private HttpClient GetHttpClient()
        //{
        //    var httpClient = new HttpClient();
        //    httpClient.BaseAddress = new Uri(@"http://localhost:57664/api/");
        //    httpClient.DefaultRequestHeaders.Accept.Clear();
        //    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //    return httpClient;
        //}
    }
}
