using Microsoft.Extensions.Caching.Memory;
using Polly.Caching;
using Polly.Timeout;
using Polly.Wrap;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Caching.Memory;
using Polly.Registry;

namespace Polly.API.Playground
{
    public class PolicyHolder : IPolicyHolder
    {
        #region Members
        public IAsyncPolicy<HttpResponseMessage> TimeoutGenericPolicy { get; set; }
        public IAsyncPolicy TimeoutPolicy { get; set; }
        public IAsyncPolicy<HttpResponseMessage> HttpRetryPolicy { get; set; }
        public IAsyncPolicy<HttpResponseMessage> HttpRequestFallbackPolicy { get; set; }
        public AsyncPolicyWrap<HttpResponseMessage> TimeoutRetryAndFallbackWrap { get; set; }
        public IAsyncPolicy<HttpResponseMessage> CachePolicy { get; set; }
        #endregion


        readonly int _fallbackValue = int.MinValue;

        public PolicyHolder(IMemoryCache memoryCache)
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
                    Content = new ObjectContent(_fallbackValue.GetType(), _fallbackValue, new JsonMediaTypeFormatter())
                }, onFallbackAsync: FallbackAsyncHandler);

            //TimeoutRetryAndFallbackWrap = Policy.WrapAsync(HttpRequestFallbackPolicy, HttpRetryPolicy, TimeoutGenericPolicy);
            TimeoutRetryAndFallbackWrap = Policy.WrapAsync(HttpRequestFallbackPolicy, HttpRetryPolicy.WrapAsync(TimeoutPolicy));

            //Cache Policy
            Func<Context, HttpResponseMessage, Ttl> cacheOnlySuccessfilter = (context, result) => new Ttl(
                                    timeSpan: result.IsSuccessStatusCode ? TimeSpan.FromMinutes(5) : TimeSpan.Zero,
                    slidingExpiration: true);

            ////new ResultTtl(cacheOnlySuccessfilter)
            Caching.Memory.MemoryCacheProvider memoryCacheProvider = new(memoryCache);

            CachePolicy = Policy.CacheAsync<HttpResponseMessage>(
                   cacheProvider: memoryCacheProvider.AsyncFor<HttpResponseMessage>(), //note the .AsyncFor<HttpResponseMessage>
                    ttlStrategy: new ResultTtl<HttpResponseMessage>(cacheOnlySuccessfilter),
                    onCacheError: CacheErrorHandler
                );
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

        private void CacheErrorHandler(Context arg1, string arg2, Exception arg3)
        {
            Console.WriteLine("cacheErrorHandler");
            Debug.WriteLine("cacheErrorHandler");
        }
    }
}