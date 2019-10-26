using System;
using System.IO;
using System.Reflection;

namespace Duality
{
    public class ConsoleImage
    {
        private struct PaletteEntry
        {
            public char Character;
            public byte Attributes;
        }

        public static bool RenderFromManifestResource(string name, out int imageTop)
        {
            if (ConsoleUtils.IsOutputRedirected) {
                imageTop = -1;
                return false;
            }

            Assembly a = Assembly.GetExecutingAssembly();
            string[] resources = a.GetManifestResourceNames();
            for (int j = 0; j < resources.Length; j++) {
                if (resources[j].EndsWith("." + name, StringComparison.Ordinal)) {
                    using (Stream s = a.GetManifestResourceStream(resources[j])) {
                        return Render(s, out imageTop);
                    }
                }
            }

            imageTop = -1;
            return false;
        }

        public static bool Render(Stream s, out int imageTop)
        {
            if (ConsoleUtils.IsOutputRedirected) {
                imageTop = -1;
                return false;
            }

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
                        imageTop = -1;
                        return false;
                    } else {
                        byte paletteIndex = r.ReadByte();

                        for (int k = 0; k < n; k++) {
                            if (i >= frameLength) {
                                imageTop = -1;
                                return false;
                            }

                            indices[i] = paletteIndex;
                            i++;
                        }
                    }
                }

                int cursorLeft = ((Console.BufferWidth - width) >> 1);
                if (cursorLeft < 0) {
                    // Window is too small to show the image
                    imageTop = -1;
                    return false;
                }

                ConsoleUtils.ScrollBufferIfNeeded(height + 1);

                int cursorTop = Console.CursorTop;

                imageTop = cursorTop;

                try {
                    for (int y = 0; y < height; y++) {
                        Console.CursorLeft = cursorLeft;

                        for (int x = 0; x < width; x++) {
                            ref PaletteEntry entry = ref palette[indices[x + y * width]];
                            if (entry.Attributes == 0 || (entry.Character == ' ' && (entry.Attributes & 0xF0) == 0)) {
                                Console.ResetColor();
                                Console.Write(' ');
                            } else {
                                Console.ForegroundColor = (ConsoleColor)(entry.Attributes & 0x0F);
                                Console.BackgroundColor = (ConsoleColor)((entry.Attributes >> 4) & 0x0F);
                                Console.Write(entry.Character);
                            }
                        }

                        cursorTop++;
                        Console.CursorTop = cursorTop;
                    }

                    cursorTop++;
                    Console.SetCursorPosition(0, cursorTop);

                    Console.ResetColor();
                    return true;
                } catch {
                    try {                    
                        // Something doesn't work, so reset colors and try to erase current line
                        Console.ResetColor();

                        Console.CursorLeft = cursorLeft;

                        for (int x = 0; x < width; x++) {
                            Console.Write(' ');
                        }

                        Console.CursorLeft = 0;
                    } catch {
                        // Do nothing on faulty terminals
                    }

                    imageTop = -1;
                    return false;
                }
            }
        }
    }
}