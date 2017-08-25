using System;
using System.IO;
using System.Reflection;

namespace Import
{
    public class ConsoleImage
    {
        private struct PaletteEntry
        {
            public char Character;
            public byte Attributes;
        }

        public static void RenderFromManifestResource(string name)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            string[] resources = a.GetManifestResourceNames();
            for (int j = 0; j < resources.Length; j++) {
                if (resources[j].EndsWith("." + name, StringComparison.Ordinal)) {
                    using (Stream s = a.GetManifestResourceStream(resources[j])) {
                        Render(s);
                    }
                }
            }
        }

        public static void Render(Stream s)
        {
            using (BinaryReader r = new BinaryReader(s)) {
                byte flags = r.ReadByte();

                byte width = r.ReadByte();
                byte height = r.ReadByte();

                byte paletteCount = r.ReadByte();
                PaletteEntry[] palette = new PaletteEntry[paletteCount];
                for (int k = 0; k < paletteCount; k++) {
                    char character = r.ReadChar();
                    byte attributes = r.ReadByte();
                    palette[k] = new PaletteEntry { Character = character, Attributes = attributes };
                }

                byte frameCount = r.ReadByte();
                ushort frameLength = (ushort)(width * height);

                // Load image into memory buffer
                int[] indices = new int[frameLength];
                int i = 0;
                while (r.BaseStream.Position < r.BaseStream.Length) {
                    byte n = r.ReadByte();
                    if (n == 0) { // New frame - not supported
                        return;
                    } else {
                        byte paletteIndex = r.ReadByte();

                        for (int k = 0; k < n; k++) {
                            if (i >= frameLength) {
                                return;
                            }

                            indices[i] = paletteIndex;
                            i++;
                        }
                    }
                }

                // Render to console (multi-platform)
                ConsoleColor originalForeground = Console.ForegroundColor;
                ConsoleColor originalBackground = Console.BackgroundColor;

                int cursorLeft = ((Console.BufferWidth - width) >> 1);
                for (int y = 0; y < height; y++) {
                    Console.CursorLeft = cursorLeft;

                    for (int x = 0; x < width; x++) {
                        ref PaletteEntry entry = ref palette[indices[x + y * width]];
                        Console.ForegroundColor = (ConsoleColor)(entry.Attributes & 0x0F);
                        Console.BackgroundColor = (ConsoleColor)((entry.Attributes >> 4) & 0x0F);
                        Console.Write(entry.Character);
                    }

                    Console.CursorTop++;
                }

                Console.SetCursorPosition(0, Console.CursorTop + 1);

                Console.ForegroundColor = originalForeground;
                Console.BackgroundColor = originalBackground;
                Console.ResetColor();
            }
        }
    }
}