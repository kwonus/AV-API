﻿namespace AVAPI
{
    using AVSearch.Model.Results;
    using AVXFramework;
    using AVXLib.Memory;
    using Blueprint.Blue;
    using Pinshot.Blue;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;
    using YamlDotNet.Core.Tokens;
    using static Blueprint.Model.Implicit.QFormat;

    public class Engine: AVEngine
    {
//      private SpecialCharacters special;
        private static string[] elipsis = ["..."];
        private static char[] whitespace = [' ', '\t'];
        private static string[] directives_or_scopes = ["||", ">", "?>", "::", ":=", "<"];
        private API api;
        Engine()
        {
            ;
        }
        public string GetSettingsAsReturnString(QSettings settings)
        {
            var yaml = settings.AsYaml();
            StringBuilder lines = new StringBuilder();
            HashSet<string> dedup = new();
            foreach (var line in yaml)
                if (!dedup.Contains(line))
                {
                    lines.AppendLine(line);
                    dedup.Add(line);
                }

            return lines.ToString();
        }
        public string Get_Revision()
        {
            return "revision: " + Pinshot_RustFFI.VERSION;
        }
        public string Get_Settings()
        {
            QSettings settings = new QSettings(QContext.SettingsFile);

            return GetSettingsAsReturnString(settings);
        }
        public string Update_Settings(string key, string value)
        {
            QSettings settings = new QSettings(QContext.SettingsFile);
            switch (key.ToLower())
            {
                case "span":            settings.Span = new Blueprint.Model.Implicit.QSpan(value);
                                        break;

                case "lexicon.search":
                case "search":
                case "lexicon":         settings.Lexicon.Domain = new Blueprint.Model.Implicit.QLexicalDomain(value);
                                        break;

                case "lexicon.render":
                case "render":          settings.Lexicon.Render = new Blueprint.Model.Implicit.QLexicalDisplay(value);
                                        break;

                case "similarity.word":
                case "word":            settings.Similarity.Word = new Blueprint.Model.Implicit.QSimilarityWord(value);
                                        break;

                case "similarity.lemma:":
                case "lemma":           settings.Similarity.Lemma = new Blueprint.Model.Implicit.QSimilarityLemma(value);
                                        break;

                case "format:":         settings.Format = new Blueprint.Model.Implicit.QFormat(value);
                                        break;
            }
            settings.Update();

            return GetSettingsAsReturnString(settings);
        }
        // TOTO: TO DO: BUG: Scope-Only selection statements are NOT returning any results
        public Stream Get_Chapter(string format, string book, string chapter, out string message, bool context = false)
        {
            string op = context ? " :: " : " := ";
            //string input = "<" + book + " " + chapter + op + format;  //  "*" is a hack-workaround to mask bug in scope-only selection statements
            string input = "* <" + book + " " + chapter + op + format;  //  "*" is a hack-workaround to mask bug in scope-only selection statements

            message = "ok";
            input = HttpUtility.UrlDecode(input).Replace('_', ' ');
            //string normalized = this.Normalize(input);
            var tuple = this.Execute(input);

            if (!tuple.message.Equals("ok"))
            {
                message = tuple.message;
            }
            else if (tuple.stmt != null && tuple.stmt.Commands != null && tuple.stmt.Commands.Context != null)
            {
                tuple.stmt.Commands.Context.InternalExportStream.Position = 0;
                return tuple.stmt.Commands.Context.InternalExportStream;
            }
            else
            {
                message = "Unexpected status during export stream generation";
            }

            return new MemoryStream();
        }

        public StringBuilder Debug_Find(string spec, out string message, bool quoted = false)
        {
            StringBuilder payload = new(2048);
            HashSet<UInt32> coordinates = this.Find(spec, out message, quoted);


            UInt32 book_chapter = 0;
            foreach (UInt32 bcv in from coord in coordinates orderby coord ascending select coord)
            {
                UInt32 bc = (UInt32)(bcv & 0xFFFF00);
                if (bc != book_chapter)
                {
                    if (book_chapter > 0)
                        payload.AppendLine();
                    book_chapter = bc;
                }
                else
                {
                    payload.Append('_');
                }
                payload.Append(bcv.ToString("X6"));
            }
            if (book_chapter > 0)
                payload.AppendLine();
            payload.Append(0.ToString("X6"));   // null-terminate
            return payload;
        }
        public Stream Binary_Find(string spec, out string message, bool quoted = false)
        {
            HashSet<UInt32> coordinates = this.Find(spec, out message, quoted);
            UInt32 len = (UInt32) coordinates.Count;

            byte[] values = new byte[(1+len) * 3];
            int i = 0;

            foreach (UInt32 bcv in from coord in coordinates orderby coord ascending select coord)
            {
                byte b = (byte) (bcv >> 16);
                byte c = (byte) ((bcv & 0x00FF00) >> 8);
                byte v = (byte) (bcv & 0x0000FF);
                values[i++] = b;
                values[i++] = c;
                values[i++] = v;
            }
            values[i++] = 0;
            values[i++] = 0;
            values[i++] = 0;
            MemoryStream payload = new(values);
            return payload;
        }
        public Stream Detail_Find(string format, string spec, string book, string chapter, out string message, bool quoted = false, bool context = false)
        {
            string input = string.Empty;
            string[] parts = spec.Split(directives_or_scopes, StringSplitOptions.None);
            string op = context ? " :: " : " := ";

            input = parts[0].Trim() + "<" + book + " " + chapter + op + format;

            if (spec.Length > 0 && spec[0] != '@')
            {
                message = "ok";
                input = HttpUtility.UrlDecode(input).Replace('_', ' ');
                //string normalized = this.Normalize(input);
                var tuple = this.Execute(input);

                if (!tuple.message.Equals("ok"))
                {
                    message = tuple.message;
                }
                else if (tuple.stmt != null && tuple.stmt.Commands != null && tuple.stmt.Commands.Context != null)
                {
                    tuple.stmt.Commands.Context.InternalExportStream.Position = 0;
                    return tuple.stmt.Commands.Context.InternalExportStream;
                }
                else
                {
                    message = "Unexpected status during export stream generation";
                }
            }
            else
            {
                message = "Invalid search specification was provided";
            }
            return new MemoryStream();
        }
        private HashSet<UInt32> Find(string spec, out string message, bool quoted = false)
        {
            HashSet<UInt32> matches = new();

            message = "ok";
            string input = HttpUtility.UrlDecode(spec).Replace('_', ' ');
            if (!string.IsNullOrWhiteSpace(input))
            {
                string squenched = SpecialCharacters.Squench(input);
                var tuple = this.Execute(squenched);

                bool ok = !string.IsNullOrWhiteSpace(tuple.message);
                if (ok)
                {
                    ok = tuple.message.Equals("ok", StringComparison.InvariantCultureIgnoreCase);
                    if (!ok)
                        message = tuple.message;
                }
                if (ok && (tuple.search != null && tuple.search.Expression != null))
                {
                    foreach (QueryBook book in tuple.search.Expression.Books.Values)
                    {
                        foreach (var match in book.Matches.Values)
                        {
                            UInt32 start = match.Start.AsUInt32() >> 8;
                            UInt32 until = match.Until.AsUInt32() >> 8;
                            if (!matches.Contains(start))
                            {
                                matches.Add(start);
                            }
                            if ((start != until) && !matches.Contains(until))
                            {
                                if (start + 1 == until)
                                {
                                    matches.Add(until);
                                }
                                else
                                {
                                    if (match.Start.C == match.Until.C)
                                    {
                                        for (UInt32 bcv = start + 1; bcv <= until; bcv++)
                                            if (!matches.Contains(bcv))
                                                matches.Add(bcv);
                                    }
                                    else
                                    {
                                        for (UInt32 bcv = (UInt32)((until & 0xFFFF00) + 1); bcv <= until; bcv++) // start at verse one (if there is a whole between chapters for long spans, we will miss those verses; and we will miss the ending verses of the initial chapet if it was no the last verse of the chapter // both are edge cases that we do not expect to be common
                                            if (!matches.Contains(bcv))
                                                matches.Add(bcv);
                                    }
                                }
                            }
                        }
                    }
                    goto DONE;
                }
                else
                {
                    message = "Non-supported syntax encountered";
                    goto DONE;
                }
            }
            message = "Search specification was empty";
        DONE:
            return matches;
        }

        internal Engine(API api)
        {
            this.api = api;
        }
    }
}
