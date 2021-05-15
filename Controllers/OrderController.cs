using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Polly.API.Playground.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        #region Members
        static int _requestCount = 0;
        private static readonly Dictionary<int, string> Orders;
        #endregion

        static OrderController()
        {
            Orders = new Dictionary<int, string>
            {
                {1, $"1-Arts - {DateTime.Now}"}, {2, $"2-Books - {DateTime.Now}"}, {3, $"3-Automotive - {DateTime.Now}"},
                {4, $"4-Beauty - {DateTime.Now}"}, {5, $"5-Cell phones - {DateTime.Now}"}, {6, $"6-Computers - {DateTime.Now}"}, {7, $"7-Electronics - {DateTime.Now}"},
                {8, $"8-Medical {DateTime.Now.ToString()}" }
            };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            _requestCount++;
            if (_requestCount % 4 == 0)
                return Ok(Orders[id]);

            await Task.Delay(200);// simulate some data processing by delaying
            return StatusCode((int)HttpStatusCode.InternalServerError, $"Please try again. Attempt {_requestCount}");
        }
    }
}
