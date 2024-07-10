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
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using YamlDotNet.Core.Tokens;
using static Blueprint.Model.Implicit.QFormat;
using static System.Net.Mime.MediaTypeNames;

namespace AVAPI
{
    public class API: HostedWebServer
    {
        private static API api;
        internal Engine engine;

        static API()
        {
            API.api = new();
        }
        public API()
        {
            this.engine = new(API.api);
            QContext.Home = Directory.GetCurrentDirectory();
        }

        protected override bool Launch(string url)
        {
            string yaml = "text/x-yaml; charset=utf-8";
            string text = "text/plain; charset=utf-8";
            string html = "text/html; charset=utf-8";
            string mkdn = "text/markdown; charset=utf-8";

            string message = string.Empty;

            try
            {
                var builder = WebApplication.CreateBuilder();
                this.App = builder.Build();

                this.App.MapGet("/revision.yml", () => API.api.engine.Get_Revision());
                this.App.MapGet("/settings.yml", () => API.api.engine.Get_Settings());

                this.App.MapGet("/settings/span", (uint value) => API.api.engine.Update_Settings("span", value.ToString()));
                this.App.MapGet("/settings/word", (string value) => API.api.engine.Update_Settings("word", value));
                this.App.MapGet("/settings/lemma", (string value) => API.api.engine.Update_Settings("lemma", value));
                this.App.MapGet("/settings/lexicon", (string value) => API.api.engine.Update_Settings("lexicon", value));
                this.App.MapGet("/settings/search", (string value) => API.api.engine.Update_Settings("lexicon", value));
                this.App.MapGet("/settings/render", (string value) => API.api.engine.Update_Settings("render", value));
                this.App.MapGet("/settings/format", (string value) => API.api.engine.Update_Settings("format", value));

                this.App.MapGet("/debug/find/{spec}", (string spec) => API.api.engine.Debug_Find(spec, out message, quoted: false).ToString());
                this.App.MapGet("/debug/find-quoted/{spec}", (string spec) => API.api.engine.Debug_Find(spec, out message, quoted: true).ToString());

                string binary = "application/octet-stream";
                this.App.MapGet("/find/{spec}", (string spec) => Results.Stream(API.api.engine.Binary_Find(spec, out message, quoted: false), binary));
                this.App.MapGet("/find-quoted/{spec}", (string spec) => Results.Stream(API.api.engine.Binary_Find(spec, out message, quoted: true), binary));

                this.App.MapGet("/{book}/{chapter}.yml", (string book, string chapter)
                    => Results.Stream(API.api.engine.Get_Chapter(nameof(QFormatVal.YAML), book, chapter, out message), yaml));
                this.App.MapGet("/{book}/{chapter}/find/{spec}.yml", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message), yaml));
                this.App.MapGet("/{book}/{chapter}/find-quoted/{spec}.yml", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, quoted: true), yaml));

                this.App.MapGet("/{book}/{chapter}.txt", (string book, string chapter)
                    => Results.Stream(API.api.engine.Get_Chapter(nameof(QFormatVal.TEXT), book, chapter, out message), text));
                this.App.MapGet("/{book}/{chapter}/find/{spec}.txt", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message), text));
                this.App.MapGet("/{book}/{chapter}/find-quoted/{spec}.txt", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, quoted: true), text));

                this.App.MapGet("/{book}/{chapter}.html", (string book, string chapter)
                    => Results.Stream(API.api.engine.Get_Chapter(nameof(QFormatVal.HTML), book, chapter, out message), html));
                this.App.MapGet("/{book}/{chapter}/find/{spec}.html", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message), html));
                this.App.MapGet("/{book}/{chapter}/find-quoted/{spec}.html", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, quoted: true), html));

                this.App.MapGet("/{book}/{chapter}/.md", (string book, string chapter)
                    => Results.Stream(API.api.engine.Get_Chapter(nameof(QFormatVal.MD), book, chapter, out message), mkdn));
                this.App.MapGet("/{book}/{chapter}/find/{spec}.md", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message), mkdn));
                this.App.MapGet("/{book}/{chapter}/find-quoted/{spec}.md", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, quoted: true), mkdn));

                this.App.MapGet("/{book}/{chapter}/context-find/{spec}.yml", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, context: true), yaml));
                this.App.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.yml", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.YAML), spec, book, chapter, out message, context: true, quoted: true), yaml));

                this.App.MapGet("/{book}/{chapter}/context-find/{spec}.txt", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, context: true), text));
                this.App.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.txt", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.TEXT), spec, book, chapter, out message, context: true, quoted: true), text));

                this.App.MapGet("/{book}/{chapter}/context-find/{spec}.html", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, context: true), html));
                this.App.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.html", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.HTML), spec, book, chapter, out message, context: true, quoted: true), html));

                this.App.MapGet("/{book}/{chapter}/context-find/{spec}.md", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, context: true), mkdn));
                this.App.MapGet("/{book}/{chapter}/context-find-quoted/{spec}.md", (string book, string chapter, string spec)
                    => Results.Stream(API.api.engine.Detail_Find(nameof(QFormatVal.MD), spec, book, chapter, out message, context: true, quoted: true), mkdn));


                this.App.MapGet("/{book}/{chapter}/", (string book, string chapter) => $"unhighlighted {book}:{chapter}");
                this.App.MapGet("/help/diagrams/{image}.png", (string image) =>
                {
                    var path = Path.Combine(AVEngine.HelpFolder, "diagrams", $"{image}.png");
                    return Results.File(path, contentType: "image/png");
                });
                this.App.MapGet("/help/css/{style}.css", (string style) =>
                {
                    var path = Path.Combine(AVEngine.HelpFolder, "css", $"{style}.css");
                    return Results.File(path, contentType: "text/css");
                });
                this.App.MapGet("/help/html-generator/{js}.js", (string js) =>
                {
                    var path = Path.Combine(AVEngine.HelpFolder, "html-generator", $"{js}.js");
                    return Results.File(path, contentType: "text/javascript");
                });
                this.App.MapGet("/help/{help}.html", (string help) =>
                {
                    var path = Path.Combine(AVEngine.HelpFolder, $"{help}.html");
                    return Results.File(path, contentType: "text/html");
                });
                this.App.MapGet("/help/{help}", (string help) =>
                {
                    var path = AVEngine.GetHelpFile(help);
                    return Results.File(path, contentType: "text/html");
                });
                this.App.MapGet("/help", () =>
                {
                    var path = AVEngine.GetHelpFile("index.html");
                    return Results.File(path, contentType: "text/html");
                });
                this.App.MapGet("/", () => "Hello AV-Bible user!\nAV-Engine Version: " + Pinshot_RustFFI.VERSION);

                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                this.App.Run("http://localhost:1769");
                this.IsRunning = true;

                return true;
            }
            catch (Exception ex)
            {
                this.LastException = ex;
                this.ErrorMessage = ex.Message;
                this.IsRunning = false;
            }
            return false;
        }
    }
}
