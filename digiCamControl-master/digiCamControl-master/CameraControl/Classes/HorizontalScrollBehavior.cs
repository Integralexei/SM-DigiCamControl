using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Xaml.Behaviors;

namespace CameraControl.Classes
{
    public class HorizontalScrollBehavior : Behavior<ItemsControl>
    {
        private const int WM_MOUSEHWHEEL = 0x020E;

        /// <summary>A reference to the internal ScrollViewer.</summary>
        private ScrollViewer ScrollViewer { get; set; }

        private HwndSource _hwndSource;

        /// <summary>
        /// By default, scrolling down on the wheel translates to right, and up to left.
        /// Set this to true to invert that translation.
        /// </summary>
        public bool IsInverted { get; set; }

        /// <summary>The ScrollViewer is not available in the visual tree until the control is loaded.</summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= OnLoaded;

            ScrollViewer = VisualTreeHelpers.FindVisualChild<ScrollViewer>(AssociatedObject);

            if (ScrollViewer != null)
                ScrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

            // Hook WM_MOUSEHWHEEL so touchpad left/right swipe scrolls the filmstrip
            var window = Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                _hwndSource = PresentationSource.FromVisual(window) as HwndSource;
                _hwndSource?.AddHook(WndProc);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (ScrollViewer != null)
                ScrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;

            _hwndSource?.RemoveHook(WndProc);
        }

        // Vertical mouse wheel → redirect to horizontal scroll
        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var newOffset = !IsInverted
                ? ScrollViewer.HorizontalOffset + e.Delta / 100
                : ScrollViewer.HorizontalOffset - e.Delta / 100;
            ScrollViewer.ScrollToHorizontalOffset(newOffset);
        }

        // WM_MOUSEHWHEEL: touchpad horizontal swipe (and horizontal wheel on mice)
        // HIWORD(wParam) = signed delta; positive = right, negative = left
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEHWHEEL && ScrollViewer != null)
            {
                int delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                var newOffset = !IsInverted
                    ? ScrollViewer.HorizontalOffset + delta / 100.0
                    : ScrollViewer.HorizontalOffset - delta / 100.0;
                ScrollViewer.ScrollToHorizontalOffset(newOffset);
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
