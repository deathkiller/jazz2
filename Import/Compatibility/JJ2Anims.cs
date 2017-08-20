using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Import;

namespace Jazz2.Compatibility
{
    public class JJ2Anims // .j2a
    {
        private class AnimFrameSection
        {
            public Pair<ushort, ushort> Size;
            public Pair<short, short> Coldspot;
            public Pair<short, short> Hotspot;
            public Pair<short, short> Gunspot;
            public byte[] ImageData;
            public BitArray MaskData;
            public int ImageAddr;
            public int MaskAddr;
            public bool DrawTransparent;
        }

        private class AnimSection
        {
            public ushort FrameCount;
            public ushort FrameRate;
            public List<AnimFrameSection> Frames;
            public int Set;
            public ushort Anim;
            public Pair<short, short> AdjustedSize;
            public Pair<short, short> LargestOffset;
            public Pair<short, short> NormalizedHotspot;
            public Pair<short, short> FrameConfiguration;
        }

        private class SampleSection
        {
            public ushort Id;
            public int Set;
            public ushort IdInSet;
            public uint SampleRate;
            public byte[] Data;
            public ushort Multiplier;
        }

        public static void Convert(string path, string targetPath, bool isPlus)
        {
            List<AnimSection> anims = new List<AnimSection>();
            List<SampleSection> samples = new List<SampleSection>();

            using (Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader r = new BinaryReader(s)) {
                bool seemsLikeCC = false;

                uint magicALIB = r.ReadUInt32();
                uint magicBABE = r.ReadUInt32();

                // Magic headers
                if (magicALIB != 0x42494C41 || magicBABE != 0x00BEBA00) {
                    throw new InvalidOperationException("Invalid magic number");
                }

                uint headerLen = r.ReadUInt32();

                uint magicUnknown = r.ReadUInt32();
                if (magicUnknown != 0x18080200) {
                    throw new InvalidOperationException("Invalid magic number");
                }

                uint fileLen = r.ReadUInt32();
                uint crc = r.ReadUInt32();
                int setCnt = r.ReadInt32();
                uint[] setAddresses = new uint[setCnt];

                for (uint i = 0; i < setCnt; i++) {
                    setAddresses[i] = r.ReadUInt32();
                }

                if (headerLen != s.Position) {
                    throw new InvalidOperationException("Header size mismatch");
                }

                bool isStreamComplete = true;

                try {
                    for (int i = 0; i < setCnt; ++i) {
                        uint magicANIM = r.ReadUInt32();
                        byte animCount = r.ReadByte();
                        byte sndCount = r.ReadByte();
                        ushort frameCount = r.ReadUInt16();
                        uint cumulativeSndIndex = r.ReadUInt32();
                        int infoBlockLenC = r.ReadInt32();
                        int infoBlockLenU = r.ReadInt32();
                        int frameDataBlockLenC = r.ReadInt32();
                        int frameDataBlockLenU = r.ReadInt32();
                        int imageDataBlockLenC = r.ReadInt32();
                        int imageDataBlockLenU = r.ReadInt32();
                        int sampleDataBlockLenC = r.ReadInt32();
                        int sampleDataBlockLenU = r.ReadInt32();

                        JJ2Block infoBlock = new JJ2Block(s, infoBlockLenC, infoBlockLenU);
                        JJ2Block frameDataBlock = new JJ2Block(s, frameDataBlockLenC, frameDataBlockLenU);
                        JJ2Block imageDataBlock = new JJ2Block(s, imageDataBlockLenC, imageDataBlockLenU);
                        JJ2Block sampleDataBlock = new JJ2Block(s, sampleDataBlockLenC, sampleDataBlockLenU);

                        if (magicANIM != 0x4D494E41) {
                            Console.WriteLine("Header for set " + i + " is incorrect (bad magic value)! Skipping the subfile.");
                            continue;
                        }

                        List<AnimSection> setAnims = new List<AnimSection>();

                        for (ushort j = 0; j < animCount; ++j) {
                            AnimSection anim = new AnimSection();
                            anim.Set = i;
                            anim.Anim = j;
                            anim.FrameCount = infoBlock.ReadUInt16();
                            anim.FrameRate = infoBlock.ReadUInt16();
                            anim.NormalizedHotspot = Pair.Create((short)0, (short)0);
                            anim.AdjustedSize = Pair.Create((short)0, (short)0);
                            anim.Frames = new List<AnimFrameSection>(anim.FrameCount);

                            // Skip the rest, seems to be 0x00000000 for all headers
                            infoBlock.DiscardBytes(4);

                            anims.Add(anim);
                            setAnims.Add(anim);
                        }

                        if (i == 65 && setAnims.Count > 5) {
                            seemsLikeCC = true;
                        }

                        if (frameCount > 0) {
                            if (setAnims.Count == 0) {
                                throw new InvalidOperationException("Set has frames but no anims");
                            }

                            Pair<short, short> lastColdspot = Pair.Create((short)0, (short)0);
                            Pair<short, short> lastGunspot = Pair.Create((short)0, (short)0);
                            Pair<short, short> lastHotspot = Pair.Create((short)0, (short)0);

                            AnimSection currentAnim = setAnims[0];
                            ushort currentAnimIdx = 0;
                            ushort currentFrame = 0;
                            for (ushort j = 0; j < frameCount; j++) {
                                if (currentFrame >= currentAnim.FrameCount) {
                                    currentAnim = setAnims[++currentAnimIdx];
                                    currentFrame = 0;
                                }

                                AnimFrameSection frame = new AnimFrameSection();
                                frame.Size = Pair.Create(frameDataBlock.ReadUInt16(), frameDataBlock.ReadUInt16());
                                frame.Coldspot = Pair.Create(frameDataBlock.ReadInt16(), frameDataBlock.ReadInt16());
                                frame.Hotspot = Pair.Create(frameDataBlock.ReadInt16(), frameDataBlock.ReadInt16());
                                frame.Gunspot = Pair.Create(frameDataBlock.ReadInt16(), frameDataBlock.ReadInt16());

                                frame.ImageAddr = frameDataBlock.ReadInt32();
                                frame.MaskAddr = frameDataBlock.ReadInt32();

                                // Adjust normalized position
                                // In the output images, we want to make the hotspot and image size constant.
                                currentAnim.NormalizedHotspot = Pair.Create((short)Math.Max(-frame.Hotspot.First, currentAnim.NormalizedHotspot.First), (short)Math.Max(-frame.Hotspot.Second, currentAnim.NormalizedHotspot.Second));
                                currentAnim.LargestOffset = Pair.Create((short)Math.Max(frame.Size.First + frame.Hotspot.First, currentAnim.LargestOffset.First), (short)Math.Max(frame.Size.Second + frame.Hotspot.Second, currentAnim.LargestOffset.Second));
                                currentAnim.AdjustedSize = Pair.Create((short)Math.Max(
                                    currentAnim.NormalizedHotspot.First + currentAnim.LargestOffset.First,
                                    currentAnim.AdjustedSize.First
                                ), (short)Math.Max(
                                    currentAnim.NormalizedHotspot.Second + currentAnim.LargestOffset.Second,
                                    currentAnim.AdjustedSize.Second
                                ));

                                currentAnim.Frames.Add(frame);

#if DEBUG
                                if (currentFrame > 0) {
                                    Pair<short, short> diffOld, diffNew;

                                    if (frame.Coldspot.First != 0 && frame.Coldspot.Second != 0) {
                                        diffNew = Pair.Create((short)(frame.Coldspot.First - frame.Hotspot.First), (short)(frame.Coldspot.Second - frame.Hotspot.Second));
                                        diffOld = Pair.Create((short)(lastColdspot.First - lastHotspot.First), (short)(lastColdspot.Second - lastHotspot.Second));
                                        if (diffNew != diffOld) {
                                            Console.WriteLine("[" + currentAnim.Set + ":" + currentAnim.Anim + "] Animation coldspots don't agree!");
                                            Console.WriteLine("    F" + (currentFrame - 1) + ": " + diffOld.First + "," + diffOld.Second + ", "
                                                + "F" + currentFrame + ": " + diffNew.First + "," + diffNew.Second);
                                        }
                                    }

                                    if (frame.Gunspot.First != 0 && frame.Gunspot.Second != 0) {
                                        diffNew = Pair.Create((short)(frame.Gunspot.First - frame.Hotspot.First), (short)(frame.Gunspot.Second - frame.Hotspot.Second));
                                        diffOld = Pair.Create((short)(lastGunspot.First - lastHotspot.First), (short)(lastGunspot.Second - lastHotspot.Second));
                                        if (diffNew != diffOld) {
                                            Console.WriteLine("[" + currentAnim.Set + ":" + currentAnim.Anim + "] Animation gunspots don't agree!");
                                            Console.WriteLine("    F" + (currentFrame - 1) + ": " + diffOld.First + "," + diffOld.Second + ", "
                                                + "F" + currentFrame + ": " + diffNew.First + "," + diffNew.Second);
                                        }
                                    }
                                }
#endif

                                lastColdspot = frame.Coldspot;
                                lastGunspot = frame.Gunspot;
                                lastHotspot = frame.Hotspot;

                                currentFrame++;
                            }

                            // Read the image data for each animation frame
                            for (ushort j = 0; j < setAnims.Count; j++) {
                                AnimSection anim = setAnims[j];

                                if (anim.FrameCount < anim.Frames.Count) {
                                    Console.WriteLine("[" + i + ":" + j + "] Frame count doesn't match! Expected " + anim.FrameCount + " frames but read " + anim.Frames.Count);
                                    throw new InvalidOperationException();
                                }

                                for (ushort frame = 0; frame < anim.FrameCount; ++frame) {
                                    int dpos = (int)(anim.Frames[frame].ImageAddr + 4);

                                    imageDataBlock.SeekTo(dpos - 4);
                                    ushort width2 = imageDataBlock.ReadUInt16();
                                    imageDataBlock.SeekTo(dpos - 2);
                                    ushort height2 = imageDataBlock.ReadUInt16();

                                    AnimFrameSection frameData = anim.Frames[frame];
                                    frameData.DrawTransparent = (width2 & 0x8000) > 0;

                                    ushort pxRead = 0;
                                    ushort pxTotal = (ushort)(frameData.Size.First * frameData.Size.Second);
                                    bool lastOpEmpty = true;

                                    List<byte> imageData = new List<byte>(pxTotal);

                                    while (pxRead < pxTotal) {
                                        if (dpos > 0x10000000) {
                                            Console.WriteLine("[" + i + ":" + j + "] Loading image data probably failed! Aborting.");
                                            break;
                                        }
                                        imageDataBlock.SeekTo(dpos);
                                        byte op = imageDataBlock.ReadByte();
                                        //if (op == 0) {
                                        //    Console.WriteLine("[" + i + ":" + j + "] Next image operation should probably not be 0x00.");
                                        //}

                                        if (op < 0x80) {
                                            // Skip the given number of pixels, writing them with the transparent color 0
                                            pxRead += op;
                                            while (op-- > 0) {
                                                imageData.Add((byte)0x00);
                                            }
                                            dpos++;
                                        } else if (op == 0x80) {
                                            // Skip until the end of the line
                                            ushort linePxLeft = (ushort)(frameData.Size.First - pxRead % frameData.Size.First);
                                            if (pxRead % anim.Frames[frame].Size.First == 0 && !lastOpEmpty) {
                                                linePxLeft = 0;
                                            }

                                            pxRead += linePxLeft;
                                            while (linePxLeft-- > 0) {
                                                imageData.Add((byte)0x00);
                                            }
                                            dpos++;
                                        } else {
                                            // Copy specified amount of pixels (ignoring the high bit)
                                            ushort bytesToRead = (ushort)(op & 0x7F);
                                            imageDataBlock.SeekTo(dpos + 1);
                                            byte[] nextData = imageDataBlock.ReadRawBytes(bytesToRead);
                                            imageData.AddRange(nextData);
                                            pxRead += bytesToRead;
                                            dpos += bytesToRead + 1;
                                        }

                                        lastOpEmpty = (op == 0x80);
                                    }

                                    frameData.ImageData = imageData.ToArray();

                                    frameData.MaskData = new BitArray(pxTotal, false);
                                    dpos = frameData.MaskAddr;
                                    pxRead = 0;

                                    // No mask
                                    if (dpos == unchecked((int)0xFFFFFFFF)) {
                                        continue;
                                    }

                                    while (pxRead < pxTotal) {
                                        imageDataBlock.SeekTo(dpos);
                                        byte b = imageDataBlock.ReadByte();
                                        for (byte bit = 0; bit < 8 && (pxRead + bit) < pxTotal; ++bit) {
                                            frameData.MaskData[pxRead + bit] = ((b & (1 << (7 - bit))) != 0);
                                        }
                                        pxRead += 8;
                                    }
                                }
                            }
                        }

                        for (ushort j = 0; j < sndCount; ++j) {
                            SampleSection sample = new SampleSection();
                            sample.Id = (ushort)(cumulativeSndIndex + j);
                            sample.IdInSet = j;
                            sample.Set = i;

                            int totalSize = sampleDataBlock.ReadInt32();
                            uint magicRIFF = sampleDataBlock.ReadUInt32();
                            int chunkSize = sampleDataBlock.ReadInt32();
                            // "ASFF" for 1.20, "AS  " for 1.24
                            uint format = sampleDataBlock.ReadUInt32();
                            bool isASFF = (format == 0x46465341);

                            uint magicSAMP = sampleDataBlock.ReadUInt32();
                            uint sampSize = sampleDataBlock.ReadUInt32();
                            // Padding/unknown data #1
                            // For set 0 sample 0:
                            //       1.20                           1.24
                            //  +00  00 00 00 00 00 00 00 00   +00  40 00 00 00 00 00 00 00
                            //  +08  00 00 00 00 00 00 00 00   +08  00 00 00 00 00 00 00 00
                            //  +10  00 00 00 00 00 00 00 00   +10  00 00 00 00 00 00 00 00
                            //  +18  00 00 00 00               +18  00 00 00 00 00 00 00 00
                            //                                 +20  00 00 00 00 00 40 FF 7F
                            sampleDataBlock.DiscardBytes(40 - (isASFF ? 12 : 0));
                            if (isASFF) {
                                // All 1.20 samples seem to be 8-bit. Some of them are among those
                                // for which 1.24 reads as 24-bit but that might just be a mistake.
                                sampleDataBlock.DiscardBytes(2);

                                sample.Multiplier = 0;
                            } else {
                                // for 1.24. 1.20 has "20 40" instead in s0s0 which makes no sense
                                sample.Multiplier = sampleDataBlock.ReadUInt16();
                            }
                            // Unknown. s0s0 1.20: 00 80, 1.24: 80 00
                            sampleDataBlock.DiscardBytes(2);

                            uint payloadSize = sampleDataBlock.ReadUInt32();
                            // Padding #2, all zeroes in both
                            sampleDataBlock.DiscardBytes(8);

                            sample.SampleRate = sampleDataBlock.ReadUInt32();
                            int actualDataSize = chunkSize - 76 + (isASFF ? 12 : 0);

                            sample.Data = sampleDataBlock.ReadRawBytes(actualDataSize);
                            // Padding #3
                            sampleDataBlock.DiscardBytes(4);

                            if (magicRIFF != 0x46464952 || magicSAMP != 0x504D4153) {
                                throw new InvalidOperationException("Sample has invalid header");
                            }

                            if (sample.Data.Length < actualDataSize) {
                                Console.WriteLine("[" + i + ":" + j + "] Sample was shorter than expected! (Expected "
                                    + actualDataSize + " bytes, only read " + sample.Data.Length + ")");
                            }

                            if (totalSize > chunkSize + 12) {
                                // Sample data is probably aligned to X bytes since the next sample doesn't always appear right after the first ends.
                                Console.WriteLine("[" + i + ":" + j + "] Adjusted read offset by " + (totalSize - chunkSize - 12) + " bytes.");

                                sampleDataBlock.DiscardBytes(totalSize - chunkSize - 12);
                            }

                            samples.Add(sample);
                        }
                    }
                } catch (EndOfStreamException) {
                    isStreamComplete = false;
                    Console.WriteLine("Stream should contain " + setCnt + " sets, but no more data found!");
                }

                JJ2Version version;
                if (headerLen == 464) {
                    if (isStreamComplete) {
                        version = JJ2Version.BaseGame;
                        Console.WriteLine("Detected Jazz Jackrabbit 2 (v1.20/1.23)");
                    } else {
                        version = JJ2Version.BaseGame | JJ2Version.SharewareDemo;
                        Console.WriteLine("Detected Jazz Jackrabbit 2 (v1.20/1.23): Shareware Demo");
                    }
                } else if (headerLen == 500 && seemsLikeCC) {
                    version = JJ2Version.CC;
                    Console.WriteLine("Detected Jazz Jackrabbit 2: Christmas Chronicles");
                } else if (headerLen == 500 && !seemsLikeCC) {
                    version = JJ2Version.TSF;
                    Console.WriteLine("Detected Jazz Jackrabbit 2: The Secret Files");
                } else if (headerLen == 476) {
                    version = JJ2Version.HH;
                    Console.WriteLine("Detected Jazz Jackrabbit 2: Holiday Hare '98");
                } else if (headerLen == 64) {
                    version = JJ2Version.PlusExtension;
                    Console.WriteLine("Detected Jazz Jackrabbit 2 Plus extension");
                } else {
                    version = JJ2Version.Unknown;
                    Console.WriteLine("Could not determine the version. Header size: " + headerLen + " bytes");
                }

                AnimSetMapping animMapping = AnimSetMapping.GetAnimMapping(version);
                AnimSetMapping sampleMapping = AnimSetMapping.GetSampleMapping(version);

                if (anims.Count > 0) {
                    Console.WriteLine("Importing animations...");

                    // Process the extracted data next
                    Parallel.ForEach(Partitioner.Create(0, anims.Count), range => {
                        for (int i = range.Item1; i < range.Item2; i++) {
                            AnimSection currentAnim = anims[i];

                            AnimSetMapping.Data data = animMapping.Get(currentAnim.Set, currentAnim.Anim);
                            if (data.Category == AnimSetMapping.Discard) {
                                continue;
                            }

                            data.Palette = data.Palette ?? (data.KeepIndexed ? JJ2DefaultPalette.ByIndex : JJ2DefaultPalette.Sprite);

                            Pair<short, short> size = Pair.Create((short)(currentAnim.AdjustedSize.First + data.AddBorder * 2), (short)(currentAnim.AdjustedSize.Second + data.AddBorder * 2));

                            // Determine the frame configuration to use.
                            // Each asset must fit into a 4096 by 4096 texture,
                            // as that is the smallest texture size we have decided to support.
                            currentAnim.FrameConfiguration = Pair.Create((short)currentAnim.FrameCount, (short)1);
                            if (currentAnim.FrameCount > 1) {
                                short second = (short)Math.Max(1,
                                    (int)Math.Ceiling(Math.Sqrt(currentAnim.FrameCount * size.First /
                                                                size.Second)));
                                short first = (short)Math.Max(1,
                                    (int)Math.Ceiling(currentAnim.FrameCount * 1.0 / second));

                                // Do a bit of optimization, as the above algorithm ends occasionally with some extra space
                                // (it is careful with not underestimating the required space)
                                while (first * (second - 1) >= currentAnim.FrameCount) {
                                    second--;
                                }

                                currentAnim.FrameConfiguration = Pair.Create(first, second);
                            }

                            Bitmap img = new Bitmap(size.First * currentAnim.FrameConfiguration.First,
                                size.Second * currentAnim.FrameConfiguration.Second,
                                PixelFormat.Format32bppArgb);

                            // ToDo: Hardcoded name
                            bool applyToasterPowerUpFix = (data.Category == "Object" && data.Name == "powerup_upgrade_toaster");

                            for (int j = 0; j < currentAnim.Frames.Count; j++) {
                                AnimFrameSection frame = currentAnim.Frames[j];
                                int offsetX = currentAnim.NormalizedHotspot.First + frame.Hotspot.First;
                                int offsetY = currentAnim.NormalizedHotspot.Second + frame.Hotspot.Second;

                                for (ushort y = 0; y < frame.Size.Second; y++) {
                                    for (ushort x = 0; x < frame.Size.First; x++) {
                                        int targetX =
                                            (j % currentAnim.FrameConfiguration.First) * size.First +
                                            offsetX + x + data.AddBorder;
                                        int targetY =
                                            (j / currentAnim.FrameConfiguration.First) * size.Second +
                                            offsetY + y + data.AddBorder;
                                        byte colorIdx = frame.ImageData[frame.Size.First * y + x];

                                        // Apply palette fixes
                                        if (applyToasterPowerUpFix) {
                                            if ((x >= 3 && y >= 4 && x <= 15 && y <= 20) || (x >= 2 && y >= 7 && x <= 15 && y <= 19)) {
                                                colorIdx = JJ2DefaultPalette.ToasterPowerUpFix[colorIdx];
                                            }
                                        }

                                        Color color = data.Palette[colorIdx];

                                        // Apply transparency
                                        if (frame.DrawTransparent) {
                                            color = Color.FromArgb(Math.Min(/*127*/140, (int)color.A), color);
                                        }

                                        img.SetPixel(targetX, targetY, color);
                                    }
                                }
                            }

                            string filename;
                            if (string.IsNullOrEmpty(data.Name)) {
                                filename = "s" + currentAnim.Set + "_a" + currentAnim.Anim + ".png";
                                if (version == JJ2Version.PlusExtension) {
                                    filename = "plus_" + filename;
                                }
                            } else {
                                Directory.CreateDirectory(Path.Combine(targetPath, data.Category));

                                filename = data.Category + "/" + data.Name + ".png";
                            }
                            filename = Path.Combine(targetPath, filename);

                            img.Save(filename, ImageFormat.Png);

                            if (!string.IsNullOrEmpty(data.Name) && !data.SkipNormalMap) {
                                using (Bitmap normalMap = NormalMapGenerator.FromSprite(img,
                                        new Point(currentAnim.FrameConfiguration.First, currentAnim.FrameConfiguration.Second),
                                        !data.KeepIndexed && data.Palette == JJ2DefaultPalette.ByIndex)) {

                                    normalMap.Save(filename.Replace(".png", ".n.png"), ImageFormat.Png);
                                }
                            }

                            using (Stream so = File.Create(filename + ".res"))
                            using (StreamWriter w = new StreamWriter(so, new UTF8Encoding(false))) {
                                w.WriteLine("{");
                                w.WriteLine("    \"Version\": {");
                                w.WriteLine("        \"Target\": \"Jazz² Resurrection\",");
                                w.WriteLine("        \"SourceLocation\": \"" +
                                            currentAnim.Set.ToString(CultureInfo.InvariantCulture) + ":" +
                                            currentAnim.Anim.ToString(CultureInfo.InvariantCulture) + "\",");

                                string sourceVersion;
                                switch (version) {
                                    case JJ2Version.BaseGame:
                                        sourceVersion = "Base (1.20/1.23)";
                                        break;
                                    case JJ2Version.HH:
                                        sourceVersion = "Holiday Hare '98 (1.23)";
                                        break;
                                    case JJ2Version.CC:
                                        sourceVersion = "Christmas Chronicles (1.24)";
                                        break;
                                    case JJ2Version.TSF:
                                        sourceVersion = "The Secret Files (1.24)";
                                        break;
                                    case JJ2Version.PlusExtension:
                                        sourceVersion = "Plus";
                                        break;
                                    default:
                                        sourceVersion = "Unknown";
                                        break;
                                }

                                w.WriteLine("        \"SourceVersion\": \"" + sourceVersion + "\"");
                                w.WriteLine("    },");

                                int flags = 0x00;
                                if (!data.KeepIndexed && data.Palette == JJ2DefaultPalette.ByIndex)
                                    flags |= 0x01;
                                if (!data.KeepIndexed) // Use Linear Sampling if the palette is applied in preprocessing phase
                                    flags |= 0x02;

                                if (flags != 0x00) {
                                    w.WriteLine("    \"Flags\": " + flags + ",");
                                }

                                w.WriteLine("    \"FrameSize\": [ " +
                                            size.First.ToString(CultureInfo.InvariantCulture) + ", " +
                                            size.Second.ToString(CultureInfo.InvariantCulture) + " ],");
                                w.WriteLine("    \"FrameConfiguration\": [ " +
                                            currentAnim.FrameConfiguration.First.ToString(CultureInfo.InvariantCulture) +
                                            ", " +
                                            currentAnim.FrameConfiguration.Second.ToString(CultureInfo.InvariantCulture) +
                                            " ],");
                                w.WriteLine("    \"FrameCount\": " +
                                            currentAnim.FrameCount.ToString(CultureInfo.InvariantCulture) + ",");
                                w.Write("    \"FrameRate\": " + currentAnim.FrameRate.ToString(CultureInfo.InvariantCulture));

                                if (currentAnim.NormalizedHotspot.First != 0 || currentAnim.NormalizedHotspot.Second != 0) {
                                    w.WriteLine(",");
                                    w.Write("    \"Hotspot\": [ " +
                                            (currentAnim.NormalizedHotspot.First + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                                            ", " +
                                            (currentAnim.NormalizedHotspot.Second + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                                            " ]");
                                }

                                if (currentAnim.Frames[0].Coldspot.First != 0 ||
                                    currentAnim.Frames[0].Coldspot.Second != 0) {
                                    w.WriteLine(",");
                                    w.Write("    \"Coldspot\": [ " +
                                            ((currentAnim.NormalizedHotspot.First + currentAnim.Frames[0].Hotspot.First) -
                                             currentAnim.Frames[0].Coldspot.First + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                                            ", " +
                                            ((currentAnim.NormalizedHotspot.Second + currentAnim.Frames[0].Hotspot.Second) -
                                             currentAnim.Frames[0].Coldspot.Second + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                                            " ]");
                                }

                                if (currentAnim.Frames[0].Gunspot.First != 0 || currentAnim.Frames[0].Gunspot.Second != 0) {
                                    w.WriteLine(",");
                                    w.Write("    \"Gunspot\": [ " +
                                            ((currentAnim.NormalizedHotspot.First + currentAnim.Frames[0].Hotspot.First) -
                                             currentAnim.Frames[0].Gunspot.First + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                                            ", " +
                                            ((currentAnim.NormalizedHotspot.Second + currentAnim.Frames[0].Hotspot.Second) -
                                             currentAnim.Frames[0].Gunspot.Second + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                                            " ]");
                                }

                                w.WriteLine();
                                w.Write("}");
                            }
                        }
                    });
                }

                if (samples.Count > 0) {
                    Console.WriteLine("Importing audio samples...");

                    Parallel.ForEach(Partitioner.Create(0, samples.Count), range => {
                        for (int i = range.Item1; i < range.Item2; i++) {
                            SampleSection sample = samples[i];

                            AnimSetMapping.Data data = sampleMapping.Get(sample.Set, sample.IdInSet);
                            if (data.Category == AnimSetMapping.Discard) {
                                continue;
                            }

                            string filename;
                            if (string.IsNullOrEmpty(data.Name)) {
                                filename = "s" + sample.Set + "_s" + sample.IdInSet + ".wav";
                                if (version == JJ2Version.PlusExtension) {
                                    filename = "plus_" + filename;
                                }
                            } else {
                                Directory.CreateDirectory(Path.Combine(targetPath, data.Category));

                                filename = Path.Combine(data.Category, data.Name + ".wav");
                            }
                            filename = Path.Combine(targetPath, filename);

                            using (FileStream so = new FileStream(filename, FileMode.Create, FileAccess.Write))
                            using (BinaryWriter w = new BinaryWriter(so)) {

                                // ToDo: The modulo here essentially clips the sample to 8- or 16-bit.
                                // There are some samples (at least the Rapier random noise) that at least get reported as 24-bit
                                // by the read header data. It is not clear if they actually are or if the header data is just
                                // read incorrectly, though - one would think the data would need to be reshaped between 24 and 8
                                // but it works just fine as is.
                                int multiplier = (sample.Multiplier / 4) % 2 + 1;

                                // Create PCM wave file
                                // Main header
                                w.Write(new[] {(byte)'R', (byte)'I', (byte)'F', (byte)'F'});
                                w.Write((uint)(sample.Data.Length + 36));
                                w.Write(new[] {(byte)'W', (byte)'A', (byte)'V', (byte)'E'});

                                // Format header
                                w.Write(new[] {(byte)'f', (byte)'m', (byte)'t', (byte)' '});
                                w.Write((uint)16); // header remainder length
                                w.Write((ushort)1); // format = PCM
                                w.Write((ushort)1); // channels
                                w.Write((uint)sample.SampleRate); // sample rate
                                w.Write((uint)(sample.SampleRate * multiplier)); // byte rate
                                w.Write((uint)(multiplier * 0x00080001));

                                // Payload
                                w.Write(new[] {(byte)'d', (byte)'a', (byte)'t', (byte)'a'});
                                w.Write((uint)sample.Data.Length); // payload length
                                for (int k = 0; k < sample.Data.Length; k++) {
                                    w.Write((byte)((multiplier << 7) ^ sample.Data[k]));
                                }
                            }
                        }
                    });
                }
            }
        }
    }
}