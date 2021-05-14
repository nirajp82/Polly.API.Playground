using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Polly.API.Playground.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private static readonly Dictionary<int, string> Departments;

        static DepartmentsController()
        {
            Departments = new Dictionary<int, string>
            {
                {1, $"1-Arts - {DateTime.Now}"}, {2, $"2-Books - {DateTime.Now}"}, {3, $"3-Automotive - {DateTime.Now}"},
                {4, $"4-Beauty - {DateTime.Now}"}, {5, $"5-Cell phones - {DateTime.Now}"}, {6, $"6-Computers - {DateTime.Now}"}, {7, $"7-Electronics - {DateTime.Now}"},
                {8, $"8-Medical {DateTime.Now.ToString()}" }
            };
        }

        [HttpGet()]
        public IActionResult Get()
        {
            return Ok(Departments);
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return Ok(Departments[id]);
        }
    }
}
