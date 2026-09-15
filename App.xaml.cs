using System.Windows;
using CrosshairOverlay.Models;
using CrosshairOverlay.Services;
using CrosshairOverlay.Views;

namespace CrosshairOverlay
{
    public partial class App : Application
    {
        private CrosshairConfig? _config;
        private OverlayWindow? _overlayWindow;
        private SettingsWindow? _settingsWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _config = ConfigService.Load();

            _overlayWindow = new OverlayWindow(_config);
            _overlayWindow.Show();

            _settingsWindow = new SettingsWindow(_config, _overlayWindow);
            _settingsWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_config != null)
            {
                ConfigService.Save(_config);
            }
            _overlayWindow?.Close();
            base.OnExit(e);
        }
    }
}
