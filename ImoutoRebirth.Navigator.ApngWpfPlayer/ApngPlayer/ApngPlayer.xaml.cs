using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine;
using ImoutoRebirth.Navigator.ApngWpfPlayer.ApngEngine.Chunks;

namespace ImoutoRebirth.Navigator.ApngWpfPlayer.ApngPlayer
{
    /// <summary>
    /// Interaction logic for ApngPlayer.xaml
    /// </summary>
    public partial class ApngPlayer : UserControl
    {
        private readonly SemaphoreSlim _loadLocker = new(1);
        private ApngImage? _apngSource;
        private CancellationTokenSource? _playingToken;

        public ApngPlayer()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SourceProperty 
            = DependencyProperty.Register(
                nameof(Source), 
                typeof (string), 
                typeof (ApngPlayer), 
                new UIPropertyMetadata(null, OnSourceChanged));

        public string Source
        {
            get => (string) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private static async void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var path = (string) e.NewValue;
            var control = (ApngPlayer) d;

            if (string.IsNullOrEmpty(path))
            {
                control.UnloadApng();
            }
            else
            {
                await control.ReloadApng(path);
            }
        }

        private async Task ReloadApng(string path)
        {
            await _loadLocker.WaitAsync();

            try
            {
                StopPlaying();

                _apngSource = new ApngImage(path);
                if (_apngSource.IsSimplePng)
                {
                    StartPlaying(true);
                }
                else
                {
                    _playingToken = new CancellationTokenSource();
                    StartPlaying(false, _playingToken.Token);
                }
            }
            finally
            {
                _loadLocker.Release();
            }
        }

        private void UnloadApng()
        {
            StopPlaying();
            _apngSource = null;
        }

        private async void StartPlaying(bool simple = false, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested)
                return;

            if (_apngSource == null)
            {
                throw new InvalidOperationException();
            }

            if (simple)
            {
                Image.Source = GetBitmapImage(_apngSource);
                return;
            }
            
            var currentFrame = -1;
            List<WriteableBitmap> readyFrames = new();
            WriteableBitmap? writeableBitmap = null;
            var numPlays = 0;

            while (!ct.IsCancellationRequested)
            {
                currentFrame++;

                if (currentFrame >= _apngSource.Frames.Length)
                {
                    numPlays++;
                    if (numPlays == _apngSource.AcTlChunk.NumPlays)
                        break;

                    currentFrame = 0;
                }

                if (_apngSource.Frames.Length == 0)
                    return;
                
                var frame = _apngSource.Frames[currentFrame];

                if (readyFrames.Count <= currentFrame)
                {
                    var xOffset = frame.FcTlChunk.XOffset;
                    var yOffset = frame.FcTlChunk.YOffset;
                    
                    writeableBitmap ??= BitmapFactory.New(
                        _apngSource.IhdrChunk.Width,
                        _apngSource.IhdrChunk.Height);

                    var writeableBitmapForCurrentFrame = writeableBitmap.Clone();
                    using (writeableBitmapForCurrentFrame.GetBitmapContext())
                    {
                        var frameBitmap = FromStream(frame.GetStream());
                        
                        var blendMode = currentFrame == 0 || frame.FcTlChunk.BlendOp == BlendOps.ApngBlendOpSource
                            ? WriteableBitmapExtensions.BlendMode.None
                            : WriteableBitmapExtensions.BlendMode.Alpha;

                        if (blendMode == WriteableBitmapExtensions.BlendMode.None
                            && Math.Abs(frameBitmap.Width - writeableBitmapForCurrentFrame.Width) < 0.01
                            && Math.Abs(frameBitmap.Height - writeableBitmapForCurrentFrame.Height) < 0.01)
                        {
                            writeableBitmapForCurrentFrame = frameBitmap;
                        }
                        else
                        {
                            writeableBitmapForCurrentFrame.Blend(
                                new Point((int)xOffset, (int)yOffset),
                                frameBitmap,
                                new Rect(0, 0, frame.FcTlChunk.Width, frame.FcTlChunk.Height),
                                Colors.White,
                                blendMode);
                        }
                    }

                    readyFrames.Add(writeableBitmapForCurrentFrame.Clone());
                    readyFrames[currentFrame].Freeze();

                    switch (frame.FcTlChunk.DisposeOp)
                    {
                        case DisposeOps.ApngDisposeOpNone:
                            writeableBitmap = writeableBitmapForCurrentFrame;
                            break;
                        case DisposeOps.ApngDisposeOpPrevious:
                            // ignore change in this frame
                            break;
                        case DisposeOps.ApngDisposeOpBackground:
                            writeableBitmapForCurrentFrame.Blend(
                                new Point((int)xOffset, (int)yOffset),
                                BitmapFactory.New((int)frame.FcTlChunk.Width, (int)frame.FcTlChunk.Height),
                                new Rect(0, 0, frame.FcTlChunk.Width, frame.FcTlChunk.Height),
                                Colors.White,
                                WriteableBitmapExtensions.BlendMode.None);
                            writeableBitmap = writeableBitmapForCurrentFrame;
                            break;
                    }

                    if (_apngSource.Frames.Length == currentFrame - 1)
                    {
                        writeableBitmap.Freeze();
                        writeableBitmap = null;
                    }
                }

                Image.Source = readyFrames[currentFrame];
                
                var den = frame.FcTlChunk.DelayDen == 0 ? 100 : frame.FcTlChunk.DelayDen;
                var num = frame.FcTlChunk.DelayNum;

                var delay = (int) (num * (1000.0 / den));
                await Task.Delay(delay);
            };
        }

        private static BitmapImage GetBitmapImage(ApngImage apngSource)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = apngSource.DefaultImage.GetStream();
                bi.EndInit();
                bi.Freeze();

                return bi;
            }
            catch
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = GetUri(apngSource.SourcePath);
                bi.EndInit();
                bi.Freeze();

                return bi;
            }
        }

        private static Uri GetUri(string source)
        {
            try
            {
                return new Uri(source);
            }
            catch
            {
                var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return new Uri(Path.Combine(appDirectory!, source));
            };
        }

        private void StopPlaying() => _playingToken?.Cancel();
        
        public static WriteableBitmap FromStream(Stream stream)
        {
            var source = new BitmapImage();
            source.BeginInit();
            source.CreateOptions = BitmapCreateOptions.None;
            source.StreamSource = stream;
            source.EndInit();
            var writeableBitmap = new WriteableBitmap(BitmapFactory.ConvertToPbgra32Format(source));
            source.UriSource = null;
            return writeableBitmap;
        }
    }
}
