using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

        private static bool initialCursorVisible;
        private static bool activeInput;
        private static int activeInputX;
        private static int activeInputY;

        private static History inputHistory;

        private static Thread inputThread;
        private static ConsoleCtrlDelegate ctrlHandlerRef;

        static Log()
        {
            if (!ConsoleUtils.IsOutputRedirected) {
                initialCursorVisible = Console.CursorVisible;
                Console.CursorVisible = false;

                try {
                    ctrlHandlerRef = OnCtrlHandler;
                    SetConsoleCtrlHandler(ctrlHandlerRef, true);
                } catch {
                    // Nothing to do...
                }
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            }
        }

        private static bool Cleanup()
        {
            Console.CursorVisible = initialCursorVisible;
            Console.ResetColor();

            if (activeInput) {
                activeInput = false;
                inputThread.Abort();
                return true;
            } else {
                return false;
            }
        }

        private static bool OnCtrlHandler(int type)
        {
            return Cleanup();
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Cleanup();
        }

        public static string FetchLine(Func<string, string> suggestionsCallback)
        {
            if (ConsoleUtils.IsOutputRedirected) {
                return Console.ReadLine();
            }

            inputThread = Thread.CurrentThread;
            activeInput = true;

            ShowActivePrompt();

            Console.CursorVisible = true;

            if (inputHistory == null) {
                inputHistory = new History();
            }

            inputHistory.CursorToEnd();
            inputHistory.Append("");

            try {
                StringBuilder sb = new StringBuilder();
                int bufferPosition = 0;
                int bufferLastLength = 0;
                string activeSuggestion = null;
                int consoleWidth;

                while (activeInput) {
                    ConsoleModifiers mod;

                    ConsoleKeyInfo cki = Console.ReadKey(true);
                    if (cki.Key == ConsoleKey.Escape) {
                        cki = Console.ReadKey(true);
                        mod = ConsoleModifiers.Alt;
                    } else {
                        mod = cki.Modifiers;
                    }

                    bool changed = false;

                    // Process input
                    switch (cki.Key) {
                        case ConsoleKey.Enter: {
                            if (sb.Length > 0) {
                                activeInput = false;
                            }
                            break;
                        }
                        case ConsoleKey.LeftArrow: {
                            if (bufferPosition > 0) {
                                bufferPosition--;
                            }
                            break;
                        }
                        case ConsoleKey.RightArrow: {
                            if (bufferPosition < sb.Length) {
                                bufferPosition++;
                            } else if (activeSuggestion != null) {
                                sb.Clear();
                                sb.Append(activeSuggestion);
                                bufferPosition = sb.Length;
                                changed = true;
                            }
                            break;
                        }
                        case ConsoleKey.UpArrow: {
                            if (inputHistory != null && inputHistory.IsPreviousAvailable) {
                                inputHistory.UpdateCurrent(sb.ToString());
                                sb.Clear();
                                sb.Append(inputHistory.GetPrevious());
                                bufferPosition = sb.Length;
                                changed = true;
                            }
                            break;
                        }
                        case ConsoleKey.DownArrow: {
                            if (inputHistory != null && inputHistory.IsNextAvailable) {
                                inputHistory.UpdateCurrent(sb.ToString());
                                sb.Clear();
                                sb.Append(inputHistory.GetNext());
                                bufferPosition = sb.Length;
                                changed = true;
                            }
                            break;
                        }
                        case ConsoleKey.Home: {
                            bufferPosition = 0;
                            break;
                        }
                        case ConsoleKey.End: {
                            bufferPosition = sb.Length;
                            break;
                        }
                        case ConsoleKey.Backspace: {
                            if (bufferPosition > 0) {
                                bufferPosition--;
                                sb.Remove(bufferPosition, 1);
                                changed = true;
                            }
                            break;
                        }
                        case ConsoleKey.Delete: {
                            if (bufferPosition < sb.Length) {
                                sb.Remove(bufferPosition, 1);
                                changed = true;
                            }
                            break;
                        }
                        case ConsoleKey.Tab: {
                            if (activeSuggestion != null) {
                                sb.Clear();
                                sb.Append(activeSuggestion);
                                bufferPosition = sb.Length;
                                changed = true;
                            }
                            break;
                        }

                        default: {
                            if (mod == ConsoleModifiers.Control && cki.Key == ConsoleKey.L) {
                                // CTRL+L: Clear screen
                                Console.Clear();
                                Console.SetCursorPosition(0, 0);
                                ShowActivePrompt();
                                changed = true;
                            } else if (cki.KeyChar != (char)0 && !char.IsControl(cki.KeyChar)) {
                                sb.Insert(bufferPosition, cki.KeyChar);
                                bufferPosition++;
                                changed = true;
                            }
                            break;
                        }
                    }

                    // Fetch suggestions
                    if (changed) {
                        if (suggestionsCallback != null) {
                            activeSuggestion = suggestionsCallback(sb.ToString());
                        }
                    }

                    // Render
                    if (activeInput) {
                        lock (lastLogLines) {
                            if (changed) {
                                Console.SetCursorPosition(activeInputX, activeInputY);

                                Console.ForegroundColor = ConsoleColor.Gray;
                                string inputToRender = sb.ToString();
                                Console.Write(inputToRender);
                                int currentLength = sb.Length;

                                if (activeSuggestion != null && activeSuggestion.Length > inputToRender.Length) {
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.Write(activeSuggestion.Substring(inputToRender.Length));
                                    currentLength = activeSuggestion.Length;
                                }

                                Console.ResetColor();

                                int extraLength = bufferLastLength - currentLength;
                                if (extraLength > 0) {
                                    for (int i = 0; i <= extraLength; i++) {
                                        Console.Write(' ');
                                    }
                                }

                                bufferLastLength = currentLength;
                            }

                            consoleWidth = Console.BufferWidth;
                            Console.SetCursorPosition((activeInputX + bufferPosition) % consoleWidth, activeInputY + (activeInputX + bufferPosition) / consoleWidth);
                        }

                        changed = false;
                    }
                }

                // Set cursor position to the end and create a new line
                consoleWidth = Console.BufferWidth;
                Console.SetCursorPosition((activeInputX + sb.Length) % consoleWidth, activeInputY + (activeInputX + sb.Length) / consoleWidth);
                Console.WriteLine();

                Console.CursorVisible = false;

                string input = sb.ToString();

                if (string.IsNullOrEmpty(input)) {
                    inputHistory.Discard();
                } else {
                    inputHistory.Accept(input);
                }

                activeInput = false;
                inputThread = null;
                return input;
            } catch (ThreadAbortException) {
                Thread.ResetAbort();

                activeInput = false;
                inputThread = null;
                return null;
            }
        }

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

        public static void Write(LogType type, string formattedLine, bool pushIndent = false)
        {
            if (string.IsNullOrEmpty(formattedLine)) {
                lock (lastLogLines) {
                    // End the current line
                    Console.WriteLine();

                    if (activeInput) {
                        ShowActivePrompt();
                    }
                }
                return;
            }

            lock (lastLogLines) {
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

                if (activeInput) {
                    Console.SetCursorPosition(0, activeInputY);
                }

                // Dot
                if (pushIndent && ConsoleUtils.SupportsUnicode) {
                    SetBrightConsoleColor(type, false);
                    Console.Write(new string(' ', indent * 2) + " ◿ ");
                } else {
                    SetBrightConsoleColor(type, highlight);
                    Console.Write(new string(' ', indent * 2) + (ConsoleUtils.SupportsUnicode ? " · " : " ˙ "));
                }

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

                Console.ResetColor();

                // End the current line
                Console.WriteLine();

                lastLogLines[lastLogLineIndex] = formattedLine;
                lastLogLineIndex = (lastLogLineIndex + 1) % lastLogLines.Length;

                if (pushIndent) {
                    PushIndent();
                }

                if (activeInput) {
                    ShowActivePrompt();
                }
            }
        }

        public static void Write(LogType type, string category, string formattedLine)
        {
            if (string.IsNullOrEmpty(formattedLine)) {
                return;
            }

            lock (lastLogLines) {
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

                if (activeInput) {
                    Console.SetCursorPosition(0, activeInputY);
                }

                // Dot
                SetBrightConsoleColor(type, highlight);
                Console.Write(new string(' ', indent * 2) + (ConsoleUtils.SupportsUnicode ? " · " : " ˙ "));

                SetBrightConsoleColor(type, false);
                Console.Write(category);

                // Dark beginning
                if (beginGreyLength != 0) {
                    SetDarkConsoleColor(LogType.Info);
                    Console.Write(formattedLine.Substring(0, beginGreyLength));
                }

                // Bright main part
                SetBrightConsoleColor(LogType.Info, false);
                Console.Write(formattedLine.Substring(beginGreyLength, formattedLine.Length - beginGreyLength - endGreyLength));

                // Dark ending
                if (endGreyLength != 0) {
                    SetDarkConsoleColor(LogType.Info);
                    Console.Write(formattedLine.Substring(formattedLine.Length - endGreyLength, endGreyLength));
                }

                // End the current line
                Console.WriteLine();

                lastLogLines[lastLogLineIndex] = formattedLine;
                lastLogLineIndex = (lastLogLineIndex + 1) % lastLogLines.Length;
                Console.ResetColor();

                if (activeInput) {
                    ShowActivePrompt();
                }
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
            if (indent != 0) {
                return false;
            }

            // If the line ends with three dots, assume that it's the header of a series of actions
            if (line.EndsWith("...")) {
                return true;
            }

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

        private static void ShowActivePrompt()
        {
            Console.CursorLeft = 0;

            if (ConsoleUtils.SupportsUnicode) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("²");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("›");
            } else {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(">");
            }

            Console.ResetColor();

            // Refresh initial cursor position
            activeInputX = Console.CursorLeft;
            activeInputY = Console.CursorTop;
        }

        private class History
        {
            private string[] lines = new string[16];
            private int head;
            private int tail;
            private int current;
            private int count;

            public bool IsPreviousAvailable
            {
                get
                {
                    if (count == 0) {
                        return false;
                    }
                    int next = current - 1;
                    if (next < 0) {
                        next = count - 1;
                    }
                    if (next == head) {
                        return false;
                    }
                    return true;
                }
            }

            public bool IsNextAvailable
            {
                get
                {
                    if (count == 0) {
                        return false;
                    }
                    int next = (current + 1) % lines.Length;
                    if (next == head) {
                        return false;
                    }
                    return true;
                }
            }

            public void Append(string s)
            {
                lines[head] = s;
                head = (head + 1) % lines.Length;
                if (head == tail) {
                    tail = (tail + 1 % lines.Length);
                }
                if (count != lines.Length) {
                    count++;
                }
            }

            public void UpdateCurrent(string s)
            {
                lines[current] = s;
            }

            public void Accept(string s)
            {
                int t = head - 1;
                if (t < 0) {
                    t = lines.Length - 1;
                }
                lines[t] = s;
            }

            public void Discard()
            {
                head = head - 1;
                if (head < 0) {
                    head = lines.Length - 1;
                }
            }

            public string GetPrevious()
            {
                if (!IsPreviousAvailable) {
                    return null;
                }

                current--;
                if (current < 0) {
                    current = lines.Length - 1;
                }
                return lines[current];
            }

            public string GetNext()
            {
                if (!IsNextAvailable) {
                    return null;
                }

                current = (current + 1) % lines.Length;
                return lines[current];
            }

            public void CursorToEnd()
            {
                if (head == tail) {
                    return;
                }

                current = head;
            }
        }

        #region Native Methods
        private delegate bool ConsoleCtrlDelegate(int type);

        [DllImport("kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);
        #endregion
    }
}