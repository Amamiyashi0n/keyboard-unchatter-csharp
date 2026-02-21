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
        private bool _startupMinimizeApplied;
        private bool _closingToTray;
        private bool _runtimeActiveLoaded;
        private bool _runtimeActive;

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
            bool shouldActivate = keyboard_unchatter_csharp.Properties.Settings.Default.activateOnLaunch;
            if (_runtimeActiveLoaded)
            {
                shouldActivate = _runtimeActive;
            }
            if (shouldActivate)
            {
                ActivateKeyboardMonitor();
            }
            else
            {
                DeactivateKeyboardMonitor();
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

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_allowClose && keyboard_unchatter_csharp.Properties.Settings.Default.closeToTray)
            {
                e.Cancel = true;
                _closingToTray = true;
                MinimizeToTray(true, true);
                return;
            }

            _isExiting = true;

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
        }

        private void LoadRuntimeConfig()
        {
            try
            {
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
                decimal? threshold = ParseDecimal(text, "chatterThreshold");
                bool? openMinimized = ParseBool(text, "openMinimized");
                bool? closeToTray = ParseBool(text, "closeToTray");

                if (threshold.HasValue)
                {
                    keyboard_unchatter_csharp.Properties.Settings.Default.chatterThreshold = threshold.Value;
                }
                if (openMinimized.HasValue)
                {
                    keyboard_unchatter_csharp.Properties.Settings.Default.openMinimized = openMinimized.Value;
                }
                if (closeToTray.HasValue)
                {
                    keyboard_unchatter_csharp.Properties.Settings.Default.closeToTray = closeToTray.Value;
                }

                if (active.HasValue)
                {
                    _runtimeActiveLoaded = true;
                    _runtimeActive = active.Value;
                }
            }
            catch
            {
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
