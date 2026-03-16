#region Licence

// Distributed under MIT License
// ===========================================================
// 
// digiCamControl - DSLR camera remote control open source software
// Copyright (C) 2014 Duka Istvan
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY,FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY 
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
// THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

#region

using System;
using System.ComponentModel;
//using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Point = System.Windows.Point;

#endregion

namespace CameraControl.windows
{
    /// <summary>
    /// Interaction logic for LiveViewWnd.xaml
    /// </summary>
    public partial class LiveViewWnd : IWindow, INotifyPropertyChanged
    {

        private ICameraDevice _selectedPortableDevice;

        private DateTime _focusMoveTime = DateTime.Now;

        // Motion guide drawing state
        // step 0 = idle
        // step 1 = P0 placed, mouse moves show dashed line preview to cursor
        // step 2 = P1 placed, mouse moves bend the arc (Cp follows cursor)
        private int _guideStep = 0;
        private Point _guideP0;      // start point
        private Point _guideP1;      // end point
        private Point _guideCp;      // quadratic bezier control point (follows mouse in step 2)
        private Point _guideCurrent; // current mouse position

        public LiveViewData LiveViewData { get; set; }

        private CameraProperty _cameraProperty;

        public CameraProperty CameraProperty
        {
            get { return _cameraProperty; }
            set
            {
                _cameraProperty = value;
                NotifyPropertyChanged("CameraProperty");
            }
        }

        public ICameraDevice SelectedPortableDevice
        {
            get { return this._selectedPortableDevice; }
            set
            {
                if (this._selectedPortableDevice != value)
                {
                    this._selectedPortableDevice = value;
                    NotifyPropertyChanged("SelectedPortableDevice");
                }
            }
        }

        public LiveViewWnd()
        {
            try
            {
                SelectedPortableDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
                LiveViewManager.PreviewLoaded += LiveViewManager_PreviewLoaded;
            }
            catch (Exception ex)
            {
                Log.Error("Live view init error ", ex);
            }
            Init();
        }

        public LiveViewWnd(ICameraDevice device)
        {
            try
            {
                SelectedPortableDevice = device;
            }
            catch (Exception ex)
            {
                Log.Error("Live view init error ", ex);
            }
            Init();
        }

        private void LiveViewManager_PreviewLoaded(ICameraDevice cameraDevice, string file)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(500);
                App.Current.BeginInvoke(zoomAndPanControl.ScaleToFit);
            });
        }

        public void Init()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Log.Error("Live view init error ", ex);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //SelectedPortableDevice.StoptLiveView();
        }


        private void Window_Closed(object sender, EventArgs e)
        {
        }



        private void image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left &&
                _selectedPortableDevice.LiveViewImageZoomRatio.Value == "All")
            {
                try
                {
                    ((LiveViewViewModel)DataContext).SetFocusPos(e.MouseDevice.GetPosition(_image), _image.ActualWidth,
                        _image.ActualHeight);

                }
                catch (Exception exception)
                {
                    Log.Error("Focus Error", exception);
                    StaticHelper.Instance.SystemMessage = "Focus error: " + exception.Message;
                }
            }
        }


        #region Implementation of IWindow

        public void ExecuteCommand(string cmd, object param)
        {

            Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    if (DataContext != null)
                        ((LiveViewViewModel)(DataContext)).WindowsManager_Event(cmd, param);
                }
                catch (Exception)
                {


                }
            }));
            switch (cmd)
            {
                case WindowsCmdConsts.LiveViewWnd_Show:
                    Dispatcher.Invoke(new Action(delegate
                        {
                            try
                            {
                                ICameraDevice cameraparam = param as ICameraDevice;
                                var properties = cameraparam.LoadProperties();
                                if (properties.SaveLiveViewWindow && properties.WindowRect.Width > 0 &&
                                    properties.WindowRect.Height > 0)
                                {
                                    this.Left = properties.WindowRect.Left;
                                    this.Top = properties.WindowRect.Top;
                                    this.Width = properties.WindowRect.Width;
                                    this.Height = properties.WindowRect.Height;
                                }
                                else
                                {
                                    this.WindowState =
                                        ((Window) ServiceProvider.PluginManager.SelectedWindow).WindowState;
                                }

                                if (cameraparam == SelectedPortableDevice && IsVisible)
                                {
                                    Activate();
                                    Focus();
                                    return;
                                }


                                var lvm = new LiveViewViewModel(cameraparam, this);
                                lvm.MotionGuides.CollectionChanged += (s2, e2) => RefreshGuideCanvas();
                                DataContext = lvm;
                                SelectedPortableDevice = cameraparam;
                                RefreshGuideCanvas();

                                Show();
                                Activate();
                                Focus();

                            }
                            catch (Exception exception)
                            {
                                Log.Error("Error initialize live view window ", exception);
                            }
                        }
                    ));
                    break;
                case WindowsCmdConsts.LiveViewWnd_Hide:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        try
                        {
                            ICameraDevice cameraparam = ((LiveViewViewModel)DataContext).CameraDevice;
                            var properties = cameraparam.LoadProperties();
                            if (properties.SaveLiveViewWindow)
                            {
                                properties.WindowRect = new Rect(this.Left, this.Top, this.Width, this.Height);
                            }
                            ((LiveViewViewModel)DataContext).UnInit();
                        }
                        catch (Exception exception)
                        {
                            Log.Error("Unable to stop live view", exception);
                        }
                        Hide();
                        //ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.FocusStackingWnd_Hide);
                    }));
                    break;
                case WindowsCmdConsts.LiveViewWnd_Message:
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            MessageBox.Show((string)param);
                        }));
                    }
                    break;
                case CmdConsts.All_Close:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        if (DataContext != null)
                        {
                            ICameraDevice cameraparam = ((LiveViewViewModel)DataContext).CameraDevice;
                            var properties = cameraparam.LoadProperties();
                            if (properties.SaveLiveViewWindow)
                            {
                                properties.WindowRect = new Rect(this.Left, this.Top, this.Width, this.Height);
                            }
                            ((LiveViewViewModel)DataContext).UnInit();
                            Hide();
                            Close();
                        }
                    }));
                    break;
                case CmdConsts.All_Minimize:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        WindowState = WindowState.Minimized;
                    }));
                    break;
                case WindowsCmdConsts.LiveViewWnd_Maximize:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        WindowState = WindowState.Maximized;
                    }));
                    break;
            }
        }

        #endregion

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (IsVisible)
            {
                e.Cancel = true;
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.LiveViewWnd_Hide, SelectedPortableDevice);
            }
        }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion


        private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _guideStep != 0)
            {
                _guideStep = 0;
                GuideCanvas.ReleaseMouseCapture();
                RefreshGuideCanvas();
                e.Handled = true;
                return;
            }
            if ((DateTime.Now - _focusMoveTime).TotalMilliseconds < 200)
                return;
            _focusMoveTime = DateTime.Now;
            TriggerClass.KeyDown(e);
        }

        private void canvas_image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    ((LiveViewViewModel)DataContext).SetFocusPos(e.MouseDevice.GetPosition(_previeImage), _previeImage.ActualWidth,
                        _previeImage.ActualHeight);

                }
                catch (Exception exception)
                {
                    Log.Error("Focus Error", exception);
                    StaticHelper.Instance.SystemMessage = "Focus error: " + exception.Message;
                }
            }

        }

        private void btn_help_Click(object sender, RoutedEventArgs e)
        {
            HelpProvider.Run(HelpSections.LiveView);
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //CameraProperty.LiveviewSettings.CanvasHeight = slide_vert.ActualHeight;
            //CameraProperty.LiveviewSettings.CanvasWidt = slide_horiz.ActualWidth;
        }

        private void _image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                ((LiveViewViewModel)DataContext).CameraDevice.LiveViewImageZoomRatio.NextValue();
            }
            else
            {
                ((LiveViewViewModel)DataContext).CameraDevice.LiveViewImageZoomRatio.PrevValue();
            }
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ((LiveViewViewModel)DataContext).SetOverlay(files[0]);
                ((LiveViewViewModel)DataContext).OverlayActivated = true;
                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
            }
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            ((LiveViewViewModel)DataContext).IsMinized = this.WindowState == WindowState.Minimized;
        }

        private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            Point curContentMousePoint = e.GetPosition(PreviewBitmap);
            if (e.Delta > 0)
            {
                zoomAndPanControl.ZoomIn(curContentMousePoint);
            }
            else if (e.Delta < 0)
            {
                // don't allow zoomout les that original image 
                if (zoomAndPanControl.ContentScale - 0.2 > zoomAndPanControl.FitScale())
                {
                    zoomAndPanControl.ZoomOut(curContentMousePoint);
                }
                else
                {
                    zoomAndPanControl.ScaleToFit();
                }
            }
        }

        private void zoomAndPanControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            zoomAndPanControl.ScaleToFit();
        }

        private void zoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point curContentMousePoint = e.GetPosition(PreviewBitmap);
            if (zoomAndPanControl.ContentScale <= zoomAndPanControl.FitScale())
            {
                zoomAndPanControl.ZoomAboutPoint(4, curContentMousePoint);
            }
            else
            {
                zoomAndPanControl.ScaleToFit();
            }
        }

        private void ButtonZoomMinus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((LiveViewViewModel)DataContext).CameraDevice.StartZoom(ZoomDirection.Out);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomMinus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((LiveViewViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.Out);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomMinus_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                ((LiveViewViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.Out);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomPlus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((LiveViewViewModel)DataContext).CameraDevice.StartZoom(ZoomDirection.In);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomPlus_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                ((LiveViewViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.In);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomPlus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((LiveViewViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.In);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        // ── Motion Guides ─────────────────────────────────────────────────

        private LiveViewViewModel GuideVM => DataContext as LiveViewViewModel;

        private void GuideCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = GuideVM;
            if (vm == null || !vm.MotionGuidesDrawMode) return;

            Point p = e.GetPosition(GuideCanvas);

            if (_guideStep == 0)
            {
                // Step 0→1: place start point
                _guideP0 = p;
                _guideCurrent = p;
                _guideStep = 1;
                GuideCanvas.CaptureMouse();
            }
            else if (_guideStep == 1)
            {
                // Step 1→2: place end point; control point starts at midpoint
                _guideP1 = p;
                _guideCp = new Point((_guideP0.X + p.X) / 2, (_guideP0.Y + p.Y) / 2);
                _guideCurrent = _guideCp;
                _guideStep = 2;
                RefreshGuideCanvas();
            }
            else if (_guideStep == 2)
            {
                // Step 2→0: confirm arc with current control point
                _guideCp = p;
                var guide = new MotionGuide
                {
                    X0  = _guideP0.X, Y0  = _guideP0.Y,
                    X1  = _guideP1.X, Y1  = _guideP1.Y,
                    CpX = _guideCp.X, CpY = _guideCp.Y,
                };
                vm.MotionGuides.Add(guide);
                vm.SaveMotionGuides();
                _guideStep = 0;
                GuideCanvas.ReleaseMouseCapture();
                RefreshGuideCanvas();
            }
        }

        private void GuideCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_guideStep == 0) return;
            _guideCurrent = e.GetPosition(GuideCanvas);
            if (_guideStep == 2)
                _guideCp = _guideCurrent; // bend the arc live
            RefreshGuideCanvas();
        }

        private void GuideCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // clicks handled in MouseDown — nothing extra needed here
        }

        private void GuideCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_guideStep != 0)
            {
                _guideStep = 0;
                GuideCanvas.ReleaseMouseCapture();
                RefreshGuideCanvas();
            }
        }

        private void RefreshGuideCanvas()
        {
            GuideCanvas.Children.Clear();
            var vm = GuideVM;
            if (vm == null) return;

            // Draw all saved guides
            foreach (var g in vm.MotionGuides)
                GuideCanvas.Children.Add(MakeArcPath(g.X0, g.Y0, g.X1, g.Y1, g.CpX, g.CpY, Brushes.Yellow, 2.5));

            if (_guideStep == 1)
            {
                // Dashed line from P0 to cursor
                GuideCanvas.Children.Add(MakeDot(_guideP0.X, _guideP0.Y, Brushes.Yellow));
                GuideCanvas.Children.Add(MakeLinePath(_guideP0.X, _guideP0.Y, _guideCurrent.X, _guideCurrent.Y, Brushes.Yellow));
            }
            else if (_guideStep == 2)
            {
                // Live arc preview bending with mouse
                GuideCanvas.Children.Add(MakeDot(_guideP0.X, _guideP0.Y, Brushes.Yellow));
                GuideCanvas.Children.Add(MakeDot(_guideP1.X, _guideP1.Y, Brushes.Yellow));
                GuideCanvas.Children.Add(MakeDot(_guideCp.X, _guideCp.Y, Brushes.Orange));  // control point
                GuideCanvas.Children.Add(MakeArcPath(_guideP0.X, _guideP0.Y, _guideP1.X, _guideP1.Y,
                                                     _guideCp.X, _guideCp.Y, Brushes.Yellow, 2.5));
                // Dashed helper lines from endpoints to control point
                GuideCanvas.Children.Add(MakeLinePath(_guideP0.X, _guideP0.Y, _guideCp.X, _guideCp.Y, Brushes.Orange));
                GuideCanvas.Children.Add(MakeLinePath(_guideP1.X, _guideP1.Y, _guideCp.X, _guideCp.Y, Brushes.Orange));
            }
        }

        private static Path MakeArcPath(double x0, double y0, double x1, double y1, double cpX, double cpY, Brush stroke, double thickness)
        {
            var fig = new PathFigure(new Point(x0, y0), new PathSegment[]
            {
                new QuadraticBezierSegment(new Point(cpX, cpY), new Point(x1, y1), true)
            }, false);
            return new Path
            {
                Stroke = stroke,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = new PathGeometry(new[] { fig })
            };
        }

        private static Path MakeLinePath(double x0, double y0, double x1, double y1, Brush stroke)
        {
            var fig = new PathFigure(new Point(x0, y0), new PathSegment[]
            {
                new LineSegment(new Point(x1, y1), true)
            }, false);
            return new Path
            {
                Stroke = stroke,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 6, 4 },
                Data = new PathGeometry(new[] { fig })
            };
        }

        private static Ellipse MakeDot(double cx, double cy, Brush fill)
        {
            var e = new Ellipse { Width = 10, Height = 10, Fill = fill };
            System.Windows.Controls.Canvas.SetLeft(e, cx - 5);
            System.Windows.Controls.Canvas.SetTop(e, cy - 5);
            return e;
        }
    }
}