using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Polly.API.Playground.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        #region Members
        private readonly ILogger<CustomerController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        #endregion


        #region Constructor
        public CustomerController(ILogger<CustomerController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        #endregion


        #region Action Methods
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            string endpoint = $"order/{id}";
            var httpClient = _httpClientFactory.CreateClient("OrderService");
            HttpResponseMessage response = await httpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                string orderDetails = await response.Content.ReadAsAsync<string>();
                return Ok(orderDetails);
            }
            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }
        #endregion
    }
}
