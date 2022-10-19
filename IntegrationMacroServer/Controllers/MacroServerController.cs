using IntegrationMacroServer.Utility;
using Microsoft.AspNetCore.Mvc;
using Refit;

namespace IntegrationMacroServer.Controllers
{
    public class AsyncResult
    {
        public bool Succeeded => Error == null;

        public ActionResult? Error { get; set; } = null;

        public int StatusCode { get; set; }
    }

    public class AsyncResult<T> : AsyncResult
    {
        public T? Result { get; set; }
    }   
    
    public abstract class MacroServerController : ControllerBase
    {
        protected ILogger Logger { get; }

        protected MacroServerController(ILogger logger)
        {
            Logger = logger;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<AsyncResult> TryAsync(Func<Task> f, string actionName)
        {
            try
            {
                await f().ConfigureAwait(false);
                return new AsyncResult();
            }
            catch (Exception ex)
            {
                return GetErrorResult(ex, actionName);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<AsyncResult> TryAsync<T1>(Func<T1, Task> f, string actionName, T1 arg)
        {
            try {
                await f(arg).ConfigureAwait(false);
                return new AsyncResult();
            } catch (Exception ex) {
                return GetErrorResult(ex, actionName);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<AsyncResult> TryAsync<T1, T2>(Func<T1, T2, Task> f,
            string actionName, T1 arg1, T2 arg2)
        {
            try {
                await f(arg1, arg2).ConfigureAwait(false);
                return new AsyncResult();
            } catch (Exception ex) {
                return GetErrorResult(ex, actionName);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<AsyncResult<R>> TryAsyncReturn<R>(Func<Task<R>> f,
            string actionName)
        {
            try {
                var result = await f().ConfigureAwait(false);
                return new AsyncResult<R>() { Result = result };
            } catch (Exception ex) {
                return GetErrorResult<R>(ex, actionName);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<AsyncResult<R>> TryAsyncReturn<R, T1>(Func<T1, Task<R>> f,
            string actionName, T1 arg)
        {
            try {
                var result = await f(arg).ConfigureAwait(false);
                return new AsyncResult<R>() { Result = result };
            } catch (Exception ex) {
                return GetErrorResult<R>(ex, actionName);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<AsyncResult<R>> TryAsyncReturn<R, T1, T2>(Func<T1, T2, Task<R>> f,
            string actionName, T1 arg1, T2 arg2)
        {
            try {
                var result = await f(arg1, arg2).ConfigureAwait(false);
                return new AsyncResult<R>() { Result = result };
            } catch (Exception ex) {
                return GetErrorResult<R>(ex, actionName);
            }
        }

        private AsyncResult GetErrorResult(Exception ex, string actionName)
        {
            int statusCode = 0;
            if (ex is ApiException apiex) {
                statusCode = (int)apiex.StatusCode;
                Logger.LogWarning("Bad return code while {0} ({1})", actionName, apiex.StatusCode);
                if ((int)apiex.StatusCode < 500) {
                    return new AsyncResult
                    {
                        Error = BadRequest(new { message = ex.Message }),
                        StatusCode = statusCode
                    };
                }
            } else {
                Logger.LogError(ex, "Exception while {0}", actionName);
            }

            return new AsyncResult
            {
                Error = new InternalServerErrorResult(new { message = ex.Message }),
                StatusCode = statusCode
            };
        }

        private AsyncResult<R> GetErrorResult<R>(Exception ex, string actionName)
        {
            int statusCode = 0;
            if (ex is ApiException apiex) {
                statusCode = (int)apiex.StatusCode;
                Logger.LogWarning("Bad return code while {0} ({1})", actionName, apiex.StatusCode);
                if ((int)apiex.StatusCode < 500) {
                    return new AsyncResult<R>
                    {
                        Error = BadRequest(new { message = ex.Message }),
                        StatusCode = statusCode
                    };
                }
            } else {
                Logger.LogError(ex, "Exception while {0}", actionName);
            }

            return new AsyncResult<R>
            {
                Error = new InternalServerErrorResult(new { message = ex.Message }),
                StatusCode = statusCode
            };
        }
    }
}

