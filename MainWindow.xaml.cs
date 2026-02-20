using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace keyboard_unchatter_csharp
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<KeyStatItem> _statItems = new ObservableCollection<KeyStatItem>();
        private readonly Dictionary<int, KeyStatItem> _statIndex = new Dictionary<int, KeyStatItem>();
        private Forms.NotifyIcon _notifyIcon;
        private Font _trayFont;
        private PrivateFontCollection _trayFontCollection;
        private bool _allowClose;
        private bool _suppressThresholdEvents;
        private bool _navExpanded = true;
        private bool _navAutoCollapsed;
        private bool _isExiting;
        private bool _writebackLaunched;
        private bool _startupMinimizeApplied;
        private bool _closingToTray;

        private static readonly byte[] _runtimeConfigMarker = Encoding.ASCII.GetBytes("KUC1CFG1");

        public MainWindow()
        {
            InitializeComponent();
            LoadRuntimeConfig();
            KeyStatsList.ItemsSource = _statItems;
            OpenMinimizedCheckBox.IsChecked = keyboard_unchatter_csharp.Properties.Settings.Default.openMinimized;
            CloseToTrayCheckBox.IsChecked = keyboard_unchatter_csharp.Properties.Settings.Default.closeToTray;
            ThresholdSlider.Value = (double)keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold;
            ThresholdInputBox.Text = keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold.ToString();
            InitializeTray();
            ApplyNavState();

            if (App.KeyboardMonitor != null)
            {
                App.KeyboardMonitor.OnKeyPress += OnKeyPress;
                App.KeyboardMonitor.OnKeyBlocked += OnKeyBlocked;
            }

            Loaded += OnLoaded;
            SizeChanged += OnWindowSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Topmost = true;
            Activate();
            if (keyboard_unchatter_csharp.Properties.Settings.Default.activateOnLaunch)
            {
                ActivateKeyboardMonitor();
            }

            if (keyboard_unchatter_csharp.Properties.Settings.Default.openMinimized)
            {
                _startupMinimizeApplied = true;
                WindowState = WindowState.Minimized;
                MinimizeToTray(false, true);
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    Topmost = false;
                }));
            }

            try
            {
                SaveRuntimeConfig();
            }
            catch
            {
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_allowClose && keyboard_unchatter_csharp.Properties.Settings.Default.closeToTray)
            {
                e.Cancel = true;
                _closingToTray = true;
                MinimizeToTray(true, true);
                try
                {
                    SaveRuntimeConfig();
                }
                catch
                {
                }
                return;
            }

            _isExiting = true;
            StartWritebackHelperIfNeeded();
            try
            {
                SaveRuntimeConfig();
            }
            catch
            {
            }

            if (App.KeyboardMonitor != null)
            {
                App.KeyboardMonitor.OnKeyPress -= OnKeyPress;
                App.KeyboardMonitor.OnKeyBlocked -= OnKeyBlocked;
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            if (_trayFont != null)
            {
                _trayFont.Dispose();
                _trayFont = null;
            }
            if (_trayFontCollection != null)
            {
                _trayFontCollection.Dispose();
                _trayFontCollection = null;
            }

            base.OnClosing(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (_isExiting)
            {
                return;
            }
            if (_closingToTray)
            {
                _closingToTray = false;
                return;
            }
            if (WindowState == WindowState.Minimized)
            {
                bool showToast = !_startupMinimizeApplied && IsLoaded;
                MinimizeToTray(false, showToast);
                _startupMinimizeApplied = false;
            }
        }

        private void InitializeTray()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "键盘去抖动";
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;

            var menu = new Forms.ContextMenuStrip();
            var openItem = new Forms.ToolStripMenuItem("打开");
            var exitItem = new Forms.ToolStripMenuItem("退出");
            menu.Items.Add(openItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(exitItem);

            openItem.Click += (s, e) => ShowProgramWindow();
            exitItem.Click += (s, e) =>
            {
                RequestShutdown();
            };

            try
            {
                _trayFont = new Font("Microsoft YaHei", 9.0f, System.Drawing.FontStyle.Regular);
                menu.Font = _trayFont;
                openItem.Font = _trayFont;
                exitItem.Font = _trayFont;
            }
            catch
            {
            }

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.Visible = true;
            _notifyIcon.MouseClick += OnNotifyIconMouseClick;
        }

        private void OnNotifyIconMouseClick(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                ShowProgramWindow();
            }
        }

        private void RequestShutdown()
        {
            if (_isExiting)
            {
                return;
            }
            _isExiting = true;
            _allowClose = true;
            StartWritebackHelperIfNeeded();
            Dispatcher.BeginInvoke(new Action(() => Application.Current.Shutdown()));
        }

        private void MinimizeToTray(bool showTip, bool showToast)
        {
            ShowInTaskbar = false;
            WindowState = WindowState.Minimized;
            Hide();
            _closingToTray = false;
            if (showToast)
            {
                ToastWindow.ShowToast("已最小化到托盘", "程序在后台运行，可通过托盘图标打开。");
            }
            if (showTip)
            {
                ShowTrayTip("已在托盘后台运行", "程序继续在后台运行，可在托盘图标中打开。");
            }
        }

        private void ShowTrayTip(string title, string message)
        {
            if (_notifyIcon == null)
            {
                return;
            }
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(1500);
        }

        private void ShowProgramWindow()
        {
            if (!IsVisible)
            {
                ShowInTaskbar = true;
                Show();
                WindowState = WindowState.Normal;
            }
            Activate();
        }

        private void OnActivateButtonClick(object sender, RoutedEventArgs e)
        {
            if (App.KeyboardMonitor != null && !App.KeyboardMonitor.Active)
            {
                ActivateKeyboardMonitor();
            }
            else
            {
                DeactivateKeyboardMonitor();
            }
        }

        private void ActivateKeyboardMonitor()
        {
            StatusDot.Fill = (System.Windows.Media.Brush)FindResource("ActiveBrush");
            StatusText.Text = "运行中";
            StartActivateButtonTransition(true);

            if (App.KeyboardMonitor != null)
            {
                App.KeyboardMonitor.Activate();
            }
            try
            {
                SaveRuntimeConfig();
            }
            catch
            {
            }
        }

        private void DeactivateKeyboardMonitor()
        {
            StatusDot.Fill = (System.Windows.Media.Brush)FindResource("InactiveBrush");
            StatusText.Text = "未启用";
            StartActivateButtonTransition(false);

            if (App.KeyboardMonitor != null)
            {
                App.KeyboardMonitor.Deactivate();
            }
            try
            {
                SaveRuntimeConfig();
            }
            catch
            {
            }
        }

        private void OnThresholdValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (_suppressThresholdEvents)
            {
                return;
            }

            double value = Math.Round(ThresholdSlider.Value);
            ApplyThreshold(value, true);
        }

        private void OnThresholdLostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplyThresholdFromInput();
        }

        private void OnThresholdPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void OnThresholdPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyThresholdFromInput();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ThresholdInputBox.Text = ThresholdSlider.Value.ToString("0");
                ThresholdInputBox.SelectAll();
                e.Handled = true;
            }
        }

        private void OnApplyThresholdClick(object sender, RoutedEventArgs e)
        {
            ApplyThresholdFromInput();
        }

        private void ApplyThresholdFromInput()
        {
            double value;
            if (double.TryParse(ThresholdInputBox.Text, out value))
            {
                ApplyThreshold(value, true);
            }
            else
            {
                ThresholdInputBox.Text = ThresholdSlider.Value.ToString("0");
            }
        }

        private void ApplyThreshold(double value, bool persist)
        {
            double min = ThresholdSlider.Minimum;
            double max = ThresholdSlider.Maximum;
            double clamped = Math.Max(min, Math.Min(max, Math.Round(value)));

            _suppressThresholdEvents = true;
            ThresholdSlider.Value = clamped;
            ThresholdInputBox.Text = clamped.ToString("0");
            _suppressThresholdEvents = false;

            keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold = (decimal)clamped;

            if (App.KeyboardMonitor != null)
            {
                App.KeyboardMonitor.ChatterTimeMs = clamped;
            }

            if (persist)
            {
                try
                {
                    SaveRuntimeConfig();
                }
                catch
                {
                }
            }
        }

        private void StartActivateButtonTransition(bool active)
        {
            var storyboardKey = active ? "ActivateButtonToActive" : "ActivateButtonToInactive";
            var storyboard = (System.Windows.Media.Animation.Storyboard)FindResource(storyboardKey);
            storyboard.Begin(this, true);
        }

        private void OnOpenMinimizedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            keyboard_unchatter_csharp.Properties.Settings.Default.openMinimized = OpenMinimizedCheckBox.IsChecked == true;
            try
            {
                SaveRuntimeConfig();
            }
            catch
            {
            }
        }

        private void OnCloseToTrayChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            keyboard_unchatter_csharp.Properties.Settings.Default.closeToTray = CloseToTrayCheckBox.IsChecked == true;
            try
            {
                SaveRuntimeConfig();
            }
            catch
            {
            }
        }

        private void OnNavToggleClick(object sender, RoutedEventArgs e)
        {
            _navExpanded = !_navExpanded;
            ApplyNavState();
        }

        private void ApplyNavState()
        {
            NavColumn.Width = _navExpanded ? new GridLength(260) : new GridLength(84);
            NavSpacerColumn.Width = new GridLength(24);
            NavExpandedPanel.Visibility = _navExpanded ? Visibility.Visible : Visibility.Collapsed;
            NavCompactPanel.Visibility = _navExpanded ? Visibility.Collapsed : Visibility.Visible;

            if (NavContainer != null)
            {
                NavContainer.Padding = _navExpanded ? new Thickness(18, 20, 18, 20) : new Thickness(10, 16, 10, 16);
            }
            if (NavScrollViewer != null)
            {
                NavScrollViewer.VerticalScrollBarVisibility = _navExpanded ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth < 900)
            {
                if (_navExpanded)
                {
                    _navExpanded = false;
                    _navAutoCollapsed = true;
                    ApplyNavState();
                }
            }
            else
            {
                if (_navAutoCollapsed)
                {
                    _navExpanded = true;
                    _navAutoCollapsed = false;
                    ApplyNavState();
                }
            }
        }

        private void OnNavMainSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            var list = sender as ListBox;
            if (list == null)
            {
                return;
            }
            int index = list.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            if (list == NavMainList)
            {
                if (NavMainCompactList.SelectedIndex != index)
                {
                    NavMainCompactList.SelectedIndex = index;
                }
            }
            else if (list == NavMainCompactList)
            {
                if (NavMainList.SelectedIndex != index)
                {
                    NavMainList.SelectedIndex = index;
                }
            }

            HomePage.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            StatsPage.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            SettingsPage.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnNavListPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W || e.Key == Key.S)
            {
                e.Handled = true;
            }
        }

        private void OnProjectLinkClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/Amamiyashi0n/keyboard-unchatter-csharp") { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show("无法打开链接。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnSponsorLinkClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://afdian.com/a/amamiyashion/plan") { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show("无法打开链接。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnKeyPress(Forms.Keys key)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => UpdateKeyStats(key, false)));
        }

        private void OnKeyBlocked(Forms.Keys key)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => UpdateKeyStats(key, true)));
        }

        private void UpdateKeyStats(Forms.Keys key, bool blocked)
        {
            int code = (int)key;
            KeyStatItem item;

            if (!_statIndex.TryGetValue(code, out item))
            {
                item = new KeyStatItem(key.ToString());
                _statIndex[code] = item;
                _statItems.Add(item);
            }

            if (blocked)
            {
                item.IncrementBlock();
            }
            else
            {
                item.IncrementPress();
            }
        }

        private void SaveRuntimeConfig()
        {
            try
            {
                bool active = App.KeyboardMonitor != null && App.KeyboardMonitor.Active;
                string json = "{"
                    + "\"active\":" + (active ? "true" : "false") + ","
                    + "\"chatterThreshold\":" + ((double)keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold).ToString("0", CultureInfo.InvariantCulture) + ","
                    + "\"openMinimized\":" + (keyboard_unchatter_csharp.Properties.Settings.Default.openMinimized ? "true" : "false") + ","
                    + "\"closeToTray\":" + (keyboard_unchatter_csharp.Properties.Settings.Default.closeToTray ? "true" : "false")
                    + "}";

                using (var store = IsolatedStorageFile.GetUserStoreForAssembly())
                using (var stream = new IsolatedStorageFileStream("runtime.config.json", FileMode.Create, FileAccess.Write, store))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write(json);
                }
            }
            catch
            {
            }
        }

        private void LoadRuntimeConfig()
        {
            try
            {
                TryExtractEmbeddedRuntimeConfigToIsolatedStorage();

                string text = null;
                using (var store = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    if (IsolatedStorageFileExists(store, "runtime.config.json"))
                    {
                        using (var stream = new IsolatedStorageFileStream("runtime.config.json", FileMode.Open, FileAccess.Read, store))
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            text = reader.ReadToEnd();
                        }
                    }
                }

                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                bool? active = ParseBool(text, "active");
                bool? openMinimized = ParseBool(text, "openMinimized");
                bool? closeToTray = ParseBool(text, "closeToTray");
                decimal? threshold = ParseDecimal(text, "chatterThreshold");

                if (openMinimized.HasValue)
                {
                    keyboard_unchatter_csharp.Properties.Settings.Default.openMinimized = openMinimized.Value;
                }
                if (closeToTray.HasValue)
                {
                    keyboard_unchatter_csharp.Properties.Settings.Default.closeToTray = closeToTray.Value;
                }
                if (threshold.HasValue)
                {
                    keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold = threshold.Value;
                }

                _suppressThresholdEvents = true;
                OpenMinimizedCheckBox.IsChecked = keyboard_unchatter_csharp.Properties.Settings.Default.openMinimized;
                CloseToTrayCheckBox.IsChecked = keyboard_unchatter_csharp.Properties.Settings.Default.closeToTray;
                ThresholdSlider.Value = (double)keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold;
                ThresholdInputBox.Text = keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold.ToString("0");
                _suppressThresholdEvents = false;

                if (App.KeyboardMonitor != null)
                {
                    App.KeyboardMonitor.ChatterTimeMs = (double)keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold;
                }

                if (active.HasValue)
                {
                    if (active.Value)
                    {
                        ActivateKeyboardMonitor();
                    }
                    else
                    {
                        DeactivateKeyboardMonitor();
                    }
                }
            }
            catch
            {
            }
        }

        private void StartWritebackHelperIfNeeded()
        {
            if (_writebackLaunched)
            {
                return;
            }
            _writebackLaunched = true;

            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(exePath))
                {
                    return;
                }

                var psi = new ProcessStartInfo();
                psi.FileName = exePath;
                psi.Arguments = "--writeback " + Process.GetCurrentProcess().Id + " \"" + exePath + "\"";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi);
            }
            catch
            {
            }
        }

        public static void RunWritebackHelper(string[] args)
        {
            try
            {
                if (args == null || args.Length < 4)
                {
                    return;
                }

                int pid;
                if (!int.TryParse(args[2], out pid))
                {
                    return;
                }

                string exePath = args[3];
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    return;
                }

                try
                {
                    var p = Process.GetProcessById(pid);
                    p.WaitForExit(8000);
                }
                catch
                {
                }

                string json = null;
                try
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForAssembly())
                    {
                        if (IsolatedStorageFileExists(store, "runtime.config.json"))
                        {
                            using (var stream = new IsolatedStorageFileStream("runtime.config.json", FileMode.Open, FileAccess.Read, store))
                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                json = reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                }

                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                bool writebackOk = WriteRuntimeConfigToExe(exePath, json);

                if (writebackOk)
                {
                    try
                    {
                        using (var store = IsolatedStorageFile.GetUserStoreForAssembly())
                        {
                            if (IsolatedStorageFileExists(store, "runtime.config.json"))
                            {
                                store.DeleteFile("runtime.config.json");
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                try
                {
                    string dir = Path.GetDirectoryName(exePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        string legacyPath = Path.Combine(dir, "keyboard-unchatter-csharp.config.json");
                        if (File.Exists(legacyPath))
                        {
                            File.Delete(legacyPath);
                        }
                    }
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        private void TryExtractEmbeddedRuntimeConfigToIsolatedStorage()
        {
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    return;
                }

                string embedded = ReadRuntimeConfigFromExe(exePath);
                if (string.IsNullOrEmpty(embedded))
                {
                    return;
                }

                using (var store = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    if (IsolatedStorageFileExists(store, "runtime.config.json"))
                    {
                        return;
                    }
                    using (var stream = new IsolatedStorageFileStream("runtime.config.json", FileMode.Create, FileAccess.Write, store))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.Write(embedded);
                    }
                }
            }
            catch
            {
            }
        }

        private static string ReadRuntimeConfigFromExe(string exePath)
        {
            try
            {
                using (var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long offset;
                    int length;
                    if (!TryFindRuntimeConfigTrailer(fs, out offset, out length))
                    {
                        return null;
                    }

                    long payloadOffset = offset + _runtimeConfigMarker.Length + 4;
                    if (payloadOffset + length > fs.Length)
                    {
                        return null;
                    }

                    fs.Position = payloadOffset;
                    byte[] data = new byte[length];
                    int read = 0;
                    while (read < length)
                    {
                        int n = fs.Read(data, read, length - read);
                        if (n <= 0)
                        {
                            break;
                        }
                        read += n;
                    }
                    if (read != length)
                    {
                        return null;
                    }
                    return Encoding.UTF8.GetString(data, 0, data.Length);
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool TryFindRuntimeConfigTrailer(FileStream fs, out long offset, out int length)
        {
            offset = 0;
            length = 0;

            try
            {
                if (fs == null || fs.Length < _runtimeConfigMarker.Length + 4)
                {
                    return false;
                }

                long searchStart = fs.Length - 1024 * 1024;
                if (searchStart < 0)
                {
                    searchStart = 0;
                }

                int searchLen = (int)(fs.Length - searchStart);
                byte[] buffer = new byte[searchLen];
                fs.Position = searchStart;
                int got = 0;
                while (got < searchLen)
                {
                    int n = fs.Read(buffer, got, searchLen - got);
                    if (n <= 0)
                    {
                        break;
                    }
                    got += n;
                }

                if (got != searchLen)
                {
                    return false;
                }

                for (int i = buffer.Length - (_runtimeConfigMarker.Length + 4); i >= 0; i--)
                {
                    bool match = true;
                    for (int j = 0; j < _runtimeConfigMarker.Length; j++)
                    {
                        if (buffer[i + j] != _runtimeConfigMarker[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (!match)
                    {
                        continue;
                    }

                    int len = BitConverter.ToInt32(buffer, i + _runtimeConfigMarker.Length);
                    if (len < 0)
                    {
                        continue;
                    }

                    long payloadStart = searchStart + i + _runtimeConfigMarker.Length + 4;
                    long available = fs.Length - payloadStart;
                    if (len > available)
                    {
                        continue;
                    }

                    offset = searchStart + i;
                    length = len;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool WriteRuntimeConfigToExe(string exePath, string json)
        {
            string tempPath = exePath + ".tmp";
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(json);
                byte[] lenBytes = BitConverter.GetBytes(payload.Length);

                using (var input = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    long offset;
                    int length;
                    long copyLen = input.Length;
                    if (TryFindRuntimeConfigTrailer(input, out offset, out length))
                    {
                        copyLen = offset;
                    }

                    input.Position = 0;
                    CopyBytes(input, output, copyLen);

                    output.Write(_runtimeConfigMarker, 0, _runtimeConfigMarker.Length);
                    output.Write(lenBytes, 0, lenBytes.Length);
                    output.Write(payload, 0, payload.Length);
                }

                try
                {
                    File.Replace(tempPath, exePath, null);
                }
                catch
                {
                    File.Copy(tempPath, exePath, true);
                    File.Delete(tempPath);
                }

                // 清理可能残留的临时文件
                CleanupTempFiles(exePath);

                string verify = ReadRuntimeConfigFromExe(exePath);
                return string.Equals(verify, json, StringComparison.Ordinal);
            }
            catch
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                }
                CleanupTempFiles(exePath);
                return false;
            }
        }

        private static void CleanupTempFiles(string exePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(exePath);
                string fileName = Path.GetFileName(exePath);
                
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    return;
                }

                // 清理 .tmp 文件
                string tempPath = exePath + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                // 清理 Windows File.Replace 产生的备份文件 (例如: exe~RFxxxxx.TMP)
                try
                {
                    string[] tempFiles = Directory.GetFiles(directory, fileName + "~*");
                    foreach (string tempFile in tempFiles)
                    {
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch
                        {
                            // 忽略单个文件删除失败
                        }
                    }
                }
                catch
                {
                    // 忽略目录扫描失败
                }
            }
            catch
            {
                // 忽略所有清理错误
            }
        }

        private static void CopyBytes(Stream input, Stream output, long count)
        {
            byte[] buffer = new byte[81920];
            long remaining = count;
            while (remaining > 0)
            {
                int read = input.Read(buffer, 0, remaining > buffer.Length ? buffer.Length : (int)remaining);
                if (read <= 0)
                {
                    break;
                }
                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        private static bool IsolatedStorageFileExists(IsolatedStorageFile store, string fileName)
        {
            try
            {
                if (store == null || string.IsNullOrEmpty(fileName))
                {
                    return false;
                }
                string[] matches = store.GetFileNames(fileName);
                return matches != null && matches.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool? ParseBool(string json, string name)
        {
            try
            {
                var m = Regex.Match(json, "\\\"" + name + "\\\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    return string.Compare(m.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase) == 0;
                }
            }
            catch
            {
            }
            return null;
        }

        private decimal? ParseDecimal(string json, string name)
        {
            try
            {
                var m = Regex.Match(json, "\\\"" + name + "\\\"\\s*:\\s*([0-9]+(\\.[0-9]+)?)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    decimal v;
                    if (decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    {
                        return v;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private class KeyStatItem : INotifyPropertyChanged
        {
            private int _pressCount;
            private int _blockCount;

            public event PropertyChangedEventHandler PropertyChanged;

            public string KeyName { get; private set; }

            public int PressCount
            {
                get { return _pressCount; }
            }

            public int BlockCount
            {
                get { return _blockCount; }
            }

            public KeyStatItem(string keyName)
            {
                KeyName = keyName;
            }

            public void IncrementPress()
            {
                _pressCount++;
                OnPropertyChanged("PressCount");
            }

            public void IncrementBlock()
            {
                _blockCount++;
                OnPropertyChanged("BlockCount");
            }

            private void OnPropertyChanged(string name)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
            }
        }
    }
}
