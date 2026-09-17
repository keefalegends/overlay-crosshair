using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CrosshairOverlay.Models;
using CrosshairOverlay.Services;
using Microsoft.Win32;

namespace CrosshairOverlay.Views
{
    public partial class OverlayWindow : Window
    {
        private readonly CrosshairConfig _config;
        private IntPtr _hwnd = IntPtr.Zero;
        private DispatcherTimer? _topmostTimer;

        public OverlayWindow(CrosshairConfig config)
        {
            InitializeComponent();
            _config = config;
            Renderer.Config = config;

            UpdatePosition();

            _config.PropertyChanged += Config_PropertyChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            UpdateVisibility();
        }

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            // BeginInvoke (async) avoids blocking the SystemEvents thread — prevents deadlock
            // if the UI thread is busy mid-render when display settings change.
            Dispatcher.BeginInvoke(UpdatePosition, System.Windows.Threading.DispatcherPriority.Normal);
        }

        private void Config_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CrosshairConfig.IsVisible))
            {
                Dispatcher.BeginInvoke(UpdateVisibility, System.Windows.Threading.DispatcherPriority.Normal);
            }
            else if (e.PropertyName == nameof(CrosshairConfig.OffsetX) || e.PropertyName == nameof(CrosshairConfig.OffsetY))
            {
                Dispatcher.BeginInvoke(UpdatePosition, System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        public void UpdatePosition()
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            Left = Math.Round(((screenW - Width) / 2.0) + _config.OffsetX);
            Top = Math.Round(((screenH - Height) / 2.0) + _config.OffsetY);
        }

        private void UpdateVisibility()
        {
            if (_config.IsVisible)
            {
                if (!IsVisible) Show();
                ReassertTopmost();
                // Only run topmost reassertion timer while the overlay is actually visible
                _topmostTimer?.Start();
            }
            else
            {
                Hide();
                // Stop timer while hidden — no need to wake the UI thread every 2s for nothing
                _topmostTimer?.Stop();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new WindowInteropHelper(this).Handle;
            Win32Helper.MakeClickThrough(_hwnd);

            // Timer to periodically reassert topmost status so games won't bury the overlay
            _topmostTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _topmostTimer.Tick += (s, args) => ReassertTopmost();
            _topmostTimer.Start();
        }

        public void ReassertTopmost()
        {
            if (_hwnd != IntPtr.Zero && _config.IsVisible)
            {
                Win32Helper.SetWindowPos(
                    _hwnd,
                    Win32Helper.HWND_TOPMOST,
                    0, 0, 0, 0,
                    Win32Helper.SWP_NOMOVE | Win32Helper.SWP_NOSIZE | Win32Helper.SWP_NOACTIVATE | Win32Helper.SWP_SHOWWINDOW
                );
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _topmostTimer?.Stop();
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            _config.PropertyChanged -= Config_PropertyChanged;
            base.OnClosed(e);
        }
    }
}
