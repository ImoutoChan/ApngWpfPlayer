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
        private readonly List<WriteableBitmap> _readyFrames = new();

        private bool _isPlaying = false;
        private int _currentFrame = -1;
        private bool _loaded = false;
        private ApngImage? _apngSource;

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
                await control.LoadApng(path);
            }
        }


        private async Task LoadApng(string path)
        {
            if (_loaded)
                return;

            await _loadLocker.WaitAsync();

            try
            {
                if (_loaded)
                    return;
                
                StopPlaying();

                _apngSource = new ApngImage(path);
                if (_apngSource.IsSimplePng)
                {
                    Image.Source = new BitmapImage(new Uri(path));
                    _apngSource = null;
                }
                else
                {
                    StartPlaying();
                }

                _loaded = true;
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

        private async void StartPlaying()
        {
            if (_isPlaying)
                return;

            _isPlaying = true;

            if (_apngSource == null)
            {
                throw new InvalidOperationException();
            }
            
            WriteableBitmap? writeableBitmap = null;
            while (_isPlaying)
            {
                _currentFrame++;

                if (_currentFrame >= _apngSource.Frames.Length)
                    _currentFrame = 0;

                if (_apngSource.Frames.Length == 0)
                    return;
                
                var frame = _apngSource.Frames[_currentFrame];
                if (_readyFrames.Count <= _currentFrame)
                {
                    var xOffset = frame.FcTlChunk.XOffset;
                    var yOffset = frame.FcTlChunk.YOffset;
                    
                    writeableBitmap ??= BitmapFactory.New(
                        (int) _apngSource.DefaultImage.FcTlChunk.Width,
                        (int) _apngSource.DefaultImage.FcTlChunk.Height);

                    using(writeableBitmap.GetBitmapContext())
                    {
                        var frameBitmap = BitmapFactory.FromStream(frame.GetStream());

                        var blendMode = _currentFrame == 0 || frame.FcTlChunk.BlendOp == BlendOps.ApngBlendOpSource
                            ? WriteableBitmapExtensions.BlendMode.None
                            : WriteableBitmapExtensions.BlendMode.Alpha;

                        if (blendMode == WriteableBitmapExtensions.BlendMode.None)
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
                    _readyFrames.Add(writeableBitmap.Clone());
                    _readyFrames[_currentFrame].Freeze();

                    if (_apngSource.Frames.Length == _currentFrame - 1)
                    {
                        writeableBitmap.Freeze();
                        writeableBitmap = null;
                    }
                }

                Image.Source = _readyFrames[_currentFrame];

                var den = frame.FcTlChunk.DelayDen == 0 ? 100 : frame.FcTlChunk.DelayDen;
                var num = frame.FcTlChunk.DelayNum;

                var delay = (int) (num * (1000.0 / den));
                await Task.Delay(delay);
            };
        }

        private void StopPlaying()
        {
            _isPlaying = false;
        }
    }
}
