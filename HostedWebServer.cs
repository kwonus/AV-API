// Adapted from: https://github.com/RickStrahl/Westwind.AspNetCore.HostedWebServer

namespace AVAPI
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.FileProviders;

    public abstract class HostedWebServer
    {
        public bool IsRunning { get; set; }

        public WebApplication App { get; set; }

        public string ErrorMessage { get; set; }

        public Exception LastException { get; set; }

        public Action<WebApplication> OnMapRequests { get; set; }

        public Action<HttpContext> OnRequestStarting { get; set; }

        public Action<HttpContext, TimeSpan> OnRequestCompleted { get; set; }

        /// <summary>
        /// Launches the Web Server synchronously and
        /// waits for it to shut down
        /// </summary>
        /// <param name="url">startup url</param>
        /// <param name="webRootPath">An optional path for static file locations. If not specified uses entry assembly location</param>
        /// <returns></returns>
        protected abstract bool Launch(string url);

        /// <summary>
        /// Launches the Web Server in the background so you can continue
        /// processing
        /// so you 
        /// </summary>
        /// <param name="url">tartup url</param>
        /// <param name="webRootPath"></param>
        public async Task<bool> LaunchAsync(string url = "http://localhost:1769")
        {
            return await Task.Run(() => Launch(url));
        }


        public async Task Stop()
        {
            await App.StopAsync();
            IsRunning = false;
        }

        /// <summary>
        /// This middle ware handler intercepts every request captures the time
        /// and then logs out to the screen (when that feature is enabled) the active
        /// request path, the status, processing time.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        async Task RequestInterceptorMiddlewareHandler(HttpContext context, Func<Task> next)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (OnRequestStarting != null)
            {
                OnRequestStarting.Invoke(context);
            }

            await next();

            if (OnRequestCompleted != null)
            {
                OnRequestCompleted.Invoke(context, sw.Elapsed);
            }
        }
    }
}

