using System;
using System.Collections.Generic;
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
                    Image.Source = new BitmapImage(new Uri(path));
                    _apngSource = null;
                }
                else
                {
                    _playingToken = new CancellationTokenSource();
                    StartPlaying(_playingToken.Token);
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

        private async void StartPlaying(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return;

            if (_apngSource == null)
            {
                throw new InvalidOperationException();
            }
            
            var currentFrame = -1;
            List<WriteableBitmap> readyFrames = new();
            WriteableBitmap? writeableBitmap = null;

            while (!ct.IsCancellationRequested)
            {
                currentFrame++;

                if (currentFrame >= _apngSource.Frames.Length)
                    currentFrame = 0;

                if (_apngSource.Frames.Length == 0)
                    return;
                
                var frame = _apngSource.Frames[currentFrame];
                if (readyFrames.Count <= currentFrame)
                {
                    var xOffset = frame.FcTlChunk.XOffset;
                    var yOffset = frame.FcTlChunk.YOffset;
                    
                    writeableBitmap ??= BitmapFactory.New(
                        (int) _apngSource.DefaultImage.FcTlChunk.Width,
                        (int) _apngSource.DefaultImage.FcTlChunk.Height);

                    using(writeableBitmap.GetBitmapContext())
                    {
                        var frameBitmap = BitmapFactory.FromStream(frame.GetStream());

                        var blendMode = currentFrame == 0 || frame.FcTlChunk.BlendOp == BlendOps.ApngBlendOpSource
                            ? WriteableBitmapExtensions.BlendMode.None
                            : WriteableBitmapExtensions.BlendMode.Alpha;

                        if (blendMode == WriteableBitmapExtensions.BlendMode.None
                            && Math.Abs(frameBitmap.Width - writeableBitmap.Width) < 0.01
                            && Math.Abs(frameBitmap.Height - writeableBitmap.Height) < 0.01)
                        {
                            writeableBitmap = frameBitmap;
                        }
                        else
                        {
                            writeableBitmap.Blend(
                                new Point((int) (xOffset),
                                    (int) (yOffset)),
                                frameBitmap,
                                new Rect(0, 0, frame.FcTlChunk.Width, frame.FcTlChunk.Height),
                                Colors.White,
                                blendMode);
                        }
                    }
                    readyFrames.Add(writeableBitmap.Clone());
                    readyFrames[currentFrame].Freeze();

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

        private void StopPlaying() => _playingToken?.Cancel();
    }
}
