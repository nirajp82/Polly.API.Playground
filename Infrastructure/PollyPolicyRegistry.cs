using Polly.Registry;
using Polly.Retry;
using Polly;
using System.Net;
using Polly.Timeout;
using Polly.Fallback;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Formatting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Polly.API.Playground.Infrastructure
{
    public class PollyPolicyRegistry
    {
        #region Member
        public const string BasicRetryPolicy = "BasicRetryPolicy";
        public const string WaitAndRetryPolicy = "WaitAndRetryPolicy";
        public const string RetryWithDelegatePolicy = "RetryWithDelegatePolicy";
        public const string RetryOrExceptionPolicy = "RetryOrExceptionPolicy";
        public const string FallbackWithTimedOutExceptionPolicy = "FallbackWithTimedOutExceptionPolicy";
        public const string TimeoutPolicy = "TimeoutPolicy";
        #endregion


        #region Constructor
        #endregion


        #region Public Method
        public static PolicyRegistry Build()
        {

            PolicyRegistry registry = new PolicyRegistry();
            //Basic Retry Policy
            AsyncRetryPolicy<HttpResponseMessage> basicRetryPolicy =
                            Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
                                .RetryAsync(3);
            registry.Add(BasicRetryPolicy, basicRetryPolicy);


            //Wait and Retry Policy
            AsyncRetryPolicy<HttpResponseMessage> waitAndRetryPolicy =
                            Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
                                .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt), onRetryAsync: WaitAndRetryHandler);

            registry.Add(WaitAndRetryPolicy, waitAndRetryPolicy);


            ////Retry Policy with onRetryAsync Delegate
            AsyncRetryPolicy<HttpResponseMessage> retryWithDelegatePolicy =
                            Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
                             .RetryAsync(2, onRetryAsync: OnRetry);
            registry.Add(RetryWithDelegatePolicy, retryWithDelegatePolicy);


            ////Retry Policy or HttpRequestException along with onRetryAsync Delegate  
            AsyncRetryPolicy<HttpResponseMessage> retryOrExceptionPolicy =
                            Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
                                .Or<HttpRequestException>()
                            .RetryAsync(2, onRetryAsync: OnRetry);
            registry.Add(RetryOrExceptionPolicy, retryOrExceptionPolicy);


            //Fallback Policy
            AsyncFallbackPolicy<HttpResponseMessage> asyncFallbackPolicy =
                            Policy.HandleResult<HttpResponseMessage>(result => !result.IsSuccessStatusCode)
                                .Or<TimeoutRejectedException>()
                                .FallbackAsync(new HttpResponseMessage(HttpStatusCode.OK)
                                {
                                    Content = new ObjectContent(typeof(int), int.MaxValue, new JsonMediaTypeFormatter())
                                }, onFallbackAsync: FallbackAsyncHandler);
            registry.Add(FallbackWithTimedOutExceptionPolicy, asyncFallbackPolicy);


            //TimeoutPolicy
            AsyncTimeoutPolicy timeoutPolicy = Policy.TimeoutAsync(1, onTimeoutAsync: TimeoutAsyncHandler);
            registry.Add(TimeoutPolicy, timeoutPolicy);

            return registry;
        }      
        #endregion

        private static Task OnRetry(DelegateResult<HttpResponseMessage> delegateResponse, int retryCnt, Context context)
        {
            if (delegateResponse.Exception != null)
            {
                Console.WriteLine(delegateResponse.Exception.GetBaseException().Message);
            }
            else if (delegateResponse.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                Console.WriteLine("NotFound");
            else if (delegateResponse.Result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                Console.WriteLine("InternalServerError");
            else if (delegateResponse.Result.StatusCode == HttpStatusCode.Unauthorized)
            {
                context.Remove("TokenValue");
                context.Add("TokenValue", "GoodAuthToken");
                Console.WriteLine("Unauthorized");
            }
            return Task.CompletedTask;
        }

        private static Task WaitAndRetryHandler(DelegateResult<HttpResponseMessage> arg1, TimeSpan arg2)
        {
            Console.WriteLine("WaitAndRetryHandler");
            Debug.WriteLine("WaitAndRetryHandler");
            return Task.CompletedTask;
        }

        private static Task FallbackAsyncHandler(DelegateResult<HttpResponseMessage> arg)
        {
            Console.WriteLine("FallbackAsyncHandler");
            Debug.WriteLine("FallbackAsyncHandler");
            return Task.CompletedTask;
        }

        private static Task TimeoutAsyncHandler(Context arg1, TimeSpan arg2, Task arg3)
        {
            Console.WriteLine("TimeoutAsyncHandler");
            Debug.WriteLine("TimeoutAsyncHandler");
            return Task.CompletedTask;
        }
    }
}
