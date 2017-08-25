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
using Duality;
using Import;

namespace Jazz2.Compatibility
{
    public class JJ2Anims // .j2a
    {
        // Can't be struct, because the instances are shared across two lists
        private class AnimSection
        {
            public ushort FrameCount;
            public ushort FrameRate;
            public AnimFrameSection[] Frames;
            public int Set;
            public ushort Anim;

            public short AdjustedSizeX, AdjustedSizeY;
            public short LargestOffsetX, LargestOffsetY;
            public short NormalizedHotspotX, NormalizedHotspotY;
            public short FrameConfigurationX, FrameConfigurationY;
        }

        private struct AnimFrameSection
        {
            public short SizeX, SizeY;
            public short ColdspotX, ColdspotY;
            public short HotspotX, HotspotY;
            public short GunspotX, GunspotY;

            public byte[] ImageData;
            public BitArray MaskData;
            public int ImageAddr;
            public int MaskAddr;
            public bool DrawTransparent;
        }

        private struct SampleSection
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
            JJ2Version version;
            RawList<AnimSection> anims = new RawList<AnimSection>();
            RawList<SampleSection> samples = new RawList<SampleSection>();

            using (Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader r = new BinaryReader(s)) {
                Log.Write(LogType.Info, "Reading compressed stream...");
                Log.PushIndent();

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

                // Read content
                bool isStreamComplete = true;
                {
                    int i = 0;
                    try {
                        for (; i < setCnt; i++) {
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
                                Log.Write(LogType.Warning, "Header for set " + i + " is incorrect (bad magic value)! Skipping the subfile.");
                                continue;
                            }

                            List<AnimSection> setAnims = new List<AnimSection>();

                            for (ushort j = 0; j < animCount; j++) {
                                AnimSection anim = new AnimSection();
                                anim.Set = i;
                                anim.Anim = j;
                                anim.FrameCount = infoBlock.ReadUInt16();
                                anim.FrameRate = infoBlock.ReadUInt16();
                                anim.Frames = new AnimFrameSection[anim.FrameCount];

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

                                short lastColdspotX = 0, lastColdspotY = 0;
                                short lastHotspotX = 0, lastHotspotY = 0;
                                short lastGunspotX = 0, lastGunspotY = 0;

                                AnimSection currentAnim = setAnims[0];
                                ushort currentAnimIdx = 0, currentFrame = 0;

                                for (ushort j = 0; j < frameCount; j++) {
                                    if (currentFrame >= currentAnim.FrameCount) {
                                        // Jump to the next animation
                                        currentAnim = setAnims[++currentAnimIdx];
                                        currentFrame = 0;
                                    }

                                    ref AnimFrameSection frame = ref currentAnim.Frames[currentFrame];

                                    frame.SizeX = frameDataBlock.ReadInt16();
                                    frame.SizeY = frameDataBlock.ReadInt16();
                                    frame.ColdspotX = frameDataBlock.ReadInt16();
                                    frame.ColdspotY = frameDataBlock.ReadInt16();
                                    frame.HotspotX = frameDataBlock.ReadInt16();
                                    frame.HotspotY = frameDataBlock.ReadInt16();
                                    frame.GunspotX = frameDataBlock.ReadInt16();
                                    frame.GunspotY = frameDataBlock.ReadInt16();

                                    frame.ImageAddr = frameDataBlock.ReadInt32();
                                    frame.MaskAddr = frameDataBlock.ReadInt32();

                                    // Adjust normalized position
                                    // In the output images, we want to make the hotspot and image size constant.
                                    currentAnim.NormalizedHotspotX = (short)Math.Max(-frame.HotspotX, currentAnim.NormalizedHotspotX);
                                    currentAnim.NormalizedHotspotY = (short)Math.Max(-frame.HotspotY, currentAnim.NormalizedHotspotY);

                                    currentAnim.LargestOffsetX = (short)Math.Max(frame.SizeX + frame.HotspotX, currentAnim.LargestOffsetX);
                                    currentAnim.LargestOffsetY = (short)Math.Max(frame.SizeY + frame.HotspotY, currentAnim.LargestOffsetY);

                                    currentAnim.AdjustedSizeX = (short)Math.Max(
                                        currentAnim.NormalizedHotspotX + currentAnim.LargestOffsetX,
                                        currentAnim.AdjustedSizeX
                                    );
                                    currentAnim.AdjustedSizeY = (short)Math.Max(
                                        currentAnim.NormalizedHotspotY + currentAnim.LargestOffsetY,
                                        currentAnim.AdjustedSizeY
                                    );

#if DEBUG
                                    if (currentFrame > 0) {
                                        int diffPrevX, diffPrevY, diffNextX, diffNextY;

                                        if (frame.ColdspotX != 0 && frame.ColdspotY != 0) {
                                            diffPrevX = (lastColdspotX - lastHotspotX);
                                            diffPrevY = (lastColdspotY - lastHotspotY);
                                            diffNextX = (frame.ColdspotX - frame.HotspotX);
                                            diffNextY = (frame.ColdspotY - frame.HotspotY);

                                            if (diffPrevX != diffNextX || diffPrevY != diffNextY) {
                                                Log.Write(LogType.Warning, "Animation " + currentAnim.Anim + " coldspots in set " + currentAnim.Set + " are different!");
                                                Log.PushIndent();
                                                Log.Write(LogType.Warning, "Frame #" + (currentFrame - 1) + ": " + diffPrevX + "," + diffPrevY + "  |  " + "Frame #" + currentFrame + ": " + diffNextX + "," + diffNextY);
                                                Log.PopIndent();
                                            }
                                        }

                                        if (frame.GunspotX != 0 && frame.GunspotY != 0) {
                                            diffPrevX = (lastGunspotX - lastHotspotX);
                                            diffPrevY = (lastGunspotY - lastHotspotY);
                                            diffNextX = (frame.GunspotX - frame.HotspotX);
                                            diffNextY = (frame.GunspotY - frame.HotspotY);

                                            if (diffPrevX != diffNextX || diffPrevY != diffNextY) {
                                                Log.Write(LogType.Warning, "Animation " + currentAnim.Anim + " gunspots in set " + currentAnim.Set + " are different!");
                                                Log.PushIndent();
                                                Log.Write(LogType.Warning, "Frame #" + (currentFrame - 1) + ": " + diffPrevX + "," + diffPrevY + "  |  " + "Frame #" + currentFrame + ": " + diffNextX + "," + diffNextY);
                                                Log.PopIndent();
                                            }
                                        }
                                    }
#endif

                                    lastColdspotX = frame.ColdspotX; lastColdspotY = frame.ColdspotY;
                                    lastHotspotX = frame.HotspotX; lastHotspotY = frame.HotspotY;
                                    lastGunspotX = frame.GunspotX; lastGunspotY = frame.GunspotY;

                                    currentFrame++;
                                }

                                // Read the image data for each animation frame
                                for (ushort j = 0; j < setAnims.Count; j++) {
                                    AnimSection anim = setAnims[j];

                                    if (anim.FrameCount < anim.Frames.Length) {
                                        Log.Write(LogType.Error, "Animation " + j + " frame count in set " + i + " doesn't match! Expected "
                                            + anim.FrameCount + " frames, but read " + anim.Frames.Length + " instead.");

                                        throw new InvalidOperationException();
                                    }

                                    for (ushort frame = 0; frame < anim.FrameCount; ++frame) {
                                        int dpos = (anim.Frames[frame].ImageAddr + 4);

                                        imageDataBlock.SeekTo(dpos - 4);
                                        ushort width2 = imageDataBlock.ReadUInt16();
                                        imageDataBlock.SeekTo(dpos - 2);
                                        ushort height2 = imageDataBlock.ReadUInt16();

                                        ref AnimFrameSection frameData = ref anim.Frames[frame];
                                        frameData.DrawTransparent = (width2 & 0x8000) > 0;

                                        int pxRead = 0;
                                        int pxTotal = (frameData.SizeX * frameData.SizeY);
                                        bool lastOpEmpty = true;

                                        List<byte> imageData = new List<byte>(pxTotal);

                                        while (pxRead < pxTotal) {
                                            if (dpos > 0x10000000) {
                                                Log.Write(LogType.Error, "Loading of animation " + j + " in set " + i + " failed! Aborting.");
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
                                                ushort linePxLeft = (ushort)(frameData.SizeX - pxRead % frameData.SizeX);
                                                if (pxRead % anim.Frames[frame].SizeX == 0 && !lastOpEmpty) {
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
                                SampleSection sample;
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
                                    Log.Write(LogType.Warning, "Sample " + j + " in set " + i + " was shorter than expected! Expected "
                                        + actualDataSize + " bytes, but read " + sample.Data.Length + " instead.");
                                }

                                if (totalSize > chunkSize + 12) {
                                    // Sample data is probably aligned to X bytes since the next sample doesn't always appear right after the first ends.
                                    Log.Write(LogType.Warning, "Adjusting read offset of sample " + j + " in set " + i + " by " + (totalSize - chunkSize - 12) + " bytes.");

                                    sampleDataBlock.DiscardBytes(totalSize - chunkSize - 12);
                                }

                                samples.Add(sample);
                            }
                        }
                    } catch (EndOfStreamException) {
                        isStreamComplete = false;
                        Log.Write(LogType.Warning, "Stream should contain " + setCnt + " sets, but found " + i + " sets instead!");
                    }
                }

                Log.PopIndent();

                // Detect version to import
                if (headerLen == 464) {
                    if (isStreamComplete) {
                        version = JJ2Version.BaseGame;
                        Log.Write(LogType.Info, "Detected Jazz Jackrabbit 2 (v1.20/1.23).");
                    } else {
                        version = JJ2Version.BaseGame | JJ2Version.SharewareDemo;
                        Log.Write(LogType.Info, "Detected Jazz Jackrabbit 2 (v1.20/1.23): Shareware Demo.");
                    }
                } else if (headerLen == 500 && seemsLikeCC) {
                    version = JJ2Version.CC;
                    Log.Write(LogType.Info, "Detected Jazz Jackrabbit 2: Christmas Chronicles.");
                } else if (headerLen == 500 && !seemsLikeCC) {
                    version = JJ2Version.TSF;
                    Log.Write(LogType.Info, "Detected Jazz Jackrabbit 2: The Secret Files.");
                } else if (headerLen == 476) {
                    version = JJ2Version.HH;
                    Log.Write(LogType.Info, "Detected Jazz Jackrabbit 2: Holiday Hare '98.");
                } else if (headerLen == 64) {
                    version = JJ2Version.PlusExtension;
                    Log.Write(LogType.Info, "Detected Jazz Jackrabbit 2 Plus extension.");
                } else {
                    version = JJ2Version.Unknown;
                    Log.Write(LogType.Warning, "Could not determine the version. Header size: " + headerLen + " bytes");
                }
            }

            ImportAnimations(targetPath, version, anims);
            ImportAudioSamples(targetPath, version, samples);
        }

        private static void ImportAnimations(string targetPath, JJ2Version version, RawList<AnimSection> anims)
        {
            if (anims.Count > 0) {
                Log.Write(LogType.Info, "Importing animations...");
                Log.PushIndent();

                AnimSetMapping animMapping = AnimSetMapping.GetAnimMapping(version);

                // Process the extracted data next
                Parallel.ForEach(Partitioner.Create(0, anims.Count), range => {
                    for (int i = range.Item1; i < range.Item2; i++) {
                        AnimSection currentAnim = anims[i];

                        AnimSetMapping.Data data = animMapping.Get(currentAnim.Set, currentAnim.Anim);
                        if (data.Category == AnimSetMapping.Discard) {
                            continue;
                        }

                        data.Palette = data.Palette ?? (data.KeepIndexed ? JJ2DefaultPalette.ByIndex : JJ2DefaultPalette.Sprite);

                        int sizeX = (currentAnim.AdjustedSizeX + data.AddBorder * 2);
                        int sizeY = (currentAnim.AdjustedSizeY + data.AddBorder * 2);
                        // Determine the frame configuration to use.
                        // Each asset must fit into a 4096 by 4096 texture,
                        // as that is the smallest texture size we have decided to support.
                        if (currentAnim.FrameCount > 1) {
                            int rows = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(currentAnim.FrameCount * sizeX / sizeY)));
                            int columns = Math.Max(1, (int)Math.Ceiling(currentAnim.FrameCount * 1.0 / rows));

                            // Do a bit of optimization, as the above algorithm ends occasionally with some extra space
                            // (it is careful with not underestimating the required space)
                            while (columns * (rows - 1) >= currentAnim.FrameCount) {
                                rows--;
                            }

                            currentAnim.FrameConfigurationX = (short)columns;
                            currentAnim.FrameConfigurationY = (short)rows;
                        } else {
                            currentAnim.FrameConfigurationX = (short)currentAnim.FrameCount;
                            currentAnim.FrameConfigurationY = 1;
                        }

                        Bitmap img = new Bitmap(sizeX * currentAnim.FrameConfigurationX,
                            sizeY * currentAnim.FrameConfigurationY,
                            PixelFormat.Format32bppArgb);

                        // ToDo: Hardcoded name
                        bool applyToasterPowerUpFix = (data.Category == "Object" && data.Name == "powerup_upgrade_toaster");
                        if (applyToasterPowerUpFix) {
                            Log.Write(LogType.Verbose, "Applying \"Toaster PowerUp\" palette fix.");
                        }

                        for (int j = 0; j < currentAnim.Frames.Length; j++) {
                            ref AnimFrameSection frame = ref currentAnim.Frames[j];

                            int offsetX = currentAnim.NormalizedHotspotX + frame.HotspotX;
                            int offsetY = currentAnim.NormalizedHotspotY + frame.HotspotY;

                            for (ushort y = 0; y < frame.SizeY; y++) {
                                for (ushort x = 0; x < frame.SizeX; x++) {
                                    int targetX =
                                        (j % currentAnim.FrameConfigurationX) * sizeX +
                                        offsetX + x + data.AddBorder;
                                    int targetY =
                                        (j / currentAnim.FrameConfigurationX) * sizeY +
                                        offsetY + y + data.AddBorder;
                                    byte colorIdx = frame.ImageData[frame.SizeX * y + x];

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
                                    new Point(currentAnim.FrameConfigurationX, currentAnim.FrameConfigurationY),
                                    !data.KeepIndexed && data.Palette == JJ2DefaultPalette.ByIndex)) {

                                normalMap.Save(filename.Replace(".png", ".n.png"), ImageFormat.Png);
                            }
                        }

                        CreateAnimationMetadataFile(filename, currentAnim, data, version, sizeX, sizeY);
                    }
                });

                Log.PopIndent();
            }
        }

        private static void CreateAnimationMetadataFile(string filename, AnimSection currentAnim, AnimSetMapping.Data data, JJ2Version version, int sizeX, int sizeY)
        {
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
                if (!data.KeepIndexed) // Use Linear Sampling, if the palette is applied in preprocessing phase
                    flags |= 0x02;

                if (flags != 0x00) {
                    w.WriteLine("    \"Flags\": " + flags + ",");
                }

                w.WriteLine("    \"FrameSize\": [ " +
                            sizeX.ToString(CultureInfo.InvariantCulture) + ", " +
                            sizeY.ToString(CultureInfo.InvariantCulture) + " ],");
                w.WriteLine("    \"FrameConfiguration\": [ " +
                            currentAnim.FrameConfigurationX.ToString(CultureInfo.InvariantCulture) +
                            ", " +
                            currentAnim.FrameConfigurationY.ToString(CultureInfo.InvariantCulture) +
                            " ],");
                w.WriteLine("    \"FrameCount\": " +
                            currentAnim.FrameCount.ToString(CultureInfo.InvariantCulture) + ",");
                w.Write("    \"FrameRate\": " + currentAnim.FrameRate.ToString(CultureInfo.InvariantCulture));

                if (currentAnim.NormalizedHotspotX != 0 || currentAnim.NormalizedHotspotY != 0) {
                    w.WriteLine(",");
                    w.Write("    \"Hotspot\": [ " +
                            (currentAnim.NormalizedHotspotX + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                            ", " +
                            (currentAnim.NormalizedHotspotY + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                            " ]");
                }

                if (currentAnim.Frames[0].ColdspotX != 0 ||
                    currentAnim.Frames[0].ColdspotY != 0) {
                    w.WriteLine(",");
                    w.Write("    \"Coldspot\": [ " +
                            ((currentAnim.NormalizedHotspotX + currentAnim.Frames[0].HotspotX) -
                             currentAnim.Frames[0].ColdspotX + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                            ", " +
                            ((currentAnim.NormalizedHotspotY + currentAnim.Frames[0].HotspotY) -
                             currentAnim.Frames[0].ColdspotY + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                            " ]");
                }

                if (currentAnim.Frames[0].GunspotX != 0 || currentAnim.Frames[0].GunspotY != 0) {
                    w.WriteLine(",");
                    w.Write("    \"Gunspot\": [ " +
                            ((currentAnim.NormalizedHotspotX + currentAnim.Frames[0].HotspotX) -
                             currentAnim.Frames[0].GunspotX + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                            ", " +
                            ((currentAnim.NormalizedHotspotY + currentAnim.Frames[0].HotspotY) -
                             currentAnim.Frames[0].GunspotX + data.AddBorder).ToString(CultureInfo.InvariantCulture) +
                            " ]");
                }

                w.WriteLine();
                w.Write("}");
            }
        }

        private static void ImportAudioSamples(string targetPath, JJ2Version version, RawList<SampleSection> samples)
        {
            if (samples.Count > 0) {
                Log.Write(LogType.Info, "Importing audio samples...");
                Log.PushIndent();

                AnimSetMapping mapping = AnimSetMapping.GetSampleMapping(version);

                Parallel.ForEach(Partitioner.Create(0, samples.Count), range => {
                    for (int i = range.Item1; i < range.Item2; i++) {
                        ref SampleSection sample = ref samples.Data[i];

                        AnimSetMapping.Data data = mapping.Get(sample.Set, sample.IdInSet);
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
                            w.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                            w.Write((uint)(sample.Data.Length + 36));
                            w.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });

                            // Format header
                            w.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                            w.Write((uint)16); // header remainder length
                            w.Write((ushort)1); // format = PCM
                            w.Write((ushort)1); // channels
                            w.Write((uint)sample.SampleRate); // sample rate
                            w.Write((uint)(sample.SampleRate * multiplier)); // byte rate
                            w.Write((uint)(multiplier * 0x00080001));

                            // Payload
                            w.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                            w.Write((uint)sample.Data.Length); // payload length
                            for (int k = 0; k < sample.Data.Length; k++) {
                                w.Write((byte)((multiplier << 7) ^ sample.Data[k]));
                            }
                        }
                    }
                });

                Log.PopIndent();
            }
        }
    }
}