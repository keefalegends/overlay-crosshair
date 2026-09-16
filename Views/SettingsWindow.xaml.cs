using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using CrosshairOverlay.Models;
using CrosshairOverlay.Services;

namespace CrosshairOverlay.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly CrosshairConfig _config;
        private readonly OverlayWindow _overlayWindow;
        private bool _isInitializing = true;
        private IntPtr _hwnd = IntPtr.Zero;
        private HwndSource? _hwndSource;

        private const int HOTKEY_TOGGLE_OVERLAY = 9001;
        private const int HOTKEY_TOGGLE_SETTINGS = 9002;
        private const int HOTKEY_STYLE_NEXT = 9003;
        private const int HOTKEY_STYLE_PREV = 9004;

        private const uint VK_PRIOR = 0x21; // Page Up
        private const uint VK_NEXT = 0x22;  // Page Down
        private const uint VK_F9 = 0x78;
        private const uint VK_F10 = 0x79;

        public SettingsWindow(CrosshairConfig config, OverlayWindow overlayWindow)
        {
            InitializeComponent();
            _config = config;
            _overlayWindow = overlayWindow;

            PreviewRenderer.Config = _config;
            _config.PropertyChanged += Config_PropertyChanged;

            LoadConfigToUI();
            _isInitializing = false;
        }

        private void LoadConfigToUI()
        {
            _isInitializing = true;

            CmbStyle.SelectedIndex = (int)_config.Style;
            TxtColorHex.Text = _config.ColorHex;
            UpdateColorPreview(_config.ColorHex);

            SliderSize.Value = _config.Size;
            SliderThickness.Value = _config.Thickness;
            SliderGap.Value = _config.Gap;
            SliderDotSize.Value = _config.DotSize;
            SliderOpacity.Value = Math.Round(_config.Opacity * 100.0);

            ChkHasOutline.IsChecked = _config.HasOutline;
            SliderOutlineThickness.Value = _config.OutlineThickness;

            SliderOffsetX.Value = _config.OffsetX;
            SliderOffsetY.Value = _config.OffsetY;

            UpdateStatusUI();

            _isInitializing = false;
        }

        private void Config_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CrosshairConfig.IsVisible))
            {
                Dispatcher.Invoke(UpdateStatusUI);
            }
            else if (e.PropertyName == nameof(CrosshairConfig.Style))
            {
                Dispatcher.Invoke(() =>
                {
                    if (CmbStyle.SelectedIndex != (int)_config.Style)
                    {
                        CmbStyle.SelectedIndex = (int)_config.Style;
                    }
                });
            }
            ConfigService.Save(_config);
        }

        private void UpdateStatusUI()
        {
            if (_config.IsVisible)
            {
                TxtStatus.Text = "ACTIVE";
                TxtStatus.Foreground = (Brush)FindResource("AccentGreen");
                BtnToggleVisibility.Content = "Hide Overlay (F10)";
                BtnToggleVisibility.Foreground = (Brush)FindResource("AccentGreen");
            }
            else
            {
                TxtStatus.Text = "HIDDEN";
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 68, 68));
                BtnToggleVisibility.Content = "Show Overlay (F10)";
                BtnToggleVisibility.Foreground = new SolidColorBrush(Color.FromRgb(255, 68, 68));
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(HwndHook);

            RegisterGlobalHotkeys();
        }

        private void RegisterGlobalHotkeys()
        {
            if (_hwnd == IntPtr.Zero) return;

            // F10: Toggle Overlay
            Win32Helper.RegisterHotKey(_hwnd, HOTKEY_TOGGLE_OVERLAY, Win32Helper.MOD_NOREPEAT, VK_F10);
            // F9: Toggle Settings Window
            Win32Helper.RegisterHotKey(_hwnd, HOTKEY_TOGGLE_SETTINGS, Win32Helper.MOD_NOREPEAT, VK_F9);
            // Page Up / Down: Cycle Styles
            Win32Helper.RegisterHotKey(_hwnd, HOTKEY_STYLE_NEXT, Win32Helper.MOD_NOREPEAT, VK_PRIOR);
            Win32Helper.RegisterHotKey(_hwnd, HOTKEY_STYLE_PREV, Win32Helper.MOD_NOREPEAT, VK_NEXT);
        }

        private void UnregisterGlobalHotkeys()
        {
            if (_hwnd == IntPtr.Zero) return;

            Win32Helper.UnregisterHotKey(_hwnd, HOTKEY_TOGGLE_OVERLAY);
            Win32Helper.UnregisterHotKey(_hwnd, HOTKEY_TOGGLE_SETTINGS);
            Win32Helper.UnregisterHotKey(_hwnd, HOTKEY_STYLE_NEXT);
            Win32Helper.UnregisterHotKey(_hwnd, HOTKEY_STYLE_PREV);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32Helper.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_TOGGLE_OVERLAY:
                        _config.IsVisible = !_config.IsVisible;
                        handled = true;
                        break;

                    case HOTKEY_TOGGLE_SETTINGS:
                        ToggleSettingsWindow();
                        handled = true;
                        break;

                    case HOTKEY_STYLE_NEXT:
                        CycleStyle(1);
                        handled = true;
                        break;

                    case HOTKEY_STYLE_PREV:
                        CycleStyle(-1);
                        handled = true;
                        break;
                }
            }
            return IntPtr.Zero;
        }

        private void ToggleSettingsWindow()
        {
            if (IsVisible && WindowState != WindowState.Minimized)
            {
                Hide();
            }
            else
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                Focus();
            }
        }

        private void CycleStyle(int direction)
        {
            int current = (int)_config.Style;
            int total = Enum.GetValues<CrosshairStyle>().Length;
            int next = (current + direction + total) % total;
            _config.Style = (CrosshairStyle)next;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;

            _config.Size = SliderSize.Value;
            _config.Thickness = SliderThickness.Value;
            _config.Gap = SliderGap.Value;
            _config.DotSize = SliderDotSize.Value;
            _config.Opacity = SliderOpacity.Value / 100.0;
            _config.OutlineThickness = SliderOutlineThickness.Value;
            _config.OffsetX = (int)SliderOffsetX.Value;
            _config.OffsetY = (int)SliderOffsetY.Value;
        }

        private void CmbStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (CmbStyle.SelectedIndex >= 0)
            {
                _config.Style = (CrosshairStyle)CmbStyle.SelectedIndex;
            }
        }

        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                TxtColorHex.Text = hex;
            }
        }

        private void TxtColorHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;

            string text = TxtColorHex.Text.Trim();
            if (!text.StartsWith("#"))
            {
                text = "#" + text;
            }

            try
            {
                var brush = (Brush?)new BrushConverter().ConvertFromString(text);
                if (brush != null)
                {
                    _config.ColorHex = text;
                    UpdateColorPreview(text);
                }
            }
            catch
            {
                // Invalid hex, ignore
            }
        }

        private void UpdateColorPreview(string hex)
        {
            try
            {
                var brush = (Brush?)new BrushConverter().ConvertFromString(hex);
                if (brush != null)
                {
                    ColorPreviewSwatch.Background = brush;
                }
            }
            catch { }
        }

        private void ChkHasOutline_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _config.HasOutline = ChkHasOutline.IsChecked == true;
        }

        private void BtnSizeMinus_Click(object sender, RoutedEventArgs e) => SliderSize.Value = Math.Max(SliderSize.Minimum, SliderSize.Value - 1);
        private void BtnSizePlus_Click(object sender, RoutedEventArgs e) => SliderSize.Value = Math.Min(SliderSize.Maximum, SliderSize.Value + 1);

        private void BtnThicknessMinus_Click(object sender, RoutedEventArgs e) => SliderThickness.Value = Math.Max(SliderThickness.Minimum, SliderThickness.Value - 1);
        private void BtnThicknessPlus_Click(object sender, RoutedEventArgs e) => SliderThickness.Value = Math.Min(SliderThickness.Maximum, SliderThickness.Value + 1);

        private void BtnGapMinus_Click(object sender, RoutedEventArgs e) => SliderGap.Value = Math.Max(SliderGap.Minimum, SliderGap.Value - 1);
        private void BtnGapPlus_Click(object sender, RoutedEventArgs e) => SliderGap.Value = Math.Min(SliderGap.Maximum, SliderGap.Value + 1);

        private void BtnDotSizeMinus_Click(object sender, RoutedEventArgs e) => SliderDotSize.Value = Math.Max(SliderDotSize.Minimum, SliderDotSize.Value - 1);
        private void BtnDotSizePlus_Click(object sender, RoutedEventArgs e) => SliderDotSize.Value = Math.Min(SliderDotSize.Maximum, SliderDotSize.Value + 1);

        private void BtnOutlineMinus_Click(object sender, RoutedEventArgs e) => SliderOutlineThickness.Value = Math.Max(SliderOutlineThickness.Minimum, Math.Round(SliderOutlineThickness.Value - 0.5, 1));
        private void BtnOutlinePlus_Click(object sender, RoutedEventArgs e) => SliderOutlineThickness.Value = Math.Min(SliderOutlineThickness.Maximum, Math.Round(SliderOutlineThickness.Value + 0.5, 1));

        private void BtnOffsetXMinus_Click(object sender, RoutedEventArgs e) => SliderOffsetX.Value = Math.Max(SliderOffsetX.Minimum, SliderOffsetX.Value - 1);
        private void BtnOffsetXPlus_Click(object sender, RoutedEventArgs e) => SliderOffsetX.Value = Math.Min(SliderOffsetX.Maximum, SliderOffsetX.Value + 1);

        private void BtnOffsetYMinus_Click(object sender, RoutedEventArgs e) => SliderOffsetY.Value = Math.Max(SliderOffsetY.Minimum, SliderOffsetY.Value - 1);
        private void BtnOffsetYPlus_Click(object sender, RoutedEventArgs e) => SliderOffsetY.Value = Math.Min(SliderOffsetY.Maximum, SliderOffsetY.Value + 1);

        private void BtnResetOffset_Click(object sender, RoutedEventArgs e)
        {
            SliderOffsetX.Value = 0;
            SliderOffsetY.Value = 0;
        }

        private void BtnResetAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Kembalikan semua pengaturan ke nilai awal (default)?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _config.ResetToDefaults();
                LoadConfigToUI();
            }
        }

        private void BtnToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            _config.IsVisible = !_config.IsVisible;
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnHideToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Tutup aplikasi crosshair sepenuhnya?\n\n(Pilih 'No' jika ingin sembunyikan ke background saja dan tetap aktif in-game)",
                "Tutup Crosshair Overlay",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
            else if (result == MessageBoxResult.No)
            {
                Hide();
            }
        }

        private void NavTab_Checked(object sender, RoutedEventArgs e)
        {
            if (SettingsView == null || AboutView == null) return;

            if (TabSettings.IsChecked == true)
            {
                SettingsView.Visibility = Visibility.Visible;
                AboutView.Visibility = Visibility.Collapsed;
            }
            else if (TabAbout.IsChecked == true)
            {
                SettingsView.Visibility = Visibility.Collapsed;
                AboutView.Visibility = Visibility.Visible;
            }
        }

        private void BtnOpenGithub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/keefalegends/overlay-crosshair",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tidak dapat membuka browser: {ex.Message}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            // Allow dragging the window from the title bar
            if (e.GetPosition(this).Y < 46)
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            UnregisterGlobalHotkeys();
            _hwndSource?.RemoveHook(HwndHook);
            _config.PropertyChanged -= Config_PropertyChanged;
            ConfigService.Save(_config);
            base.OnClosed(e);
        }
    }
}
