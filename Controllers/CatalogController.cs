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

namespace PollyBefore.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CatalogController : Controller
    {
        #region Members
        readonly AsyncRetryPolicy<HttpResponseMessage> _httpAsyncRetryPolicy;
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
                            .RetryAsync(3, onRetryAsync: (httpResponseMessage, retryCount) =>
                            {
                                if (httpResponseMessage.Result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                                    logger.LogError("Unauthorized");
                                else if (httpResponseMessage.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                                    logger.LogError("NotFound");
                                else if (httpResponseMessage.Result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                    logger.LogError("InternalServerError");
                                return Task.CompletedTask;
                            });
        }
        #endregion


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var httpClient = GetHttpClient();
            string requestEndpoint = $"inventory/{id}";

            HttpResponseMessage response = await _httpAsyncRetryPolicy.ExecuteAsync(() => httpClient.GetAsync(requestEndpoint));

            if (response.IsSuccessStatusCode)
            {
                var itemsInStock = JsonSerializer.Deserialize<int>(await response.Content.ReadAsStringAsync());
                Response.Headers.Add("X-Debug-Message", response.Headers.GetValues("X-Debug-Message").FirstOrDefault());
                return Ok(itemsInStock);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }

        private HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(@"http://localhost:57664/api/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }
    }
}
