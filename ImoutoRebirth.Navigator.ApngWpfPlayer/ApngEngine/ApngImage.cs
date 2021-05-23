using System;
using System.Collections.Generic;
using System.IO;
using ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine.Chunks;

namespace ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine
{
    internal class ApngImage
    {
        private readonly List<Frame> _frames = new();

        public ApngImage(string fileName)
            : this(File.ReadAllBytes(fileName))
        {
        }

        private ApngImage(byte[] fileBytes)
        {
            var ms = new MemoryStream(fileBytes);

            // check file signature.
            if (!Helper.IsBytesEqual(ms.ReadBytes(Frame.Signature.Length), Frame.Signature))
                throw new Exception("File signature incorrect.");

            // Read IHDR chunk.
            IhdrChunk = new IhdrChunk(ms);
            if (IhdrChunk.ChunkType != "IHDR")
                throw new Exception("IHDR chunk must located before any other chunks.");

            // Now let's loop in chunks
            Chunk chunk;
            Frame frame = null;
            var otherChunks = new List<OtherChunk>();
            var isIdatAlreadyParsed = false;
            do
            {
                if (ms.Position == ms.Length)
                    throw new Exception("IEND chunk expected.");

                chunk = new Chunk(ms);

                switch (chunk.ChunkType)
                {
                    case "IHDR":
                        throw new Exception("Only single IHDR is allowed.");
                        break;

                    case "acTL":
                        if (IsSimplePng)
                            throw new Exception("acTL chunk must located before any IDAT and fdAT");

                        AcTlChunk = new AcTlChunk(chunk);
                        break;

                    case "IDAT":
                        // To be an ApngImage, acTL must located before any IDAT and fdAT.
                        if (AcTlChunk == null)
                            IsSimplePng = true;

                        // Only default image has IDAT.
                        DefaultImage.IhdrChunk = IhdrChunk;
                        DefaultImage.AddIdatChunk(new IdatChunk(chunk));
                        isIdatAlreadyParsed = true;
                        break;

                    case "fcTL":
                        // Simple PNG should ignore this.
                        if (IsSimplePng)
                            continue;

                        if (frame != null && frame.IdatChunks.Count == 0)
                            throw new Exception("One frame must have only one fcTL chunk.");

                        // IDAT already parsed means this fcTL is used by FRAME IMAGE.
                        if (isIdatAlreadyParsed)
                        {
                            // register current frame object and build a new frame object
                            // for next use
                            if (frame != null)
                                _frames.Add(frame);
                            frame = new Frame
                                    {
                                        IhdrChunk = IhdrChunk,
                                        FcTlChunk = new FcTlChunk(chunk)
                                    };
                        }
                            // Otherwise this fcTL is used by the DEFAULT IMAGE.
                        else
                        {
                            DefaultImage.FcTlChunk = new FcTlChunk(chunk);
                        }
                        break;
                    case "fdAT":
                        // Simple PNG should ignore this.
                        if (IsSimplePng)
                            continue;

                        // fdAT is only used by frame image.
                        if (frame == null || frame.FcTlChunk == null)
                            throw new Exception("fcTL chunk expected.");

                        frame.AddIdatChunk(new FdAtChunk(chunk).ToIdatChunk());
                        break;

                    case "IEND":
                        // register last frame object
                        if (frame != null)
                            _frames.Add(frame);

                        if (DefaultImage.IdatChunks.Count != 0)
                            DefaultImage.IendChunk = new IendChunk(chunk);
                        foreach (var f in _frames)
                        {
                            f.IendChunk = new IendChunk(chunk);
                        }
                        break;

                    default:
                        otherChunks.Add(new OtherChunk(chunk));
                        break;
                }
            } while (chunk.ChunkType != "IEND");

            // We have one more thing to do:
            // If the default image if part of the animation,
            // we should insert it into frames list.
            if (DefaultImage.FcTlChunk != null)
            {
                _frames.Insert(0, DefaultImage);
                DefaultImageIsAnimated = true;
            }

            // Now we should apply every chunk in otherChunks to every frame.
            _frames.ForEach(f => otherChunks.ForEach(f.AddOtherChunk));
        }

        /// <summary>
        ///     Indicate whether the file is a simple PNG.
        /// </summary>
        public bool IsSimplePng { get; private set; }

        /// <summary>
        ///     Indicate whether the default image is part of the animation
        /// </summary>
        public bool DefaultImageIsAnimated { get; private set; }

        /// <summary>
        ///     Gets the base image.
        ///     If IsSimplePNG = True, returns the only image;
        ///     if False, returns the default image
        /// </summary>
        public Frame DefaultImage { get; } = new();

        /// <summary>
        ///     Gets the frame array.
        ///     If IsSimplePNG = True, returns empty
        /// </summary>
        public Frame[] Frames => _frames.ToArray();

        /// <summary>
        ///     Gets the IHDR Chunk
        /// </summary>
        public IhdrChunk IhdrChunk { get; private set; }

        /// <summary>
        ///     Gets the acTL Chunk
        /// </summary>
        public AcTlChunk AcTlChunk { get; private set; }
    }
}