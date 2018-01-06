using System.Text;

namespace Jazz2.Compatibility
{
    public static class JJ2Text
    {
        public static string ConvertFormattedString(string current, bool keepColors = true)
        {
            StringBuilder sb = new StringBuilder();
            bool randomColor = false;
            int colorIndex = -1;
            bool colorEmitted = true;
            for (int j = 0; j < current.Length; j++) {
                if (current[j] == '"') {
                    sb.Append("\\\"");
                } else if (current[j] == '@') {
                    // New line
                    sb.Append("\\n");
                } else if (current[j] == '§' && j + 1 < current.Length && char.IsDigit(current[j + 1])) {
                    // Char spacing
                    j++;
                    int spacing = current[j] - '0';
                    int converted = 100 - (spacing * 10);

                    sb.Append("\\f[");
                    sb.Append("w:");
                    sb.Append(converted);
                    sb.Append("]");
                } else if (current[j] == '#') {
                    // Random color
                    colorEmitted = false;
                    randomColor ^= true;
                    colorIndex = -1;
                } else if (current[j] == '~') {
                    // Freeze the active color
                    randomColor = false;
                } else if (current[j] == '|') {
                    // Custom color
                    colorIndex++;
                    colorEmitted = false;
                } else {
                    if (keepColors && !colorEmitted) {
                        colorEmitted = true;
                        sb.Append("\\f[");
                        sb.Append("c:");
                        sb.Append(colorIndex);
                        sb.Append("]");
                    }

                    sb.Append(current[j]);

                    if (randomColor && colorIndex > -1) {
                        colorIndex = -1;
                        colorEmitted = false;
                    }
                }
            }

            return sb.ToString();
        }
    }
}