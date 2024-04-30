using System.Collections.Generic;
using System.Text;

namespace AVAPI
{
    public class SpecialCharacters // all characters contain a left-before & left-after and right-before & right-after
    {
        private const int BEFORE_LEFT = 0;
        private const int AFTER_LEFT = 1;
        private const int BEFORE_RIGHT = 2;
        private const int AFTER_RIGHT = 3;

        private string[] SQUIGGLY_OPEN = ["{", " {", "{ ", "{"];
        private string[] SQUIGGLY_CLOSE = [" }", "}", "}", "} "];
        private string[] PAREN_OPEN = ["(", " (", "( ", "("];
        private string[] PAREN_CLOSE = [" )", ")", ")", ") "];
        private string[] BRACE_OPEN = ["[", " [", "[ ", "["];
        private string[] BRACE_CLOSE = [" ]", "]", "]", "] "];
        private string[] OR = [" |", "|", "| ", "|"];
        private string[] AND = [" &", "&", "& ", "&"];
        private string[] POUND = ["#", " #", "# ", "#"];
        private string[] FILTER_OP = ["<", " <", ">", "> "];
        private string[] ASSIGN_OP = ["+", " +", "+ ", "+"];
        private string[] EQUALS = [" =", "=", "= ", "="];
        private string[] COMMA = [" ,", ",", ", ", ","];
        private string[] PERCENT = [" %", "%", "%", "% "];
        private string[] DOT = [" .", ".", ". ", "."];
        private string[] NOT = ["-", " -", "- ", "-"];

        internal readonly Dictionary<string, string> LEFT_FIX;

        internal readonly Dictionary<string, string> RIGHT_FIX;

        private SpecialCharacters()
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
        public string Normalize(string input)
        {
            bool done = true;
            int len = input.Length;

            string output = input;

        AGAIN:
            foreach (var pair in this.LEFT_FIX)
            {
                output = output.Replace(pair.Key, pair.Value);
                if (len > output.Length)
                    done = false;

                len = output.Length;
            }
            foreach (var pair in this.RIGHT_FIX)
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
            return SpecialCharacters.Squench(output);
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
    }
}
