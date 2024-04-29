namespace AVAPI
{
    using AVSearch.Model.Expressions;
    using AVSearch.Model.Results;
    using AVXFramework;
    using AVXLib.Memory;
    using Blueprint.Blue;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Web;

    internal class SpecialCharacters // all characters contain a left-before & left-after and right-before & right-after
    {
        private const int BEFORE_LEFT = 0;
        private const int AFTER_LEFT = 1;
        private const int BEFORE_RIGHT = 2;
        private const int AFTER_RIGHT = 3;

        private string[] SQUIGGLY_OPEN  = ["{", " {", "{ ", "{"];
        private string[] SQUIGGLY_CLOSE = [" }", "}", "}", "} "];
        private string[] PAREN_OPEN     = ["(", " (", "( ", "("];
        private string[] PAREN_CLOSE    = [" )", ")", ")", ") "];
        private string[] BRACE_OPEN     = ["[", " [", "[ ", "["];
        private string[] BRACE_CLOSE    = [" ]", "]", "]", "] "];
        private string[] OR             = [" |", "|", "| ", "|"];
        private string[] AND            = [" &", "&", "& ", "&"];
        private string[] POUND          = ["#", " #", "# ", "#"];
        private string[] FILTER_OP      = ["<", " <", ">", "> "];
        private string[] ASSIGN_OP      = ["+", " +", "+ ", "+"];
        private string[] EQUALS         = [" =", "=", "= ", "="];
        private string[] COMMA          = [" ,", ",", ", ", ","];
        private string[] PERCENT        = [" %", "%", "%", "% "];
        private string[] DOT            = [" .", ".", ". ", "."];
        private string[] NOT            = ["-", " -", "- ", "-"];

        internal readonly Dictionary<string, string> LEFT_FIX;

        internal readonly Dictionary<string, string> RIGHT_FIX;

        public SpecialCharacters()
        {
            this.LEFT_FIX = new() {
                { SQUIGGLY_OPEN [BEFORE_LEFT], SQUIGGLY_OPEN [AFTER_LEFT] },
                { SQUIGGLY_CLOSE[BEFORE_LEFT], SQUIGGLY_CLOSE[AFTER_LEFT] },
                { PAREN_OPEN    [BEFORE_LEFT], PAREN_OPEN    [AFTER_LEFT] },
                { PAREN_CLOSE   [BEFORE_LEFT], PAREN_CLOSE   [AFTER_LEFT] },
                { BRACE_OPEN    [BEFORE_LEFT], BRACE_OPEN    [AFTER_LEFT] },
                { BRACE_CLOSE   [BEFORE_LEFT], BRACE_CLOSE   [AFTER_LEFT] },
                { OR            [BEFORE_LEFT], OR            [AFTER_LEFT] },
                { AND           [BEFORE_LEFT], AND           [AFTER_LEFT] },
                { POUND         [BEFORE_LEFT], POUND         [AFTER_LEFT] },
                { FILTER_OP     [BEFORE_LEFT], FILTER_OP     [AFTER_LEFT] },
                { ASSIGN_OP     [BEFORE_LEFT], ASSIGN_OP     [AFTER_LEFT] },
                { EQUALS        [BEFORE_LEFT], EQUALS        [AFTER_LEFT] },
                { COMMA         [BEFORE_LEFT], COMMA         [AFTER_LEFT] },
                { PERCENT       [BEFORE_LEFT], PERCENT       [AFTER_LEFT] },
                { DOT           [BEFORE_LEFT], DOT           [AFTER_LEFT] },
                { NOT           [BEFORE_LEFT], NOT           [AFTER_LEFT] }
            };
            this.RIGHT_FIX = new() {
                { SQUIGGLY_OPEN [BEFORE_RIGHT], SQUIGGLY_OPEN [AFTER_RIGHT] },
                { SQUIGGLY_CLOSE[BEFORE_RIGHT], SQUIGGLY_CLOSE[AFTER_RIGHT] },
                { PAREN_OPEN    [BEFORE_RIGHT], PAREN_OPEN    [AFTER_RIGHT] },
                { PAREN_CLOSE   [BEFORE_RIGHT], PAREN_CLOSE   [AFTER_RIGHT] },
                { BRACE_OPEN    [BEFORE_RIGHT], BRACE_OPEN    [AFTER_RIGHT] },
                { BRACE_CLOSE   [BEFORE_RIGHT], BRACE_CLOSE   [AFTER_RIGHT] },
                { OR            [BEFORE_RIGHT], OR            [AFTER_RIGHT] },
                { AND           [BEFORE_RIGHT], AND           [AFTER_RIGHT] },
                { POUND         [BEFORE_RIGHT], POUND         [AFTER_RIGHT] },
                { FILTER_OP     [BEFORE_RIGHT], FILTER_OP     [AFTER_RIGHT] },
                { ASSIGN_OP     [BEFORE_RIGHT], ASSIGN_OP     [AFTER_RIGHT] },
                { EQUALS        [BEFORE_RIGHT], EQUALS        [AFTER_RIGHT] },
                { COMMA         [BEFORE_RIGHT], COMMA         [AFTER_RIGHT] },
                { PERCENT       [BEFORE_RIGHT], PERCENT       [AFTER_RIGHT] },
                { DOT           [BEFORE_RIGHT], DOT           [AFTER_RIGHT] },
                { NOT           [BEFORE_RIGHT], NOT           [AFTER_RIGHT] }
            };
        }
    }

    public class Engine: AVEngine
    {
        private SpecialCharacters special;
        private static string[] elipsis = ["..."];
        private static char[] whitespace = [' ', '\t'];
        private static string[] directives_or_scopes = ["||", ">", "?>", "::", "<"];
        private API api;
        Engine()
        {
            ;
        }
        public static string Squench(string input)
        {
            StringBuilder output = new(input.Length);
            bool spaced = true;
            foreach (char c in input.Trim())
            {
                if (c == ' ')
                {
                    if (spaced)
                        continue;
                    spaced = true;
                }
                else
                {
                    spaced = true;
                }
                output.Append(c);
            }
            return output.ToString();
        }
        public string Normalize(string input)
        {
            bool done = true;
            int len = input.Length;

            string output = input;

            AGAIN:
            foreach (var pair in this.special.LEFT_FIX)
            {
                output = output.Replace(pair.Key, pair.Value);
                if (len > output.Length)
                    done = false;

                len = output.Length;
            }
            foreach (var pair in this.special.RIGHT_FIX)
            {
                output = output.Replace(pair.Key, pair.Value);
                if (len > output.Length)
                    done = false;

                len = output.Length;
            }
            if (!done)
            {
                done = true;
                goto AGAIN;
            }
            return Engine.Squench(output);
        }
        public StringBuilder Debug_Find(string spec, out string message, bool quoted = false)
        {
            StringBuilder payload = new(2048);
            HashSet<UInt32> coordinates = this.Find(spec, out message, quoted);
            UInt32 len = (UInt32)coordinates.Count;
            payload.Append(len.ToString("X8"));

            UInt32 book_chapter = 0;
            foreach (UInt32 bcv in from coord in coordinates orderby coord ascending select coord)
            {
                UInt32 bc = (UInt32)(bcv & 0xFFFF00);
                if (bc != book_chapter)
                {
                    book_chapter = bc;
                    if (book_chapter > 0)
                        payload.AppendLine();
                }
                else
                {
                    payload.Append('_');
                }
                payload.Append(bcv.ToString("X6"));
            }
            return payload;
        }
        public Stream Binary_Find(string spec, out string message, bool quoted = false)
        {
            HashSet<UInt32> coordinates = this.Find(spec, out message, quoted);
            UInt32 len = (UInt32) coordinates.Count;
            byte[] lenBytes = BitConverter.GetBytes(len);

            byte[] values = new byte[lenBytes.Length + (coordinates.Count * 3)];
            int i = 0;
            foreach (byte b in lenBytes)
            {
                values[i] = lenBytes[i];
                i++;
            }
            foreach (UInt32 bcv in from coord in coordinates orderby coord ascending select coord)
            {
                byte b = (byte) (bcv >> 16);
                byte c = (byte) ((bcv & 0x00FF00) >> 8);
                byte v = (byte) (bcv & 0x0000FF);
                values[i++] = b;
                values[i++] = c;
                values[i++] = v;
            }
            MemoryStream payload = new(values);

            return payload;
        }
        public Stream Detail_Find(string spec, string book, string chapter, out string message, bool quoted = false)
        {
            string input = string.Empty;
            string[] parts = spec.Split(directives_or_scopes, StringSplitOptions.None);
            input = parts[0].Trim() + "<" + book + " " + chapter + " :: yaml";

            if (spec.Length > 0 && spec[0] != '@')
            {
                message = "ok";
                input = HttpUtility.UrlDecode(input).Replace('_', ' ');
                string normalized = this.Normalize(input);
                var tuple = this.Execute(normalized);

                if (!tuple.message.Equals("ok"))
                {
                    message = tuple.message;
                }
                else if (tuple.stmt != null && tuple.stmt.Commands != null && tuple.stmt.Commands.Context != null)
                {
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
                string normalized = this.Normalize(input);
                var tuple = this.Execute(normalized);

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
                            UInt32 start = match.Start.elements >> 8;
                            UInt32 until = match.Until.elements >> 8;
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
        EMPTY:
            message = "Search specification was empty";
        DONE:
            return matches;
        }

        internal Engine(API api)
        {
            this.api = api;
            this.special = new();
        }
    }
}
