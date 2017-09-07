using System;

namespace Jazz2
{
    public enum LogType
    {
        Verbose,
        Info,
        Warning,
        Error
    }

    public static class Log
    {
        private static int indent;
        private static string[] lastLogLines = new string[3];
        private static int lastLogLineIndex;

        public static void PushIndent()
        {
            lock (lastLogLines) {
                indent++;
            }
        }

        public static void PopIndent()
        {
            lock (lastLogLines) {
                if (indent > 0) {
                    indent--;
                }
            }
        }

        public static void Write(LogType type, string formattedLine)
        {
            if (string.IsNullOrEmpty(formattedLine)) {
                return;
            }

            lock (lastLogLines) {
                ConsoleColor clrBg = Console.BackgroundColor;
                ConsoleColor clrFg = Console.ForegroundColor;

                bool highlight = IsHighlightLine(formattedLine);

                // If we're writing the same kind of text again, "grey out" the repeating parts
                int beginGreyLength = 0;
                int endGreyLength = 0;
                if (!highlight) {
                    for (int i = 0; i < lastLogLines.Length; i++) {
                        string lastLogLine = lastLogLines[i] ?? string.Empty;
                        beginGreyLength = Math.Max(beginGreyLength, GetEqualBeginChars(lastLogLine, formattedLine));
                        endGreyLength = Math.Max(endGreyLength, GetEqualEndChars(lastLogLine, formattedLine));
                    }
                    if (beginGreyLength == formattedLine.Length) {
                        endGreyLength = 0;
                    }
                    if (beginGreyLength + endGreyLength >= formattedLine.Length) {
                        endGreyLength = 0;
                    }
                }

                // Dot
                SetBrightConsoleColor(type, highlight);
                Console.Write(new string(' ', indent * 2) + (ConsoleUtils.SupportsUnicode ? " · " : " ˙ "));

                // Dark beginning
                if (beginGreyLength != 0) {
                    SetDarkConsoleColor(type);
                    Console.Write(formattedLine.Substring(0, beginGreyLength));
                }

                // Bright main part
                SetBrightConsoleColor(type, highlight);
                Console.Write(formattedLine.Substring(beginGreyLength, formattedLine.Length - beginGreyLength - endGreyLength));

                // Dark ending
                if (endGreyLength != 0) {
                    SetDarkConsoleColor(type);
                    Console.Write(formattedLine.Substring(formattedLine.Length - endGreyLength, endGreyLength));
                }

                // End the current line
                Console.WriteLine();

                lastLogLines[lastLogLineIndex] = formattedLine;
                lastLogLineIndex = (lastLogLineIndex + 1) % lastLogLines.Length;
                Console.ForegroundColor = clrFg;
                Console.BackgroundColor = clrBg;
            }
        }

        private static void SetDarkConsoleColor(LogType type)
        {
            switch (type) {
                default:
                case LogType.Info: Console.ForegroundColor = ConsoleColor.DarkGray; break;
                case LogType.Warning: Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                case LogType.Error: Console.ForegroundColor = ConsoleColor.DarkRed; break;
                case LogType.Verbose: Console.ForegroundColor = ConsoleColor.DarkGray; break;
            }
        }

        private static void SetBrightConsoleColor(LogType type, bool highlight)
        {
            switch (type) {
                default:
                case LogType.Info:
                    Console.ForegroundColor = highlight ?
                                              ConsoleColor.White :
                                              ConsoleColor.Gray; break;
                case LogType.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case LogType.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                case LogType.Verbose: Console.ForegroundColor = ConsoleColor.DarkGray; break;
            }
        }

        private static bool IsHighlightLine(string line)
        {
            // If it's an indented line, don't highlight it
            if (indent != 0) return false;

            // If the line ends with three dots, assume that it's the header of a series of actions
            if (line.EndsWith("...")) return true;

            return false;
        }

        private static int GetEqualBeginChars(string a, string b)
        {
            int minLen = Math.Min(a.Length, b.Length);
            int lastBreakCount = 0;
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length) {
                // Skip whitespace / indentation
                if (a[i] == ' ') {
                    ++i;
                    continue;
                }
                if (b[j] == ' ') {
                    ++j;
                    lastBreakCount = j;
                    continue;
                }

                if (a[i] != b[j]) {
                    return lastBreakCount;
                }
                if (!char.IsLetterOrDigit(b[j])) {
                    lastBreakCount = j + 1;
                }

                ++i;
                ++j;
            }
            return minLen;
        }

        private static int GetEqualEndChars(string a, string b)
        {
            int minLen = Math.Min(a.Length, b.Length);
            int lastBreakCount = 0;
            for (int i = 0; i < minLen; i++) {
                if (a[a.Length - 1 - i] != b[b.Length - 1 - i]) {
                    return lastBreakCount;
                }
                if (!char.IsLetterOrDigit(a[a.Length - 1 - i])) {
                    lastBreakCount = i + 1;
                }
            }
            return minLen;
        }
    }
}