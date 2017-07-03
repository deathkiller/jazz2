namespace Jazz2
{
    public static class ExtensionMethods
    {
        public static unsafe string SubstringByOffset(this string input, char delimiter, int offset)
        {
            if (string.IsNullOrEmpty(input)) {
                return null;
            }

            fixed (char* ptr = input) {
                int delimiterCount = 0;
                int start = 0;
                for (int i = 0; i < input.Length; i++) {
                    if (ptr[i] == delimiter) {
                        if (delimiterCount == offset - 1) {
                            start = i + 1;
                        } else if (delimiterCount == offset) {
                            return new string(ptr, start, i - start);
                        }
                        delimiterCount++;
                    }
                }

                if (offset == 0) {
                    return input;
                } else {
                    return (start > 0 ? new string(ptr, start, input.Length - start) : null);
                }
            }
        }
    }
}