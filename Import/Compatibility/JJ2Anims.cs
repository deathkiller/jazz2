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
        private class J2AnimFrame
        {
            public Pair<ushort, ushort> size;
            public Pair<short, short> coldspot;
            public Pair<short, short> hotspot;
            public Pair<short, short> gunspot;
            public byte[] imageData;
            public BitArray maskData;
            public int imageAddr;
            public int maskAddr;
            public bool drawTransparent;
        }

        private class J2Anim
        {
            public ushort frameCnt;
            public ushort fps;
            public List<J2AnimFrame> frames;
            public int set;
            public ushort anim;
            public Pair<short, short> adjustedSize;
            public Pair<short, short> largestOffset;
            public Pair<short, short> normalizedHotspot;
            public Pair<short, short> frameConfiguration;
        }

        private class J2Sample
        {
            public ushort id;
            public int set;
            public ushort idInSet;
            public uint sampleRate;
            public byte[] soundData;
            public ushort multiplier;
        }

        public static void Convert(string path, string targetPath, bool isPlus)
        {
            List<J2Anim> anims = new List<J2Anim>();
            List<J2Sample> samples = new List<J2Sample>();

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

                    List<J2Anim> setAnims = new List<J2Anim>();

                    for (ushort j = 0; j < animCount; ++j) {
                        J2Anim anim = new J2Anim();
                        anim.set = i;
                        anim.anim = j;
                        anim.frameCnt = infoBlock.ReadUInt16();
                        anim.fps = infoBlock.ReadUInt16();
                        anim.normalizedHotspot = Pair.Create((short)0, (short)0);
                        anim.adjustedSize = Pair.Create((short)0, (short)0);
                        anim.frames = new List<J2AnimFrame>();

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

                        J2Anim currentAnim = setAnims[0];
                        ushort currentAnimIdx = 0;
                        ushort currentFrame = 0;
                        for (ushort j = 0; j < frameCount; j++) {
                            if (currentFrame >= currentAnim.frameCnt) {
                                currentAnim = setAnims[++currentAnimIdx];
                                currentFrame = 0;
                            }

                            J2AnimFrame frame = new J2AnimFrame();
                            frame.size = Pair.Create(frameDataBlock.ReadUInt16(), frameDataBlock.ReadUInt16());
                            frame.coldspot = Pair.Create(frameDataBlock.ReadInt16(), frameDataBlock.ReadInt16());
                            frame.hotspot = Pair.Create(frameDataBlock.ReadInt16(), frameDataBlock.ReadInt16());
                            frame.gunspot = Pair.Create(frameDataBlock.ReadInt16(), frameDataBlock.ReadInt16());

                            frame.imageAddr = frameDataBlock.ReadInt32();
                            frame.maskAddr = frameDataBlock.ReadInt32();

                            // Adjust normalized position
                            // In the output images, we want to make the hotspot and image size constant.
                            currentAnim.normalizedHotspot = Pair.Create((short)Math.Max(-frame.hotspot.First, currentAnim.normalizedHotspot.First), (short)Math.Max(-frame.hotspot.Second, currentAnim.normalizedHotspot.Second));
                            currentAnim.largestOffset = Pair.Create((short)Math.Max(frame.size.First + frame.hotspot.First, currentAnim.largestOffset.First), (short)Math.Max(frame.size.Second + frame.hotspot.Second, currentAnim.largestOffset.Second));
                            currentAnim.adjustedSize = Pair.Create((short)Math.Max(
                                currentAnim.normalizedHotspot.First + currentAnim.largestOffset.First,
                                currentAnim.adjustedSize.First
                            ), (short)Math.Max(
                                currentAnim.normalizedHotspot.Second + currentAnim.largestOffset.Second,
                                currentAnim.adjustedSize.Second
                            ));

                            currentAnim.frames.Add(frame);

#if DEBUG
                            if (currentFrame > 0) {
                                Pair<short, short> diffOld, diffNew;

                                if (frame.coldspot.First != 0 && frame.coldspot.Second != 0) {
                                    diffNew = Pair.Create((short)(frame.coldspot.First - frame.hotspot.First), (short)(frame.coldspot.Second - frame.hotspot.Second));
                                    diffOld = Pair.Create((short)(lastColdspot.First - lastHotspot.First), (short)(lastColdspot.Second - lastHotspot.Second));
                                    if (diffNew != diffOld) {
                                        Console.WriteLine("[" + currentAnim.set + ":" + currentAnim.anim + "] Animation coldspots don't agree!");
                                        Console.WriteLine("    F" + (currentFrame - 1) + ": " + diffOld.First + "," + diffOld.Second + ", "
                                            + "F" + currentFrame + ": " + diffNew.First + "," + diffNew.Second);
                                    }
                                }

                                if (frame.gunspot.First != 0 && frame.gunspot.Second != 0) {
                                    diffNew = Pair.Create((short)(frame.gunspot.First - frame.hotspot.First), (short)(frame.gunspot.Second - frame.hotspot.Second));
                                    diffOld = Pair.Create((short)(lastGunspot.First - lastHotspot.First), (short)(lastGunspot.Second - lastHotspot.Second));
                                    if (diffNew != diffOld) {
                                        Console.WriteLine("[" + currentAnim.set + ":" + currentAnim.anim + "] Animation gunspots don't agree!");
                                        Console.WriteLine("    F" + (currentFrame - 1) + ": " + diffOld.First + "," + diffOld.Second + ", "
                                            + "F" + currentFrame + ": " + diffNew.First + "," + diffNew.Second);
                                    }
                                }
                            }
#endif

                            lastColdspot = frame.coldspot;
                            lastGunspot = frame.gunspot;
                            lastHotspot = frame.hotspot;

                            currentFrame++;
                        }

                        // Read the image data for each animation frame
                        for (ushort j = 0; j < setAnims.Count; j++) {
                            J2Anim anim = setAnims[j];

                            if (anim.frameCnt < anim.frames.Count) {
                                Console.WriteLine("[" + i + ":" + j + "] Frame count doesn't match! Expected " + anim.frameCnt + " frames but read " + anim.frames.Count);
                                throw new InvalidOperationException();
                            }

                            for (ushort frame = 0; frame < anim.frameCnt; ++frame) {
                                int dpos = (int)(anim.frames[frame].imageAddr + 4);

                                imageDataBlock.SeekTo(dpos - 4);
                                ushort width2 = imageDataBlock.ReadUInt16();
                                imageDataBlock.SeekTo(dpos - 2);
                                ushort height2 = imageDataBlock.ReadUInt16();

                                J2AnimFrame frameData = anim.frames[frame];
                                frameData.drawTransparent = (width2 & 0x8000) > 0;

                                ushort pxRead = 0;
                                ushort pxTotal = (ushort)(frameData.size.First * frameData.size.Second);
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
                                        ushort linePxLeft = (ushort)(frameData.size.First - pxRead % frameData.size.First);
                                        if (pxRead % anim.frames[frame].size.First == 0 && !lastOpEmpty) {
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

                                frameData.imageData = imageData.ToArray();

                                frameData.maskData = new BitArray(pxTotal, false);
                                dpos = frameData.maskAddr;
                                pxRead = 0;

                                // No mask
                                if (dpos == unchecked((int)0xFFFFFFFF)) {
                                    continue;
                                }

                                while (pxRead < pxTotal) {
                                    imageDataBlock.SeekTo(dpos);
                                    byte b = imageDataBlock.ReadByte();
                                    for (byte bit = 0; bit < 8 && (pxRead + bit) < pxTotal; ++bit) {
                                        frameData.maskData[pxRead + bit] = ((b & (1 << (7 - bit))) != 0);
                                    }
                                    pxRead += 8;
                                }
                            }
                        }
                    }

                    for (ushort j = 0; j < sndCount; ++j) {
                        J2Sample sample = new J2Sample();
                        sample.id = (ushort)(cumulativeSndIndex + j);
                        sample.idInSet = j;
                        sample.set = i;

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

                            sample.multiplier = 0;
                        } else {
                            // for 1.24. 1.20 has "20 40" instead in s0s0 which makes no sense
                            sample.multiplier = sampleDataBlock.ReadUInt16();
                        }
                        // Unknown. s0s0 1.20: 00 80, 1.24: 80 00
                        sampleDataBlock.DiscardBytes(2);

                        uint payloadSize = sampleDataBlock.ReadUInt32();
                        // Padding #2, all zeroes in both
                        sampleDataBlock.DiscardBytes(8);

                        sample.sampleRate = sampleDataBlock.ReadUInt32();
                        int actualDataSize = chunkSize - 76 + (isASFF ? 12 : 0);

                        sample.soundData = sampleDataBlock.ReadRawBytes(actualDataSize);
                        // Padding #3
                        sampleDataBlock.DiscardBytes(4);

                        if (magicRIFF != 0x46464952 || magicSAMP != 0x504D4153) {
                            throw new InvalidOperationException("Sample has invalid header");
                        }

                        if (sample.soundData.Length < actualDataSize) {
                            Console.WriteLine("[" + i + ":" + j + "] Sample was shorter than expected! (Expected "
                                + actualDataSize + " bytes, only read " + sample.soundData.Length + ")");
                        }

                        if (totalSize > chunkSize + 12) {
                            // Sample data is probably aligned to X bytes since the next sample doesn't always appear right after the first ends.
                            Console.WriteLine("[" + i + ":" + j + "] Adjusted read offset by " + (totalSize - chunkSize - 12) + " bytes.");

                            sampleDataBlock.DiscardBytes(totalSize - chunkSize - 12);
                        }

                        samples.Add(sample);
                    }
                }

                JJ2Version version;
                if (headerLen == 464) {
                    version = JJ2Version.BaseGame;
                    Console.WriteLine("Detected Jazz Jackrabbit 2 version 1.20/1.23.");
                } else if (headerLen == 500 && seemsLikeCC) {
                    version = JJ2Version.CC;
                    Console.WriteLine("Detected Jazz Jackrabbit 2: Christmas Chronicles.");
                } else if (headerLen == 500 && !seemsLikeCC) {
                    version = JJ2Version.TSF;
                    Console.WriteLine("Detected Jazz Jackrabbit 2: The Secret Files.");
                } else if (headerLen == 476) {
                    version = JJ2Version.HH;
                    Console.WriteLine("Detected Jazz Jackrabbit 2: Holiday Hare '98.");
                } else if (headerLen == 64) {
                    version = JJ2Version.PlusExtension;
                    Console.WriteLine("Detected Jazz Jackrabbit 2 Plus extension.");
                } else {
                    version = JJ2Version.Unknown;
                    Console.WriteLine("Could not determine the version. Header size: " + headerLen + " bytes");
                }

                AnimSetMapping animMapping = AnimSetMapping.GetAnimMapping(version);
                AnimSetMapping sampleMapping = AnimSetMapping.GetSampleMapping(version);

                if (anims.Count > 0) {
                    Console.WriteLine("Exporting animations...");

                    // Process the extracted data next
                    Parallel.ForEach(Partitioner.Create(0, anims.Count), range => {
                        for (int i = range.Item1; i < range.Item2; i++) {
                            J2Anim currentAnim = anims[i];

                            AnimSetMapping.Data data = animMapping.Get(currentAnim.set, currentAnim.anim);
                            if (data.Category == AnimSetMapping.Discard) {
                                continue;
                            }

                            data.Palette = data.Palette ?? JJ2DefaultPalette.Sprite;

                            // Determine the frame configuration to use.
                            // Each asset must fit into a 4096 by 4096 texture,
                            // as that is the smallest texture size we have decided to support.
                            currentAnim.frameConfiguration = Pair.Create((short)currentAnim.frameCnt, (short)1);
                            if (currentAnim.frameCnt > 1) {
                                short second = (short)Math.Max(1,
                                    (int)Math.Ceiling(Math.Sqrt(currentAnim.frameCnt * currentAnim.adjustedSize.First /
                                                                currentAnim.adjustedSize.Second)));
                                short first = (short)Math.Max(1,
                                    (int)Math.Ceiling(currentAnim.frameCnt * 1.0 / second));

                                // Do a bit of optimization, as the above algorithm ends occasionally with some extra space
                                // (it is careful with not underestimating the required space)
                                while (first * (second - 1) >= currentAnim.frameCnt) {
                                    second--;
                                }

                                currentAnim.frameConfiguration = Pair.Create(first, second);
                            }

                            Bitmap img = new Bitmap(currentAnim.adjustedSize.First * currentAnim.frameConfiguration.First,
                                currentAnim.adjustedSize.Second * currentAnim.frameConfiguration.Second,
                                PixelFormat.Format32bppArgb);

                            for (int j = 0; j < currentAnim.frames.Count; j++) {
                                J2AnimFrame frame = currentAnim.frames[j];
                                int offsetX = currentAnim.normalizedHotspot.First + frame.hotspot.First;
                                int offsetY = currentAnim.normalizedHotspot.Second + frame.hotspot.Second;

                                for (ushort y = 0; y < frame.size.Second; ++y) {
                                    for (ushort x = 0; x < frame.size.First; ++x) {
                                        int targetX =
                                            (j % currentAnim.frameConfiguration.First) * currentAnim.adjustedSize.First +
                                            offsetX + x;
                                        int targetY =
                                            (j / currentAnim.frameConfiguration.First) * currentAnim.adjustedSize.Second +
                                            offsetY + y;
                                        byte colorIdx = frame.imageData[frame.size.First * y + x];

                                        Color color = data.Palette[colorIdx];
                                        if (frame.drawTransparent) {
                                            color = Color.FromArgb(Math.Min(127, (int)color.A), color);
                                        }

                                        img.SetPixel(targetX, targetY, color);
                                    }
                                }
                            }

                            string filename;
                            if (string.IsNullOrEmpty(data.Name)) {
                                filename = "s" + currentAnim.set + "_a" + currentAnim.anim + ".png";
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
                                        new Point(currentAnim.frameConfiguration.First, currentAnim.frameConfiguration.Second),
                                        data.Palette == JJ2DefaultPalette.ByIndex)) {

                                    normalMap.Save(filename.Replace(".png", ".n.png"), ImageFormat.Png);
                                }
                            }

                            using (Stream so = File.Create(filename + ".res"))
                            using (StreamWriter w = new StreamWriter(so, new UTF8Encoding(false))) {
                                w.WriteLine("{");
                                w.WriteLine("    \"Version\": {");
                                w.WriteLine("        \"Target\": \"Jazz² Resurrection\",");
                                w.WriteLine("        \"SourceLocation\": \"" +
                                            currentAnim.set.ToString(CultureInfo.InvariantCulture) + ":" +
                                            currentAnim.anim.ToString(CultureInfo.InvariantCulture) + "\",");

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

                                int flags = 0;
                                if (data.Palette == JJ2DefaultPalette.ByIndex)
                                    flags |= 1;
                                if (flags != 0) {
                                    w.WriteLine("    \"Flags\": " + flags + ",");
                                }

                                w.WriteLine("    \"FrameSize\": [ " +
                                            currentAnim.adjustedSize.First.ToString(CultureInfo.InvariantCulture) + ", " +
                                            currentAnim.adjustedSize.Second.ToString(CultureInfo.InvariantCulture) + " ],");
                                w.WriteLine("    \"FrameConfiguration\": [ " +
                                            currentAnim.frameConfiguration.First.ToString(CultureInfo.InvariantCulture) +
                                            ", " +
                                            currentAnim.frameConfiguration.Second.ToString(CultureInfo.InvariantCulture) +
                                            " ],");
                                w.WriteLine("    \"FrameCount\": " +
                                            currentAnim.frameCnt.ToString(CultureInfo.InvariantCulture) + ",");
                                w.Write("    \"FrameRate\": " + currentAnim.fps.ToString(CultureInfo.InvariantCulture));

                                if (currentAnim.normalizedHotspot.First != 0 || currentAnim.normalizedHotspot.Second != 0) {
                                    w.WriteLine(",");
                                    w.Write("    \"Hotspot\": [ " +
                                            currentAnim.normalizedHotspot.First.ToString(CultureInfo.InvariantCulture) +
                                            ", " +
                                            currentAnim.normalizedHotspot.Second.ToString(CultureInfo.InvariantCulture) +
                                            " ]");
                                }

                                if (currentAnim.frames[0].coldspot.First != 0 ||
                                    currentAnim.frames[0].coldspot.Second != 0) {
                                    w.WriteLine(",");
                                    w.Write("    \"Coldspot\": [ " +
                                            ((currentAnim.normalizedHotspot.First + currentAnim.frames[0].hotspot.First) -
                                             currentAnim.frames[0].coldspot.First).ToString(CultureInfo.InvariantCulture) +
                                            ", " +
                                            ((currentAnim.normalizedHotspot.Second + currentAnim.frames[0].hotspot.Second) -
                                             currentAnim.frames[0].coldspot.Second).ToString(CultureInfo.InvariantCulture) +
                                            " ]");
                                }

                                if (currentAnim.frames[0].gunspot.First != 0 || currentAnim.frames[0].gunspot.Second != 0) {
                                    w.WriteLine(",");
                                    w.Write("    \"Gunspot\": [ " +
                                            ((currentAnim.normalizedHotspot.First + currentAnim.frames[0].hotspot.First) -
                                             currentAnim.frames[0].gunspot.First).ToString(CultureInfo.InvariantCulture) +
                                            ", " +
                                            ((currentAnim.normalizedHotspot.Second + currentAnim.frames[0].hotspot.Second) -
                                             currentAnim.frames[0].gunspot.Second).ToString(CultureInfo.InvariantCulture) +
                                            " ]");
                                }

                                w.WriteLine();
                                w.Write("}");
                            }
                        }
                    });
                }

                if (samples.Count > 0) {
                    Console.WriteLine("Exporting audio samples...");

                    Parallel.ForEach(Partitioner.Create(0, samples.Count), range => {
                        for (int i = range.Item1; i < range.Item2; i++) {
                            J2Sample sample = samples[i];

                            AnimSetMapping.Data data = sampleMapping.Get(sample.set, sample.idInSet);
                            if (data.Category == AnimSetMapping.Discard) {
                                continue;
                            }

                            string filename;
                            if (string.IsNullOrEmpty(data.Name)) {
                                filename = "s" + sample.set + "_s" + sample.idInSet + ".wav";
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
                                int multiplier = (sample.multiplier / 4) % 2 + 1;

                                // Create PCM wave file
                                // Main header
                                w.Write(new[] {(byte)'R', (byte)'I', (byte)'F', (byte)'F'});
                                w.Write((uint)(sample.soundData.Length + 36));
                                w.Write(new[] {(byte)'W', (byte)'A', (byte)'V', (byte)'E'});

                                // Format header
                                w.Write(new[] {(byte)'f', (byte)'m', (byte)'t', (byte)' '});
                                w.Write((uint)16); // header remainder length
                                w.Write((ushort)1); // format = PCM
                                w.Write((ushort)1); // channels
                                w.Write((uint)sample.sampleRate); // sample rate
                                w.Write((uint)(sample.sampleRate * multiplier)); // byte rate
                                w.Write((uint)(multiplier * 0x00080001));

                                // Payload
                                w.Write(new[] {(byte)'d', (byte)'a', (byte)'t', (byte)'a'});
                                w.Write((uint)sample.soundData.Length); // payload length
                                for (int k = 0; k < sample.soundData.Length; k++) {
                                    w.Write((byte)((multiplier << 7) ^ sample.soundData[k]));
                                }
                            }
                        }
                    });
                }
            }
        }
    }
}