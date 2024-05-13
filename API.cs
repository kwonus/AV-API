using Blueprint.Model.Implicit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Pinshot.Blue;
using System.Net;
using static Blueprint.Model.Implicit.QFormat;

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

            string yaml = "text/x-yaml; charset=utf-8";
            string text = "text/plain; charset=utf-8";
            string html = "text/html; charset=utf-8";
            string mkdn = "text/markdown; charset=utf-8";

            app.MapGet("/{book}/{chapter}/find/{spec}.yml", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message), yaml));
            app.MapGet("/{book}/{chapter}/find-quoted/{spec}.yml", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, quoted: true), yaml));

            app.MapGet("/{book}/{chapter}/find/{spec}.txt", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message), text));
            app.MapGet("/{book}/{chapter}/find-quoted/{spec}.txt", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, quoted: true), text));

            app.MapGet("/{book}/{chapter}/find/{spec}.html", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message), html));
            app.MapGet("/{book}/{chapter}/find-quoted/{spec}.html", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, quoted: true), html));

            app.MapGet("/{book}/{chapter}/find/{spec}.md", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message), mkdn));
            app.MapGet("/{book}/{chapter}/find-quoted/{spec}.md", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, quoted: true), mkdn));

            app.MapGet("/{book}/{chapter}/context-find/{spec}.yml", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, context: true), yaml));
            app.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.yml", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, context: true, quoted: true), yaml));

            app.MapGet("/{book}/{chapter}/context-find/{spec}.txt", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, context: true), text));
            app.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.txt", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, context: true, quoted: true), text));

            app.MapGet("/{book}/{chapter}/context-find/{spec}.html", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, context: true), html));
            app.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.html", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, context: true, quoted: true), html));

            app.MapGet("/{book}/{chapter}/context-find/{spec}.md", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, context: true), mkdn));
            app.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.md", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, context: true, quoted: true), mkdn));


            app.MapGet("/{book}/{chapter}/", (string book, string chapter) => $"unhighlighted {book}:{chapter}");
            app.MapGet("/", () => "Hello AV-Bible user!\nFramework Version: " + Pinshot_RustFFI.VERSION);

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

#if DEBUG
            app.Run("http://localhost:1769");
#else
            app.Run();
#endif
        }
    }
}
