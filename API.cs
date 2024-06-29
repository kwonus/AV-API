using AVXFramework;
using Blueprint.Blue;
using Blueprint.Model.Implicit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pinshot.Blue;
using System;
using System.IO;
using System.Net;
using static Blueprint.Model.Implicit.QFormat;
using static System.Net.Mime.MediaTypeNames;

namespace AVAPI
{
    public class API
    {
        private static API api;
        internal Engine engine;
        internal static Guid ID;
        internal static WebApplication App;
        static API()
        {
            API.api = new();
        }
        private API()
        {
            this.engine = new(API.api);
            QContext.Home = Directory.GetCurrentDirectory();
        }
        public static void Start()
        {
            API.ID = Guid.NewGuid();
        }
        public static void Stop(Guid id)
        {
            if (id == API.ID)
            {
                API.App.StopAsync();
            }
        }
        public static string Heartbeat()
        {
            return API.ID.ToString();
        }
        public static void Main(string[] args)
        {
            API.Start();

            string yaml = "text/x-yaml; charset=utf-8";
            string text = "text/plain; charset=utf-8";
            string html = "text/html; charset=utf-8";
            string mkdn = "text/markdown; charset=utf-8";

            // NOTE:
            // Unlike inproc library, the API has not concept of non-persistent settings.
            // If a setting is passed in, it is explictly saved.

            string message = string.Empty;

            var builder = WebApplication.CreateBuilder(args);
            API.App = builder.Build();

            API.App.MapGet("/exit", (Guid id) => API.Stop(id));
            API.App.MapGet("/status", () => API.Heartbeat());

            API.App.MapGet("/settings.yml", () => API.api.engine.Get_Settings());

            API.App.MapGet("/settings/span",    (uint value)   => API.api.engine.Update_Settings("span", value.ToString()));
            API.App.MapGet("/settings/word",    (string value) => API.api.engine.Update_Settings("word", value));
            API.App.MapGet("/settings/lemma",   (string value) => API.api.engine.Update_Settings("lemma", value));
            API.App.MapGet("/settings/lexicon", (string value) => API.api.engine.Update_Settings("lexicon", value));
            API.App.MapGet("/settings/search",  (string value) => API.api.engine.Update_Settings("lexicon", value));
            API.App.MapGet("/settings/render",  (string value) => API.api.engine.Update_Settings("render", value));
            API.App.MapGet("/settings/format",  (string value) => API.api.engine.Update_Settings("format", value));

            API.App.MapGet("/debug/find/{spec}", (string spec) => API.api.engine.Debug_Find(spec, out message, quoted: false).ToString());
            API.App.MapGet("/debug/find-quoted/{spec}", (string spec) => API.api.engine.Debug_Find(spec, out message, quoted: true).ToString());

            string binary = "application/octet-stream";
            API.App.MapGet("/find/{spec}", (string spec) => Results.Stream(API.api.engine.Binary_Find(spec, out message, quoted: false), binary));
            API.App.MapGet("/find-quoted/{spec}", (string spec) => Results.Stream(API.api.engine.Binary_Find(spec, out message, quoted: true), binary));

            API.App.MapGet("/{book}/{chapter}.yml", (string book, string chapter)
                => Results.Stream(API.api.engine.Get_Chapter(nameof(QFormatVal.YAML), book, chapter, out message), yaml));
            API.App.MapGet("/{book}/{chapter}/find/{spec}.yml", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message), yaml));
            API.App.MapGet("/{book}/{chapter}/find-quoted/{spec}.yml", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, quoted: true), yaml));

            API.App.MapGet("/{book}/{chapter}.txt", (string book, string chapter)
                => Results.Stream(API.api.engine.Get_Chapter(nameof(QFormatVal.TEXT), book, chapter, out message), text));
            API.App.MapGet("/{book}/{chapter}/find/{spec}.txt", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message), text));
            API.App.MapGet("/{book}/{chapter}/find-quoted/{spec}.txt", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, quoted: true), text));

            API.App.MapGet("/{book}/{chapter}.html", (string book, string chapter)
                => Results.Stream(API.api.engine.Get_Chapter(nameof(QFormatVal.HTML), book, chapter, out message), html));
            API.App.MapGet("/{book}/{chapter}/find/{spec}.html", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message), html));
            API.App.MapGet("/{book}/{chapter}/find-quoted/{spec}.html", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, quoted: true), html));

            API.App.MapGet("/{book}/{chapter}/.md", (string book, string chapter)
                => Results.Stream(API.api.engine.Get_Chapter(nameof(QFormatVal.MD), book, chapter, out message), mkdn));
            API.App.MapGet("/{book}/{chapter}/find/{spec}.md", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message), mkdn));
            API.App.MapGet("/{book}/{chapter}/find-quoted/{spec}.md", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, quoted: true), mkdn));

            API.App.MapGet("/{book}/{chapter}/context-find/{spec}.yml", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, context: true), yaml));
            API.App.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.yml", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, context: true, quoted: true), yaml));

            API.App.MapGet("/{book}/{chapter}/context-find/{spec}.txt", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, context: true), text));
            API.App.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.txt", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, context: true, quoted: true), text));

            API.App.MapGet("/{book}/{chapter}/context-find/{spec}.html", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, context: true), html));
            API.App.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.html", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, context: true, quoted: true), html));

            API.App.MapGet("/{book}/{chapter}/context-find/{spec}.md", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, context: true), mkdn));
            API.App.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.md", (string book, string chapter, string spec)
                => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, context: true, quoted: true), mkdn));


            API.App.MapGet("/{book}/{chapter}/", (string book, string chapter) => $"unhighlighted {book}:{chapter}");
            API.App.MapGet("/help/diagrams/{image}.png", (string image) =>
            {
                var path = Path.Combine(AVEngine.HelpFolder, "diagrams", $"{image}.png");
                return Results.File(path, contentType: "image/png");
            });
            API.App.MapGet("/help/css/{style}.css", (string style) =>
            {
                var path = Path.Combine(AVEngine.HelpFolder, "css", $"{style}.css");
                return Results.File(path, contentType: "text/css");
            });
            API.App.MapGet("/help/html-generator/{js}.js", (string js) =>
            {
                var path = Path.Combine(AVEngine.HelpFolder, "html-generator", $"{js}.js");
                return Results.File(path, contentType: "text/javascript");
            });
            API.App.MapGet("/help/{help}.html", (string help) =>
            {
                var path = Path.Combine(AVEngine.HelpFolder, $"{help}.html");
                return Results.File(path, contentType: "text/html");
            });
            API.App.MapGet("/help/{help}", (string help) =>
            {
                var path = AVEngine.GetHelpFile(help);
                return Results.File(path, contentType: "text/html");
            });
            API.App.MapGet("/help", () =>
            {
                var path = AVEngine.GetHelpFile("index.html");
                return Results.File(path, contentType: "text/html");
            });
            API.App.MapGet("/", () => "Hello AV-Bible user!\nAV-Engine Version: " + Pinshot_RustFFI.VERSION);

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

#if DEBUG
            API.App.Run("http://localhost:1769");
#else
            API.App.Run();
#endif
        }
    }
}
