using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.Plugins.ToolPlugins
{
    public class ImageSequencerViewModel : ViewModelBase
    {
        private DispatcherTimer _playbackTimer;
        private bool _isPlaying = false;
        private BitmapSource _bitmap;
        private int _totalImages;
        private int _currentImages;
        private Window _window;
        private int _fps;
        private int _minValue;
        private int _maxValue;
        private bool _loop;
        private bool _isPaused;
        private BitmapSource _previewBitmap;
        private BitmapSource _previewBitmap10;
        private BitmapSource _previewBitmap11;
        private BitmapSource _previewBitmap12;
        private BitmapSource _previewBitmap20;
        private BitmapSource _previewBitmap21;
        private BitmapSource _previewBitmap22;
        private List<FileItem> _visibleFiles = new List<FileItem>();
        private ConcurrentDictionary<int, BitmapSource> _frameCache = new ConcurrentDictionary<int, BitmapSource>();
        private CancellationTokenSource _cacheCts;

        // --- RAM pre-decode playback ---
        private volatile bool _ramLoaded = false;
        private BitmapSource[] _ramFrames;
        private int _ramMin;
        private int _ramMax;
        private int _ramPlayIndex;
        private CancellationTokenSource _loadCts;
        private int _decodeWidth = 1280;
        private int _loadProgress;
        private bool _isLoading;
        private bool _isLoaded;

        private void PreCacheFramesAsync()
        {
            _cacheCts?.Cancel();
            _cacheCts = new CancellationTokenSource();
            var token = _cacheCts.Token;
            var files = _visibleFiles.ToList();
            var min = MinValue;
            var max = MaxValue;
            Task.Run(() =>
            {
                for (int i = min; i <= max; i++)
                {
                    if (token.IsCancellationRequested) break;
                    if (_frameCache.ContainsKey(i)) continue;
                    if (i >= files.Count) break;
                    var bmp = BitmapLoader.Instance.LoadImage(files[i], false);
                    if (bmp != null && !token.IsCancellationRequested)
                        _frameCache[i] = bmp;
                }
            }, token);
        }

        private void LoadFramesAsync(Action onComplete = null)
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;
            var files = _visibleFiles.ToList();
            int min = MinValue, max = MaxValue, decodeWidth = _decodeWidth;
            int count = max - min + 1;
            IsLoading = true;
            IsLoaded = false;
            LoadProgress = 0;

            Task.Run(() =>
            {
                var frames = new BitmapSource[count];
                var stdExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif" };

                for (int i = 0; i < count; i++)
                {
                    if (token.IsCancellationRequested) break;
                    int fi = min + i;
                    if (fi >= files.Count) break;
                    var item = files[fi];
                    try
                    {
                        string ext = System.IO.Path.GetExtension(item.FileName);
                        BitmapSource frame;

                        if (stdExts.Contains(ext))
                        {
                            // Fast path: BitmapImage with DecodePixelWidth — JPEG DCT sub-sampling,
                            // decodes directly at target width without loading full-res pixels.
                            string src = (!string.IsNullOrEmpty(item.LargeThumb) && System.IO.File.Exists(item.LargeThumb))
                                ? item.LargeThumb : item.FileName;
                            if (!System.IO.File.Exists(src)) { frames[i] = null; continue; }
                            var bi = new BitmapImage();
                            bi.BeginInit();
                            bi.UriSource = new Uri(src);
                            bi.DecodePixelWidth = decodeWidth;
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.EndInit();
                            bi.Freeze();
                            frame = bi;
                        }
                        else
                        {
                            // RAW/other: BitmapLoader handles dcraw etc., then downscale if needed
                            var wb = BitmapLoader.Instance.LoadImage(item, false);
                            if (wb == null) { frames[i] = null; continue; }
                            if (wb.PixelWidth > decodeWidth)
                            {
                                double scale = (double)decodeWidth / wb.PixelWidth;
                                var tb = new TransformedBitmap(wb, new System.Windows.Media.ScaleTransform(scale, scale));
                                frame = BitmapFrame.Create(tb);
                            }
                            else
                                frame = wb;
                            frame.Freeze();
                        }
                        frames[i] = frame;
                    }
                    catch (Exception ex) { Log.Error(ex); frames[i] = null; }

                    int captured = i;
                    Application.Current?.Dispatcher.InvokeAsync(() =>
                        LoadProgress = (int)((captured + 1) * 100.0 / count));
                }

                if (token.IsCancellationRequested)
                {
                    Application.Current?.Dispatcher.InvokeAsync(() => IsLoading = false);
                    return;
                }

                // Commit: write array before setting _ramLoaded (volatile ensures visibility)
                _ramFrames = frames;
                _ramMin = min;
                _ramMax = max;
                _ramLoaded = true;

                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    LoadProgress = 100;
                    IsLoading = false;
                    IsLoaded = true;
                    onComplete?.Invoke();
                });
            }, token);
        }

        private void InvalidateRamFrames()
        {
            _ramLoaded = false;
            IsLoaded = false;
            _ramFrames = null;
        }

        private void RebuildVisibleFiles()
        {
            _visibleFiles = ServiceProvider.Settings.DefaultSession.Files
                .Where(f => f.Visible)
                .ToList();
            TotalImages = _visibleFiles.Count - 1;
        }

        public BitmapSource Bitmap
        {
            get { return _bitmap; }
            set
            {
                _bitmap = value;
                RaisePropertyChanged(() => Bitmap);
            }
        }

        public BitmapSource PreviewBitmap
        {
            get { return _previewBitmap; }
            set
            {
                _previewBitmap = value;
                RaisePropertyChanged(() => PreviewBitmap);
            }
        }

        public BitmapSource PreviewBitmap10
        {
            get { return _previewBitmap10; }
            set
            {
                _previewBitmap10 = value;
                RaisePropertyChanged(() => PreviewBitmap10);
            }
        }

        public BitmapSource PreviewBitmap11
        {
            get { return _previewBitmap11; }
            set
            {
                _previewBitmap11 = value;
                RaisePropertyChanged(() => PreviewBitmap11);
            }
        }

        public BitmapSource PreviewBitmap12
        {
            get { return _previewBitmap12; }
            set
            {
                _previewBitmap12 = value;
                RaisePropertyChanged(() => PreviewBitmap12);
            }
        }

        public BitmapSource PreviewBitmap20
        {
            get { return _previewBitmap20; }
            set
            {
                _previewBitmap20 = value;
                RaisePropertyChanged(() => PreviewBitmap20);
            }
        }

        public BitmapSource PreviewBitmap21
        {
            get { return _previewBitmap21; }
            set
            {
                _previewBitmap21 = value;
                RaisePropertyChanged(() => PreviewBitmap21);
            }
        }

        public BitmapSource PreviewBitmap22
        {
            get { return _previewBitmap22; }
            set
            {
                _previewBitmap22 = value;
                RaisePropertyChanged(() => PreviewBitmap22);
            }
        }

        public int TotalImages
        {
            get { return _totalImages; }
            set
            {
                _totalImages = value;
                if (MaxValue > _totalImages)
                    MaxValue = TotalImages;
                RaisePropertyChanged(() => TotalImages);
            }
        }

        public int CurrentImages
        {
            get { return _currentImages; }
            set
            {
                _currentImages = value;
                try
                {
                    if (CurrentImages >= 0 && CurrentImages < _visibleFiles.Count)
                    {
                        // Use RAM frames first (zero I/O) — covers slider write-back during RAM playback
                        if (_ramLoaded && _ramFrames != null)
                        {
                            int idx = CurrentImages - _ramMin;
                            if (idx >= 0 && idx < _ramFrames.Length && _ramFrames[idx] != null)
                            {
                                Bitmap = _ramFrames[idx];
                                RaisePropertyChanged(() => CurrentImages);
                                RaisePropertyChanged(() => CounterText);
                                return;
                            }
                        }
                        if (_frameCache.TryGetValue(CurrentImages, out var cached))
                        {
                            Bitmap = cached;
                        }
                        else
                        {
                            var item = _visibleFiles[CurrentImages];
                            Bitmap = BitmapLoader.Instance.LoadImage(item, false);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
                RaisePropertyChanged(() => CurrentImages);
                RaisePropertyChanged(() => CounterText);
            }
        }

        public int Fps
        {
            get { return _fps; }
            set
            {
                _fps = value;
                RaisePropertyChanged(() => Fps);
            }
        }

        public bool IsBusy
        {
            get { return _isPlaying; }
        }

        public bool IsFree
        {
            get { return !_isPlaying; }
        }

        public string CounterText
        {
            get { return string.Format("[{0}] {1}/{2} [{3}]", MinValue + 1, CurrentImages - MinValue + 1, MaxValue - MinValue + 1, MaxValue + 1); }
        }

        public int MinValue
        {
            get { return _minValue; }
            set
            {
                _minValue = value;
                InvalidateRamFrames();
                RaisePropertyChanged(() => MemoryEstimateText);
                if (CurrentImages < MinValue)
                    CurrentImages = MinValue;
                PreviewBitmap = GetThubnail(MinValue);
                PreviewBitmap10 = GetThubnail(MinValue-1);
                PreviewBitmap11 = GetThubnail(MinValue);
                PreviewBitmap12 = GetThubnail(MinValue + 1);
                RaisePropertyChanged(() => MinValue);
                RaisePropertyChanged(() => CounterText);
            }
        }

        public int MaxValue
        {
            get { return _maxValue; }
            set
            {
                _maxValue = value;
                InvalidateRamFrames();
                RaisePropertyChanged(() => MemoryEstimateText);
                if (CurrentImages > MaxValue)
                    CurrentImages = MaxValue;
                PreviewBitmap = GetThubnail(MaxValue);
                PreviewBitmap20 = GetThubnail(MaxValue - 1);
                PreviewBitmap21 = GetThubnail(MaxValue);
                PreviewBitmap22 = GetThubnail(MaxValue + 1);
                RaisePropertyChanged(() => MaxValue);
                RaisePropertyChanged(() => CounterText);
            }
        }

        public bool Loop
        {
            get { return _loop; }
            set
            {
                _loop = value;
                RaisePropertyChanged(() => Loop);
            }
        }

        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                if (_isPlaying && _playbackTimer != null)
                {
                    if (_isPaused)
                        _playbackTimer.Stop();
                    else
                        _playbackTimer.Start();
                }
                RaisePropertyChanged(() => IsPaused);
            }
        }

        public int DecodeWidth
        {
            get { return _decodeWidth; }
            set
            {
                _decodeWidth = value;
                RaisePropertyChanged(() => DecodeWidth);
                RaisePropertyChanged(() => MemoryEstimateText);
                InvalidateRamFrames();
            }
        }

        public int LoadProgress
        {
            get { return _loadProgress; }
            private set { _loadProgress = value; RaisePropertyChanged(() => LoadProgress); RaisePropertyChanged(() => LoadProgressText); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            private set
            {
                _isLoading = value;
                RaisePropertyChanged(() => IsLoading);
                RaisePropertyChanged(() => IsNotLoading);
                RaisePropertyChanged(() => LoadProgressText);
                LoadFramesCommand?.RaiseCanExecuteChanged();
            }
        }

        public string LoadProgressText
        {
            get { return _isLoading ? string.Format("Loading {0}%", _loadProgress) : string.Empty; }
        }

        public bool IsNotLoading { get { return !_isLoading; } }

        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { _isLoaded = value; RaisePropertyChanged(() => IsLoaded); }
        }

        public string MemoryEstimateText
        {
            get
            {
                int count = MaxValue - MinValue + 1;
                long h = (long)(_decodeWidth * 9.0 / 16.0);
                long mb = (long)count * _decodeWidth * h * 4 / (1024 * 1024);
                return string.Format("~{0} MB", mb);
            }
        }

        public RelayCommand StartCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        public RelayCommand PauseCommand { get; set; }
        public RelayCommand CreateMovieCommand { get; set; }
        public RelayCommand LoadFramesCommand { get; set; }

        public RelayCommand PrevImageCommand1 { get; set; }
        public RelayCommand NextImageCommand1 { get; set; }

        public RelayCommand PrevImageCommand2 { get; set; }
        public RelayCommand NextImageCommand2 { get; set; }

        public ImageSequencerViewModel()
        {

        }

        public ImageSequencerViewModel(Window window)
        {
            _window = window;
            _window.Closed += _window_Closed;
            StartCommand = new RelayCommand(Start);
            StopCommand = new RelayCommand(Stop);
            PauseCommand = new RelayCommand(Pause);
            PrevImageCommand1 = new RelayCommand(() => MinValue--);
            PrevImageCommand2 = new RelayCommand(() => MaxValue--);
            NextImageCommand1 = new RelayCommand(() => MinValue++);
            NextImageCommand2 = new RelayCommand(() => MaxValue++);
            LoadFramesCommand = new RelayCommand(() => LoadFramesAsync(null), () => !_isLoading);

            CreateMovieCommand = new RelayCommand(CreateMovie);
            Fps = 15;
            ServiceProvider.Settings.DefaultSession.PropertyChanged += DefaultSession_PropertyChanged;
            RebuildVisibleFiles();
            MinValue = 0;
            MaxValue = TotalImages;
            CurrentImages = MinValue;
            PreCacheFramesAsync();
        }

        void DefaultSession_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VisibleFilesCount")
            {
                bool wasAtMax = (TotalImages == MaxValue);
                RebuildVisibleFiles();
                if (wasAtMax)
                    MaxValue = TotalImages;
                _frameCache.Clear();
                _loadCts?.Cancel();
                InvalidateRamFrames();
                PreCacheFramesAsync();
            }
        }

        private BitmapSource GetThubnail(int i)
        {
            try
            {
                if (i < 0)
                    return null;
                if (i >= _visibleFiles.Count)
                    return null;
                return _visibleFiles[i].Thumbnail;
            }
            catch (Exception)
            {

            }
            return null;
        }

        void _window_Closed(object sender, EventArgs e)
        {
            ServiceProvider.Settings.DefaultSession.PropertyChanged -= DefaultSession_PropertyChanged;
            _cacheCts?.Cancel();
            _loadCts?.Cancel();
            Stop();
        }

        private void Start()
        {
            _isPlaying = true;
            _isPaused = false;
            RaisePropertyChanged(() => IsPaused);

            if (_playbackTimer == null)
            {
                _playbackTimer = new DispatcherTimer();
                _playbackTimer.Tick += PlaybackTimer_Tick;
            }
            _playbackTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / Fps);
            RaisePropertyChanged(() => IsBusy);
            RaisePropertyChanged(() => IsFree);

            if (_ramLoaded && _ramMin == MinValue && _ramMax == MaxValue)
            {
                // RAM frames ready — start immediately with zero-I/O playback
                _ramPlayIndex = _ramMin;
                CurrentImages = _ramMin;
                _playbackTimer.Start();
            }
            else
            {
                // Load all frames into RAM first, then start timer
                LoadFramesAsync(() =>
                {
                    _ramPlayIndex = _ramMin;
                    CurrentImages = _ramMin;
                    _playbackTimer.Start();
                });
            }
        }

        private void Pause()
        {
            IsPaused = !IsPaused;
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (_ramLoaded && _ramFrames != null)
            {
                // Zero-I/O fast path: array lookup only — microseconds per frame
                int idx = _ramPlayIndex - _ramMin;
                if (idx >= 0 && idx < _ramFrames.Length)
                {
                    var frame = _ramFrames[idx];
                    if (frame != null) Bitmap = frame;
                }
                _currentImages = _ramPlayIndex;
                RaisePropertyChanged(() => CurrentImages);
                RaisePropertyChanged(() => CounterText);

                _ramPlayIndex++;
                if (_ramPlayIndex > _ramMax)
                {
                    if (Loop) _ramPlayIndex = _ramMin;
                    else Stop();
                }
            }
            else
            {
                // Fallback: original path (used when RAM frames not available)
                CurrentImages++;
                if (CurrentImages >= MaxValue)
                {
                    if (Loop)
                        CurrentImages = MinValue;
                    else
                        Stop();
                }
            }
        }

        private void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            _playbackTimer?.Stop();
            RaisePropertyChanged(() => IsBusy);
            RaisePropertyChanged(() => IsFree);
            RaisePropertyChanged(() => IsPaused);
        }

        private void CreateMovie()
        {
            GenMovieWindow window = new GenMovieWindow();
            var viewmodel = new GenMovieViewModel(window);
            viewmodel.Fps = Fps;
            viewmodel.MinValue = MinValue;
            viewmodel.MaxValue = MaxValue;
            window.DataContext = viewmodel;
            window.ShowDialog();
        }
    }
}
