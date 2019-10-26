using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Jazz2.Game;

namespace Duality
{
    /// <summary>
	/// The type of a log message / entry.
	/// </summary>
    public enum LogType
    {
        /// <summary>
        /// Usually a further description of a regular message.
        /// </summary>
        Verbose,
        /// <summary>
		/// Just a regular message. Nothing special. Neutrally informs about what's going on.
		/// </summary>
        Info,
        /// <summary>
		/// A warning message. It informs about unexpected data or behaviour that might not have caused any errors yet, but can lead to them.
		/// It might also be used for expected errors from which Duality is likely to recover.
		/// </summary>
        Warning,
        /// <summary>
		/// An error message. It informs about an unexpected and/or critical error that has occurred.
		/// </summary>
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

        private static StreamWriter logStream;
        private static StringBuilder buffer;
        private static int bufferPosition;
        private static int bufferLastLength;
        private static string activeSuggestion;

        private static History inputHistory;

        static Log()
        {
            bool hasConsoleWindow = true;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                hasConsoleWindow = (GetConsoleWindow() != IntPtr.Zero);
            }

            if (!hasConsoleWindow) {
                if (!Debugger.IsAttached) {
                    try {
                        logStream = new StreamWriter(Path.Combine(App.AssemblyPath, "Jazz2.log"));
                    } catch {
                        // Nothing to do...
                    }
                }
            } else if (!ConsoleUtils.IsOutputRedirected) {
                initialCursorVisible = Console.CursorVisible;
                Console.CursorVisible = false;

                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            }
        }

        public static string FetchLine(Func<string, string> suggestionsCallback)
        {
            if (logStream != null) {
                throw new NotSupportedException("Log is redirected to file");
            }

            if (ConsoleUtils.IsOutputRedirected) {
                return Console.ReadLine();
            }

            activeInput = true;

            RenderInputPrompt();

            Console.CursorVisible = true;
            Console.TreatControlCAsInput = true;

            int consoleWidth;

            buffer = new StringBuilder();
            bufferPosition = 0;
            bufferLastLength = 0;
            activeSuggestion = null;

            if (inputHistory == null) {
                inputHistory = new History();
            }

            inputHistory.CursorToEnd();
            inputHistory.Append("");

            while (activeInput) {
                bool isChanged = false;

                ConsoleModifiers mod;
                ConsoleKeyInfo cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.Escape) {
                    cki = Console.ReadKey(true);
                    mod = ConsoleModifiers.Alt;
                } else {
                    mod = cki.Modifiers;
                }

                // Process input
                switch (cki.Key) {
                    case ConsoleKey.Enter: {
                        if (buffer.Length > 0) {
                            activeInput = false;
                            isChanged = true;
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
                        if (bufferPosition < buffer.Length) {
                            bufferPosition++;
                        } else if (activeSuggestion != null) {
                            buffer.Clear();
                            buffer.Append(activeSuggestion);
                            bufferPosition = buffer.Length;
                            isChanged = true;
                        }
                        break;
                    }
                    case ConsoleKey.UpArrow: {
                        if (inputHistory != null && inputHistory.IsPreviousAvailable) {
                            inputHistory.UpdateCurrent(buffer.ToString());
                            buffer.Clear();
                            buffer.Append(inputHistory.GetPrevious());
                            bufferPosition = buffer.Length;
                            isChanged = true;
                        }
                        break;
                    }
                    case ConsoleKey.DownArrow: {
                        if (inputHistory != null && inputHistory.IsNextAvailable) {
                            inputHistory.UpdateCurrent(buffer.ToString());
                            buffer.Clear();
                            buffer.Append(inputHistory.GetNext());
                            bufferPosition = buffer.Length;
                            isChanged = true;
                        }
                        break;
                    }
                    case ConsoleKey.Home: {
                        bufferPosition = 0;
                        break;
                    }
                    case ConsoleKey.End: {
                        bufferPosition = buffer.Length;
                        break;
                    }
                    case ConsoleKey.Backspace: {
                        if (bufferPosition > 0) {
                            bufferPosition--;
                            buffer.Remove(bufferPosition, 1);
                            isChanged = true;
                        }
                        break;
                    }
                    case ConsoleKey.Delete: {
                        if (bufferPosition < buffer.Length) {
                            buffer.Remove(bufferPosition, 1);
                            isChanged = true;
                        }
                        break;
                    }
                    case ConsoleKey.Tab: {
                        if (activeSuggestion != null) {
                            buffer.Clear();
                            buffer.Append(activeSuggestion);
                            bufferPosition = buffer.Length;
                            isChanged = true;
                        }
                        break;
                    }

                    default: {
                        if (mod == ConsoleModifiers.Control && cki.Key == ConsoleKey.C) {
                            // CTRL+C: Close
                            activeInput = false;
                            buffer = null;
                        } else if (mod == ConsoleModifiers.Control && cki.Key == ConsoleKey.L) {
                            // CTRL+L: Clear screen
                            Console.Clear();
                            Console.SetCursorPosition(0, 0);
                            RenderInputPrompt();
                            isChanged = true;
                        } else if (cki.KeyChar != (char)0 && !char.IsControl(cki.KeyChar)) {
                            buffer.Insert(bufferPosition, cki.KeyChar);
                            bufferPosition++;
                            isChanged = true;
                        }
                        break;
                    }
                }

                // Fetch suggestions
                if (activeInput && isChanged && suggestionsCallback != null) {
                    activeSuggestion = suggestionsCallback(buffer.ToString());
                }

                RenderInput(isChanged);
                isChanged = false;
            }

            Console.TreatControlCAsInput = false;
            Console.CursorVisible = false;

            activeInput = false;

            if (buffer != null) {
                // Set cursor position to the end and create a new line
                consoleWidth = Console.BufferWidth;
                Console.SetCursorPosition((activeInputX + buffer.Length) % consoleWidth, activeInputY + (activeInputX + buffer.Length) / consoleWidth);
                Console.WriteLine();

                string input = buffer.ToString();

                if (string.IsNullOrEmpty(input)) {
                    inputHistory.Discard();
                } else {
                    inputHistory.Accept(input);
                }

                return input;
            } else {
                // Erase input line
                Console.SetCursorPosition(0, activeInputY);
                Console.ResetColor();

                for (int i = 0; i <= activeInputX + bufferLastLength; i++) {
                    Console.Write(' ');
                }

                Console.SetCursorPosition(0, activeInputY);

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

        public static void Write(LogType type, string message)
        {
            if (logStream != null) {
                logStream.WriteLine(message);
                logStream.Flush();
                return;
            }

            if (string.IsNullOrEmpty(message)) {
                lock (lastLogLines) {
                    // End the current line
                    Console.WriteLine();

                    if (activeInput) {
                        RenderInputPrompt();
                        RenderInput(true);
                    }
                }
                return;
            }

            lock (lastLogLines) {
                bool highlight = IsHighlightLine(message);

                // If we're writing the same kind of text again, "grey out" the repeating parts
                int beginGreyLength = 0;
                int endGreyLength = 0;
                if (!highlight) {
                    for (int i = 0; i < lastLogLines.Length; i++) {
                        string lastLogLine = lastLogLines[i] ?? string.Empty;
                        beginGreyLength = Math.Max(beginGreyLength, GetEqualBeginChars(lastLogLine, message));
                        endGreyLength = Math.Max(endGreyLength, GetEqualEndChars(lastLogLine, message));
                    }
                    if (beginGreyLength == message.Length) {
                        endGreyLength = 0;
                    }
                    if (beginGreyLength + endGreyLength >= message.Length) {
                        endGreyLength = 0;
                    }
                }

                if (activeInput) {
                    Console.SetCursorPosition(0, activeInputY);
                }

                // Dot
                SetBrightConsoleColor(type, highlight);
                Console.Write(new string(' ', indent * 2) + (ConsoleUtils.SupportsUnicode ? " · " : " ˙ "));

                // Dark beginning
                if (beginGreyLength != 0) {
                    SetDarkConsoleColor(type);
                    Console.Write(message.Substring(0, beginGreyLength));
                }

                // Bright main part
                SetBrightConsoleColor(type, highlight);
                Console.Write(message.Substring(beginGreyLength, message.Length - beginGreyLength - endGreyLength));

                // Dark ending
                if (endGreyLength != 0) {
                    SetDarkConsoleColor(type);
                    Console.Write(message.Substring(message.Length - endGreyLength, endGreyLength));
                }

                Console.ResetColor();

                // End the current line
                Console.WriteLine();

                lastLogLines[lastLogLineIndex] = message;
                lastLogLineIndex = (lastLogLineIndex + 1) % lastLogLines.Length;

                if (activeInput) {
                    RenderInputPrompt();
                    RenderInput(true);
                }
            }
        }

        public static void Write(LogType type, string message, params object[] messageParams)
        {
            Write(type, messageParams != null && messageParams.Length > 0 ? string.Format(message, messageParams) : message);
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

        private static void RenderInput(bool isChanged)
        {
            lock (lastLogLines) {
                if (isChanged) {
                    // Hide cursor while we're rendering
                    Console.CursorVisible = false;
                    Console.SetCursorPosition(activeInputX, activeInputY);

                    Console.ForegroundColor = ConsoleColor.Gray;
                    string inputToRender = buffer.ToString();
                    Console.Write(inputToRender);
                    int currentLength = buffer.Length;

                    if (activeInput && activeSuggestion != null && activeSuggestion.Length > inputToRender.Length) {
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

                    Console.CursorVisible = true;
                }

                int consoleWidth = Console.BufferWidth;
                Console.SetCursorPosition((activeInputX + bufferPosition) % consoleWidth, activeInputY + (activeInputX + bufferPosition) / consoleWidth);
            }
        }

        private static void RenderInputPrompt()
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

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Console.CursorVisible = initialCursorVisible;
            Console.ResetColor();
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

        [DllImport("kernel32")]
        private static extern IntPtr GetConsoleWindow();
    }
}