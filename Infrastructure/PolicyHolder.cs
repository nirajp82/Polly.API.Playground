using Polly.Timeout;
using Polly.Wrap;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace Polly.API.Playground
{
    public class PolicyHolder : IPolicyHolder
    {
        public IAsyncPolicy<HttpResponseMessage> TimeoutGenericPolicy { get; set; }
        public IAsyncPolicy TimeoutPolicy { get; set; }
        public IAsyncPolicy<HttpResponseMessage> HttpRetryPolicy { get; set; }
        public IAsyncPolicy<HttpResponseMessage> HttpRequestFallbackPolicy { get; set; }
        public AsyncPolicyWrap<HttpResponseMessage> TimeoutRetryAndFallbackWrap { get; set; }

        readonly int _cachedResult = int.MinValue;

        public PolicyHolder()
        {
            TimeoutGenericPolicy = Policy.TimeoutAsync<HttpResponseMessage>(1, onTimeoutAsync: TimeoutAsyncHandler); // throws TimeoutRejectedException if timeout of 1 second is exceeded
            TimeoutPolicy = Policy.TimeoutAsync(1, onTimeoutAsync: TimeoutAsyncHandler); // throws TimeoutRejectedException if timeout of 1 second is exceeded

            HttpRetryPolicy =
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .Or<TimeoutRejectedException>()
                    .RetryAsync(3, onRetryAsync: RetryHandler);

            HttpRequestFallbackPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<TimeoutRejectedException>()
                .FallbackAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ObjectContent(_cachedResult.GetType(), _cachedResult, new JsonMediaTypeFormatter())
                }, onFallbackAsync: FallbackAsyncHandler);

            //TimeoutRetryAndFallbackWrap = Policy.WrapAsync(HttpRequestFallbackPolicy, HttpRetryPolicy, TimeoutGenericPolicy);
            TimeoutRetryAndFallbackWrap = Policy.WrapAsync(HttpRequestFallbackPolicy, HttpRetryPolicy.WrapAsync(TimeoutPolicy));
        }

        private Task RetryHandler(DelegateResult<HttpResponseMessage> arg1, int arg2)
        {
            Console.WriteLine("RetryHandler");
            Debug.WriteLine("RetryHandler");
            return Task.CompletedTask;
        }

        Task FallbackAsyncHandler(DelegateResult<HttpResponseMessage> arg)
        {
            Console.WriteLine("FallbackAsyncHandler");
            Debug.WriteLine("FallbackAsyncHandler");
            return Task.CompletedTask;
        }

        Task TimeoutAsyncHandler(Context arg1, TimeSpan arg2, Task arg3)
        {
            Console.WriteLine("TimeoutAsyncHandler");
            Debug.WriteLine("TimeoutAsyncHandler");
            return Task.CompletedTask;
        }
    }
}