using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly.Caching;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Polly.API.Playground.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy<HttpResponseMessage> _cachePolicy;

        public HomeController(HttpClient httpClient, IPolicyHolder policyHolder)
        {
            _httpClient = httpClient;
            _cachePolicy = policyHolder.CachePolicy;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            string requestEndpoint = $"departments";

            Context policyExecutionContext = new Context($"departments");

            HttpResponseMessage response = await _cachePolicy.ExecuteAsync(
                async (context) =>
                {
                    return await _httpClient.GetAsync(requestEndpoint);
                }, policyExecutionContext);

            if (response.IsSuccessStatusCode)
            {
                Dictionary<int, string> departments = JsonConvert.DeserializeObject<Dictionary<int, string>>(
                    await response.Content.ReadAsStringAsync());
                return Ok(departments);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }

        [HttpGet("{departmentId}")]
        public async Task<IActionResult> Get(int departmentId)
        {
            string requestEndpoint = $"departments/{departmentId}";

            Context policyExecutionContext = new Context($"department-{departmentId}");

            HttpResponseMessage response = await _cachePolicy.ExecuteAsync(
                async (context) =>
                {
                    return await _httpClient.GetAsync($"{requestEndpoint}");
                }, policyExecutionContext);

            if (response.IsSuccessStatusCode)
            {
                string departmentName = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Ok(departmentName);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }
        
      public async Task<T> BasicRetryAsync<T>(Func<Task<T>> funcAsync)
      {
          var result = await Policy
                     .Handle<RedisConnectionException>() // Possible RedisConnectionException issue 
                     .Or<SocketException>() //Possible SocketException issue
                     .WaitAndRetryAsync(_retryMaxAttempts, i => TimeSpan.FromMilliseconds(_sleepDurationMS),
                         onRetryAsync: async (exception, sleepDuration, attemptNumber, context) =>
                         {
                           await ForceReconnectAsync();
                         })
                     .ExecuteAsync(async () =>
                     {
                       return await funcAsync();
                     });

         return result;
    }
    }
}
