using Polly.Caching;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Polly.API.Playground
{
    public interface IPolicyHolder
    {
        IAsyncPolicy<HttpResponseMessage> TimeoutGenericPolicy { get; set; }

        IAsyncPolicy<HttpResponseMessage> HttpRetryPolicy { get; set; }

        IAsyncPolicy<HttpResponseMessage> HttpRequestFallbackPolicy { get; set; }

        AsyncPolicyWrap<HttpResponseMessage> TimeoutRetryAndFallbackWrap { get; set; }

        public IAsyncPolicy<HttpResponseMessage> CachePolicy { get; set; }
    }
}