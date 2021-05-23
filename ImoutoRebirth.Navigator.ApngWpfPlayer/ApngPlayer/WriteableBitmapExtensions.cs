using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImoutoRebirth.Navigator.ApngWpfPlayer.ApngPlayer
{
    internal static unsafe class WriteableBitmapExtensions
    {
        public enum BlendMode
        {
            None,
            Alpha
        }

        /// <summary>
        ///     Copies (blits) the pixels from the WriteableBitmap source to the destination WriteableBitmap (this).
        /// </summary>
        /// <param name="bmp">The destination WriteableBitmap.</param>
        /// <param name="destPosition">The destination position in the destination bitmap.</param>
        /// <param name="source">The source WriteableBitmap.</param>
        /// <param name="sourceRect">The rectangle that will be copied from the source to the destination.</param>
        /// <param name="color">
        ///     If not Colors.White, will tint the source image. A partially transparent color and the image will
        ///     be drawn partially transparent.
        /// </param>
        /// <param name="blendMode">The blending mode <see cref="BlendMode" />.</param>
        public static void Blend(
            this WriteableBitmap bmp,
            Point destPosition,
            WriteableBitmap source,
            Rect sourceRect,
            Color color,
            BlendMode blendMode)
        {
            var destRect = new Rect(destPosition, new Size(sourceRect.Width, sourceRect.Height));
            Blend(bmp, destRect, source, sourceRect, color, blendMode);
        }

        /// <summary>
        ///     Copies (blits) the pixels from the WriteableBitmap source to the destination WriteableBitmap (this).
        /// </summary>
        /// <param name="bmp">The destination WriteableBitmap.</param>
        /// <param name="destRect">The rectangle that defines the destination region.</param>
        /// <param name="source">The source WriteableBitmap.</param>
        /// <param name="sourceRect">The rectangle that will be copied from the source to the destination.</param>
        /// <param name="color">
        ///     If not Colors.White, will tint the source image. A partially transparent color and the image will
        ///     be drawn partially transparent. If the BlendMode is ColorKeying, this color will be used as color key to mask all
        ///     pixels with this value out.
        /// </param>
        /// <param name="blendMode">The blending mode <see cref="BlendMode" />.</param>
        private static void Blend(
            this WriteableBitmap bmp,
            Rect destRect,
            WriteableBitmap source,
            Rect sourceRect,
            Color color,
            BlendMode blendMode)
        {
            if (color.A == 0)
            {
                return;
            }

            var dw = (int) destRect.Width;
            var dh = (int) destRect.Height;

            using var srcContext = source.GetBitmapContext(ReadWriteMode.ReadOnly);

            using var destContext = bmp.GetBitmapContext();

            var sourceWidth = srcContext.Width;
            var dpw = destContext.Width;
            var dph = destContext.Height;

            var intersect = new Rect(0, 0, dpw, dph);
            intersect.Intersect(destRect);
            if (intersect.IsEmpty)
            {
                return;
            }

            var destPixels = destContext.Pixels;
            var sourceLength = dw * dh;

            var px = (int) destRect.X;
            var py = (int) destRect.Y;

            int ca = color.A;
            int cr = color.R;
            int cg = color.G;
            int cb = color.B;
            var tinted = color != Colors.White;
            var sw = (int) sourceRect.Width;
            var sdx = sourceRect.Width / destRect.Width;
            var sdy = sourceRect.Height / destRect.Height;
            var sourceStartX = (int) sourceRect.X;
            var sourceStartY = (int) sourceRect.Y;

            const int lastii = -1;
            const int lastjj = -1;

            double jj = sourceStartY;
            var y = py;
            for (var j = 0; j < dh; j++)
            {
                if (y >= 0 && y < dph)
                {
                    double ii = sourceStartX;
                    var idx = px + y * dpw;
                    var x = px;
                    var sourcePixel = GetPixelValue(srcContext, 0);

                    // Scan line BlockCopy is much faster (3.5x) if no tinting and blending is needed,
                    // even for smaller sprites like the 32x32 particles. 
                    int sourceIdx;
                    if (blendMode == BlendMode.None && !tinted && srcContext.Format.BitsPerPixel == 4*8)
                    {
                        sourceIdx = (int) ii + (int) jj * sourceWidth;
                        var offset = x < 0 ? -x : 0;
                        var xx = x + offset;
                        var wx = sourceWidth - offset;
                        var len = xx + wx < dpw ? wx : dpw - xx;
                        if (len > sw) len = sw;
                        if (len > dw) len = dw;

                        BitmapContext.BlockCopy(
                            srcContext,
                            (sourceIdx + offset) * 4,
                            destContext,
                            (idx + offset) * 4,
                            len * 4);
                    }

                    // Pixel by pixel copying
                    else
                    {
                        for (var i = 0; i < dw; i++)
                        {
                            if (x >= 0 && x < dpw)
                            {
                                if ((int) ii != lastii || (int) jj != lastjj)
                                {
                                    sourceIdx = (int) ii + (int) jj * sourceWidth;
                                    if (sourceIdx >= 0 && sourceIdx < sourceLength)
                                    {
                                        sourcePixel = GetPixelValue(srcContext, sourceIdx);
                                        var sa = (sourcePixel >> 24) & 0xff;
                                        var sr = (sourcePixel >> 16) & 0xff;
                                        var sg = (sourcePixel >> 8) & 0xff;
                                        var sb = sourcePixel & 0xff;
                                        if (tinted && sa != 0)
                                        {
                                            sa = (sa * ca * 0x8081) >> 23;
                                            sr = (((sr * cr * 0x8081) >> 23) * ca * 0x8081) >> 23;
                                            sg = (((sg * cg * 0x8081) >> 23) * ca * 0x8081) >> 23;
                                            sb = (((sb * cb * 0x8081) >> 23) * ca * 0x8081) >> 23;
                                            sourcePixel = (sa << 24) | (sr << 16) | (sg << 8) | sb;
                                        }
                                    }
                                }

                                if (blendMode == BlendMode.None)
                                {
                                    destPixels[idx] = sourcePixel;
                                }

                                if (blendMode == BlendMode.Alpha)
                                {
                                    if (sourcePixel != 0)
                                        destPixels[idx] = sourcePixel;
                                }
                            }

                            x++;
                            idx++;
                            ii += sdx;
                        }
                    }
                }

                jj += sdy;
                y++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPixelValue(BitmapContext sourcePixels, int index)
        {
            if (sourcePixels.Format.BitsPerPixel != 8) 
                return sourcePixels.Pixels[index];


            var pixels = (byte*) sourcePixels.WriteableBitmap.BackBuffer;
            var trueColor = sourcePixels.WriteableBitmap.Palette.Colors[pixels[index]];

            var intColor = BitConverter.ToInt32(new[] {trueColor.B, trueColor.G, trueColor.R, trueColor.A}, 0);
                
            return intColor;
        }
    }
}
