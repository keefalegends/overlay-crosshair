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

            bool f10Ok  = Win32Helper.RegisterHotKey(_hwnd, HOTKEY_TOGGLE_OVERLAY,  Win32Helper.MOD_NOREPEAT, VK_F10);
            bool f9Ok   = Win32Helper.RegisterHotKey(_hwnd, HOTKEY_TOGGLE_SETTINGS, Win32Helper.MOD_NOREPEAT, VK_F9);
            bool pgUpOk = Win32Helper.RegisterHotKey(_hwnd, HOTKEY_STYLE_NEXT,      Win32Helper.MOD_NOREPEAT, VK_PRIOR);
            bool pgDnOk = Win32Helper.RegisterHotKey(_hwnd, HOTKEY_STYLE_PREV,      Win32Helper.MOD_NOREPEAT, VK_NEXT);

            // Notify user if a key couldn't be registered — common cause: OBS, Nvidia, MSI Afterburner
            // stealing F9/F10. Use BeginInvoke so the warning doesn't block app startup.
            if (!f10Ok || !f9Ok || !pgUpOk || !pgDnOk)
            {
                var failed = string.Join(", ", new[]
                {
                    !f10Ok  ? "F10 (Toggle Overlay)" : null,
                    !f9Ok   ? "F9 (Toggle Settings)" : null,
                    !pgUpOk ? "Page Up (Next Style)" : null,
                    !pgDnOk ? "Page Down (Prev Style)" : null,
                }.Where(s => s != null));

                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(
                        $"Warning: The following hotkey(s) could not be registered:\n\n  {failed}\n\n" +
                        "Another application is already using them (e.g. OBS, Nvidia Overlay, MSI Afterburner).\n\n" +
                        "Affected hotkeys won't work in-game. Use the Settings panel buttons instead.",
                        "Hotkey Conflict Detected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
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
            if (_isInitializing || sender is not Slider s) return;

            // Only update the specific property that changed — avoids firing 7 PropertyChanged
            // events (and 7 potential disk writes) every time a single slider moves.
            if      (s == SliderSize)            _config.Size = s.Value;
            else if (s == SliderThickness)       _config.Thickness = s.Value;
            else if (s == SliderGap)             _config.Gap = s.Value;
            else if (s == SliderDotSize)         _config.DotSize = s.Value;
            else if (s == SliderOpacity)         _config.Opacity = s.Value / 100.0;
            else if (s == SliderOutlineThickness) _config.OutlineThickness = s.Value;
            else if (s == SliderOffsetX)         _config.OffsetX = (int)s.Value;
            else if (s == SliderOffsetY)         _config.OffsetY = (int)s.Value;
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

        private void BtnCopyEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText("keefastudys@gmail.com");
                MessageBox.Show("Alamat email 'keefastudys@gmail.com' berhasil disalin ke clipboard!", "Email Disalin", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyalin email: {ex.Message}", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnOpenProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/keefalegends",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tidak dapat membuka browser: {ex.Message}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSendGithubIssue_Click(object sender, RoutedEventArgs e)
        {
            string category = (CmbFeedbackType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Feedback";
            string feedback = TxtFeedback.Text.Trim();

            if (string.IsNullOrEmpty(feedback))
            {
                MessageBox.Show("Silakan tulis pesan atau masukanmu terlebih dahulu!", "Pesan Kosong", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string title = Uri.EscapeDataString($"[{category.Trim()}] Feedback dari Pengguna");
                string body = Uri.EscapeDataString($"### Kategori\n{category.Trim()}\n\n### Deskripsi / Masukan\n{feedback}\n\n---\n*Dikirim melalui Crosshair Overlay v1.0.0*");
                string url = $"https://github.com/keefalegends/overlay-crosshair/issues/new?title={title}&body={body}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tidak dapat membuka browser: {ex.Message}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSendEmail_Click(object sender, RoutedEventArgs e)
        {
            string category = (CmbFeedbackType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Feedback";
            string feedback = TxtFeedback.Text.Trim();

            if (string.IsNullOrEmpty(feedback))
            {
                MessageBox.Show("Silakan tulis pesan atau masukanmu terlebih dahulu!", "Pesan Kosong", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Copy text to clipboard as safety backup
                Clipboard.SetText(feedback);

                string subject = Uri.EscapeDataString($"[Crosshair Overlay] {category.Trim()}");
                string body = Uri.EscapeDataString($"Kategori: {category.Trim()}\n\nMasukan:\n{feedback}\n\n---\nDikirim dari Crosshair Overlay v1.0.0");
                
                // Direct Gmail web compose link (works in all modern browsers without blank tab issue)
                string gmailUrl = $"https://mail.google.com/mail/?view=cm&fs=1&to=keefastudys@gmail.com&su={subject}&body={body}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = gmailUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Fallback to standard mailto if web URL fails
                try
                {
                    string subject = Uri.EscapeDataString($"[Crosshair Overlay] {category.Trim()}");
                    string body = Uri.EscapeDataString(feedback);
                    string mailto = $"mailto:keefastudys@gmail.com?subject={subject}&body={body}";
                    Process.Start(new ProcessStartInfo { FileName = mailto, UseShellExecute = true });
                }
                catch
                {
                    MessageBox.Show($"Tidak dapat membuka browser/email: {ex.Message}\n\nPesan sudah otomatis disalin ke clipboard.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnCopyFeedback_Click(object sender, RoutedEventArgs e)
        {
            string feedback = TxtFeedback.Text.Trim();
            if (string.IsNullOrEmpty(feedback))
            {
                MessageBox.Show("Silakan tulis pesan terlebih dahulu sebelum menyalin!", "Pesan Kosong", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Clipboard.SetText(feedback);
                MessageBox.Show("Pesan masukan berhasil disalin ke clipboard!", "Tersalin", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyalin: {ex.Message}", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            _hwndSource?.Dispose();  // Release unmanaged Win32 handle
            _hwndSource = null;
            _config.PropertyChanged -= Config_PropertyChanged;
            ConfigService.Save(_config);
            base.OnClosed(e);
        }
    }
}
