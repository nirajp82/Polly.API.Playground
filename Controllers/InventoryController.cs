using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System;

namespace PollyBefore.Controllers
{
    [Route("api/Inventory")]
    public class InventoryController : Controller
    {
        static int _requestCount = 0;
        static string _message = "";

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            await Task.Delay(20);// simulate some data processing by delaying
            _requestCount++;

            if (_requestCount % 3 == 0) // only one of out 3 requests will succeed
            {
                _message = string.Empty;
                _requestCount = 0;
                return Ok(15);
            }
            _message += $" Attempt: {_requestCount} \r\n";
            return StatusCode((int)HttpStatusCode.InternalServerError, $"{_message}");
        }
    }
}
