using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace AVAPI
{
    public class API
    {
        private static API api;
        internal Engine engine;
        static API()
        {
            API.api = new();
        }
        private API()
        {
            this.engine = new(API.api);
        }
        public static void Main(string[] args)
        {
            string message = string.Empty;

            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/debug/find/{spec}", (string spec) => API.api.engine.Debug_Find(spec, out message, quoted: false).ToString());
            app.MapGet("/debug/find-quoted/{spec}", (string spec) => API.api.engine.Debug_Find(spec, out message, quoted: true).ToString());

            string binary = "application/octet-stream";
            app.MapGet("/find/{spec}", (string spec) => Results.Stream(API.api.engine.Binary_Find(spec, out message, quoted: false), binary));
            app.MapGet("/find-quoted/{spec}", (string spec) => Results.Stream(API.api.engine.Binary_Find(spec, out message, quoted: true), binary));

            string yaml = "application/x-yaml";
            app.MapGet("/{book}/{chapter}/find/{spec}", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(spec, book, chapter, out message, quoted: false), yaml));
            app.MapGet("/{book}/{chapter}/find-quoted/{spec}", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(spec, book, chapter, out message, quoted: true), yaml));

            app.MapGet("/{book}/{chapter}/", (string book, string chapter) => $"unhighlighted {book}:{chapter}");
            app.MapGet("/", () => "Hello Status!");

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            app.Run("http://localhost:1769");
        }
    }
}
