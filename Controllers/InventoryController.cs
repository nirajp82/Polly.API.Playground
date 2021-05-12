using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System;
using Microsoft.Extensions.Logging;

namespace PollyBefore.Controllers
{
    [Route("api/Inventory")]
    public class InventoryController : Controller
    {
        #region Members
        readonly ILogger<CatalogController> _logger;
        static int _requestCount = 0;
        #endregion

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            return await AuthExample();
            //return await RetryExample();
        }

        #region Private Methods
        private async Task<IActionResult> RetryExample()
        {
            await Task.Delay(20);// simulate some data processing by delaying
            _requestCount++;

            if (_requestCount % 3 == 0) // only one of out 3 requests will succeed
            {
                _requestCount = 0;
                return Ok(15);
            }
            return StatusCode((int)HttpStatusCode.InternalServerError, $"Please try again. Attempt {_requestCount}");
        }

        private async Task<IActionResult> AuthExample()
        {
            await Task.Delay(20);// simulate some data processing by delaying

            string authCode = Request.Cookies["Auth"];
            if (authCode == "GoodAuthToken")
                return Ok(15);
            else
                return StatusCode((int)HttpStatusCode.Unauthorized, $"Not authorized ");
        }
        #endregion
    }
}
